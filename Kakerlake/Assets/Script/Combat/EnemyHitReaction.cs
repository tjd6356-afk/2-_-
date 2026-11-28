using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyStats), typeof(Rigidbody))]
public class EnemyHitReaction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody body;
    [SerializeField] private NavMeshAgent agent;
    [Tooltip("AI scripts that move or attack. Do not include EnemyStats or this component.")]
    [SerializeField] private Behaviour[] suspendDuringHit;

    [Header("Melee")]
    [SerializeField, Min(0f)] private float meleeHorizontalSpeed = 14f;
    [SerializeField, Min(0f)] private float meleeLiftSpeed = 3f;
    [SerializeField, Min(0.01f)] private float meleeStunTime = 0.55f;
    [SerializeField, Min(0f)] private float meleeDamping = 0.6f;

    [Header("Other Weapons")]
    [SerializeField, Min(0f)] private float weakPushSpeed = 2.5f;
    [SerializeField, Min(0.01f)] private float weakStunTime = 0.12f;
    [SerializeField, Min(0f)] private float weakDamping = 7f;
    [SerializeField, Min(0f)] private float weakRepeatInterval = 0.18f;

    [Header("Recovery")]
    [SerializeField] private bool gravityDuringHit = true;
    [SerializeField] private bool waitForGround = true;
    [SerializeField, Min(0.1f)] private float maxLandingWait = 1.4f;
    [SerializeField, Min(0.1f)] private float navMeshRecoveryRadius = 1.5f;

    [Header("Wall Impact")]
    [SerializeField] private LayerMask wallMask;
    [SerializeField, Min(0f)] private float minWallImpactSpeed = 5f;
    [SerializeField] private WallImpactDebris debrisEffect;

    private EnemyStats stats;
    private bool reacting;
    private bool strongReaction;
    private bool debrisCreated;
    private float recoverAt;
    private float forceRecoverAt;
    private float nextWeakHit;
    private float groundContactTime = -100f;
    private bool savedKinematic, savedGravity, savedAgentEnabled;
    private float savedDamping;
    private RigidbodyConstraints savedConstraints;
    private CollisionDetectionMode savedCollisionMode;
    private bool[] savedAIEnabled;
    private Vector3 lastNavPosition;
    private bool hadNavPosition;
    private Vector3 previousVelocity;

    public bool IsReacting => reacting;

    private void Awake()
    {
        stats = GetComponent<EnemyStats>();
        if (body == null) body = GetComponent<Rigidbody>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        // Existing ground enemy AI can be linked automatically.
        if (suspendDuringHit == null || suspendDuringHit.Length == 0)
        {
            EnemyAI groundAI = GetComponent<EnemyAI>();
            suspendDuringHit = groundAI != null
                ? new Behaviour[] { groundAI }
                : new Behaviour[0];
        }
    }

    public void ReceiveHit(Vector3 direction, bool melee)
    {
        if (stats == null || stats.IsDead || body == null) return;

        float now = Time.time;
        if (!melee)
        {
            if (reacting && strongReaction) return;
            if (now < nextWeakHit) return;
            nextWeakHit = now + weakRepeatInterval;
        }

        Vector3 flat = Vector3.ProjectOnPlane(direction, Vector3.up);
        if (flat.sqrMagnitude < 0.0001f)
            flat = Vector3.ProjectOnPlane(-transform.forward, Vector3.up);
        if (flat.sqrMagnitude < 0.0001f) flat = Vector3.forward;
        flat.Normalize();

        if (!reacting) BeginReaction();

        strongReaction = melee;
        if (melee) debrisCreated = false;
        float stun = melee ? meleeStunTime : weakStunTime;
        recoverAt = now + stun;
        forceRecoverAt = recoverAt + maxLandingWait;
        groundContactTime = -100f;

        body.linearDamping = melee ? meleeDamping : weakDamping;
        // VelocityChange is independent of enemy mass. Speeds are tunable
        // per prefab; re-hits replace velocity instead of stacking endlessly.
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        Vector3 impulse = flat * (melee ? meleeHorizontalSpeed : weakPushSpeed);
        if (melee) impulse += Vector3.up * meleeLiftSpeed;
        body.AddForce(impulse, ForceMode.VelocityChange);
        previousVelocity = impulse;
    }

    private void BeginReaction()
    {
        reacting = true;
        debrisCreated = false;
        savedKinematic = body.isKinematic;
        savedGravity = body.useGravity;
        savedDamping = body.linearDamping;
        savedConstraints = body.constraints;
        savedCollisionMode = body.collisionDetectionMode;
        savedAgentEnabled = agent != null && agent.enabled;
        hadNavPosition = savedAgentEnabled && agent.isOnNavMesh;
        if (hadNavPosition) lastNavPosition = agent.nextPosition;

        // Disable agent first; never have agent and dynamic body move the
        // same transform at the same time.
        if (savedAgentEnabled)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            agent.enabled = false;
        }

        savedAIEnabled = new bool[suspendDuringHit.Length];
        for (int i = 0; i < suspendDuringHit.Length; i++)
        {
            Behaviour ai = suspendDuringHit[i];
            if (ai == null || ai == this || ai == stats || ai == agent) continue;
            savedAIEnabled[i] = ai.enabled;
            if (ai.enabled) ai.enabled = false;
        }

        body.isKinematic = false;
        body.useGravity = gravityDuringHit;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.WakeUp();
    }

    private void FixedUpdate()
    {
        if (!reacting || body == null) return;

        previousVelocity = body.linearVelocity;
        float now = Time.time;
        if (now < recoverAt) return;

        bool grounded = now - groundContactTime <= 0.12f &&
                        body.linearVelocity.y <= 0.5f;
        if (strongReaction && gravityDuringHit && waitForGround &&
            !grounded && now < forceRecoverAt)
            return;

        if (stats == null || stats.IsDead)
        {
            reacting = false;
            Destroy(gameObject);
            return;
        }

        // Use the nearest valid NavMesh point, then fall back to the last
        // known valid position if the enemy was thrown off the walkable area.
        if (savedAgentEnabled && agent != null)
        {
            NavMeshQueryFilter filter = new NavMeshQueryFilter
            {
                agentTypeID = agent.agentTypeID,
                areaMask = agent.areaMask
            };
            bool found = NavMesh.SamplePosition(
                body.position, out NavMeshHit navHit, navMeshRecoveryRadius, filter);
            if (!found && now < forceRecoverAt) return;
            if (!found && hadNavPosition)
                found = NavMesh.SamplePosition(lastNavPosition, out navHit, 1f, filter);

            if (!found)
            {
                // Keep movement suspended and retry rather than enabling
                // an agent on an invalid surface.
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                recoverAt = now + 0.2f;
                return;
            }

            RestoreBody();
            body.position = navHit.position;
            transform.position = navHit.position;
            agent.enabled = true;

            if (!agent.isOnNavMesh || !agent.Warp(navHit.position))
            {
                agent.enabled = false;
                body.isKinematic = false;
                body.useGravity = false;
                recoverAt = now + 0.2f;
                return;
            }
            agent.isStopped = false;
        }
        else
        {
            RestoreBody();
        }

        reacting = false;
        RestoreAI();
    }

    private void RestoreBody()
    {
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        // ContinuousDynamic is not valid for a kinematic body.
        body.collisionDetectionMode = CollisionDetectionMode.Discrete;
        body.isKinematic = savedKinematic;
        body.useGravity = savedGravity;
        body.constraints = savedConstraints;
        body.linearDamping = savedDamping;
        body.collisionDetectionMode = savedCollisionMode;
    }

    private void RestoreAI()
    {
        if (savedAIEnabled == null) return;
        for (int i = 0; i < suspendDuringHit.Length; i++)
        {
            Behaviour ai = suspendDuringHit[i];
            if (ai != null && ai != this && ai != stats && ai != agent &&
                savedAIEnabled[i])
                ai.enabled = true;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        RecordGround(collision);
        if (!reacting || debrisCreated || debrisEffect == null) return;
        if ((wallMask.value & (1 << collision.gameObject.layer)) == 0) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            // Reject floor / ceiling contacts.
            if (Mathf.Abs(contact.normal.y) > 0.55f) continue;
            float speed = Mathf.Max(
                Mathf.Abs(Vector3.Dot(previousVelocity, contact.normal)),
                Mathf.Abs(Vector3.Dot(collision.relativeVelocity, contact.normal)));
            if (speed < minWallImpactSpeed) continue;

            debrisCreated = true;
            debrisEffect.Spawn(contact.point, contact.normal, speed);
            break;
        }
    }

    private void OnCollisionStay(Collision collision) => RecordGround(collision);

    private void RecordGround(Collision collision)
    {
        if (!reacting) return;
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.55f)
            {
                groundContactTime = Time.time;
                break;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, navMeshRecoveryRadius);
    }
}
