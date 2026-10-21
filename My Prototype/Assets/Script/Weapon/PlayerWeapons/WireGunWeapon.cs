using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WireGunWeapon : WeaponBase
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private CharacterController controller;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Transform wireOrigin;
    [SerializeField] private LineRenderer wireLine;

    [Header("Wire")]
    [SerializeField] private float maxWireDistance = 40f;
    [SerializeField] private LayerMask wireHitMask = ~0;

    [Header("🔥 Swing Movement")]
    [SerializeField] private float swingGravity = 20f;
    [SerializeField] private float initialForwardBoost = 5f;
    [SerializeField] private float maxGroundHeightForBoost = 3f;
    [SerializeField] private float liftSpeed = 15f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float lengthFixGracePeriod = 0.1f;
    [SerializeField] private float swingRotationSpeed = 15f; // 스윙 중 부드러운 회전 속도

    [Header("🔥 Wire Physics (Elasticity)")]
    [SerializeField] private float wireSpringForce = 150f;
    [SerializeField] private float wireSpringDamper = 15f;
    [SerializeField] private float maxWireStretch = 2f;

    [Header("🔥 Swing Dash")]
    [SerializeField] private float swingDashForce = 15f;
    private bool canSwingDash;

    [Header("Enemy Pull")]
    [SerializeField] private float enemyPullSpeed = 12f;
    [SerializeField] private float enemyHoldDistance = 2f;

    [Header("Throw")]
    [SerializeField] private float enemyThrowSpeed = 20f;

    [Header("Wire Damage")]
    [SerializeField] private float wireDamageRadius = 0.25f;
    [SerializeField] private float wireDamageMultiplier = 0.5f;
    [SerializeField] private float wireDamageCooldown = 0.5f;

    private bool attachedToWorld;
    private Vector3 worldAnchor;
    private float currentWireLength;
    private Vector3 swingVelocity;

    private float remainingLiftHeight;
    private bool isWireLengthFixed;
    private float wireAttachTime;

    private EnemyWireTarget grabbedEnemy;
    private Dictionary<EnemyStats, float> nextDamageTime = new Dictionary<EnemyStats, float>();
    private PlayerMovement playerMovement;

    private void Awake()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (controller == null) controller = GetComponent<CharacterController>();
        if (playerStats == null) playerStats = GetComponent<PlayerStats>();

        if (wireLine != null)
        {
            wireLine.positionCount = 2;
            wireLine.useWorldSpace = true;
            wireLine.enabled = false;
        }
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (attachedToWorld || grabbedEnemy != null) ReleaseWire();
            else FireWire();
        }

        if (grabbedEnemy != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            ThrowEnemy();
        }

        if (attachedToWorld)
        {
            CheckSwingDash();
            HandleSwinging();
        }

        if (grabbedEnemy != null) PullEnemy();

        DamageEnemiesTouchingWire();
    }

    private void LateUpdate()
    {
        if (wireLine == null) return;

        if (!attachedToWorld && grabbedEnemy == null)
        {
            wireLine.enabled = false;
            return;
        }

        wireLine.enabled = true;
        wireLine.SetPosition(0, wireOrigin.position);

        if (grabbedEnemy != null) wireLine.SetPosition(1, grabbedEnemy.transform.position);
        else wireLine.SetPosition(1, worldAnchor);
    }

    private void FireWire()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (!Physics.Raycast(ray, out RaycastHit hit, maxWireDistance, wireHitMask, QueryTriggerInteraction.Ignore)) return;

        EnemyWireTarget enemy = hit.collider.GetComponentInParent<EnemyWireTarget>();
        if (enemy != null)
        {
            grabbedEnemy = enemy;
            grabbedEnemy.BeginGrab();
            return;
        }

        worldAnchor = hit.point;
        attachedToWorld = true;

        isWireLengthFixed = false;
        wireAttachTime = Time.time;
        remainingLiftHeight = 0f;
        canSwingDash = true;

        if (playerMovement != null) playerMovement.isSwingingByWeapon = true;

        Vector3 currentVelocity = controller.velocity;
        Vector3 boostDir = playerCamera.transform.forward;
        boostDir.y = 0;
        boostDir.Normalize();

        swingVelocity = currentVelocity + (boostDir * initialForwardBoost);

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit, maxGroundHeightForBoost, groundMask))
        {
            float neededHeight = maxGroundHeightForBoost - groundHit.distance;
            if (neededHeight > 0)
            {
                remainingLiftHeight = neededHeight;
            }
            swingVelocity.y = Mathf.Max(currentVelocity.y, 0f);
        }
        else
        {
            swingVelocity.y = currentVelocity.y;
        }
    }

    private void CheckSwingDash()
    {
        if (Keyboard.current == null || !canSwingDash) return;

        if (Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.rightShiftKey.wasPressedThisFrame)
        {
            Vector2 input = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;

            if (input.sqrMagnitude < 0.01f) return;

            Vector3 camForward = playerCamera.transform.forward;
            Vector3 camRight = playerCamera.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 flatDashDir = (camForward * input.y + camRight * input.x).normalized;
            Vector3 anchorToPlayer = (transform.position - worldAnchor).normalized;
            Vector3 arcDashDir = Vector3.ProjectOnPlane(flatDashDir, anchorToPlayer).normalized;

            float currentSpeed = swingVelocity.magnitude;
            swingVelocity = arcDashDir * (currentSpeed + swingDashForce);

            canSwingDash = false;
        }
    }

    // =========================================================
    // 🌟 통합된 단일 Move() 스윙 물리 로직 (멈춤 현상 완전 해결)
    // =========================================================
    private void HandleSwinging()
    {
        Vector3 frameMove = Vector3.zero;

        // 1. 부드러운 상승(Lift) 처리
        if (remainingLiftHeight > 0f)
        {
            float liftStep = liftSpeed * Time.deltaTime;
            if (liftStep > remainingLiftHeight) liftStep = remainingLiftHeight;

            frameMove += Vector3.up * liftStep;
            remainingLiftHeight -= liftStep;
            swingVelocity.y = Mathf.Max(swingVelocity.y, 0f);

            if (remainingLiftHeight <= 0f)
            {
                wireAttachTime = Time.time;
            }
        }
        else
        {
            // 중력 적용
            swingVelocity.y -= swingGravity * Time.deltaTime;

            if (controller.isGrounded)
            {
                float currentSpeed = swingVelocity.magnitude;
                swingVelocity.y = Mathf.Max(swingVelocity.y, 0f);

                Vector3 flatDir = new Vector3(swingVelocity.x, 0f, swingVelocity.z).normalized;
                if (flatDir.sqrMagnitude > 0f)
                {
                    swingVelocity = flatDir * currentSpeed;
                }

                if (isWireLengthFixed)
                {
                    float currentDistanceToAnchor = Vector3.Distance(transform.position, worldAnchor);
                    if (currentDistanceToAnchor < currentWireLength)
                    {
                        currentWireLength = currentDistanceToAnchor;
                    }
                }
            }
        }

        // 2. 스프링 장력 계산
        Vector3 expectedPosition = transform.position + (swingVelocity * Time.deltaTime);
        Vector3 offsetFromAnchor = expectedPosition - worldAnchor;
        float distance = offsetFromAnchor.magnitude;

        if (!isWireLengthFixed && remainingLiftHeight <= 0f)
        {
            if (Time.time - wireAttachTime >= lengthFixGracePeriod && swingVelocity.y <= 0f)
            {
                isWireLengthFixed = true;
                currentWireLength = Vector3.Distance(transform.position, worldAnchor);
            }
        }

        if (isWireLengthFixed && distance > currentWireLength)
        {
            float stretch = distance - currentWireLength;
            Vector3 ropeDir = offsetFromAnchor.normalized;

            if (stretch > maxWireStretch)
            {
                expectedPosition = worldAnchor + (ropeDir * (currentWireLength + maxWireStretch));
                swingVelocity = (expectedPosition - transform.position) / Time.deltaTime;
            }
            else
            {
                Vector3 springForce = -ropeDir * (stretch * wireSpringForce);
                float radialVelocity = Vector3.Dot(swingVelocity, ropeDir);
                Vector3 damperForce = -ropeDir * (radialVelocity * wireSpringDamper);

                swingVelocity += (springForce + damperForce) * Time.deltaTime;
            }
        }

        // 3. 속도 이동량 합산
        frameMove += swingVelocity * Time.deltaTime;

        // 🌟 단 한 번의 Move() 호출로 모든 충돌과 이동을 매끄럽게 처리
        CollisionFlags flags = controller.Move(frameMove);

        // 머리 충돌 시 추락
        if ((flags & CollisionFlags.Above) != 0)
        {
            remainingLiftHeight = 0f;
            if (swingVelocity.y > 0f) swingVelocity.y = -2f;
        }

        // 🌟 스윙 중 자연스러운 회전 (이동 방향을 부드럽게 바라봄)
        Vector3 horizontalVelocity = new Vector3(swingVelocity.x, 0f, swingVelocity.z);
        if (horizontalVelocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(horizontalVelocity);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, swingRotationSpeed * Time.deltaTime);
        }
    }

    private void PullEnemy()
    {
        Vector3 holdPoint = transform.position + Vector3.up * 1f + playerCamera.transform.forward * enemyHoldDistance;
        grabbedEnemy.PullTowards(holdPoint, enemyPullSpeed);
    }

    private void ThrowEnemy()
    {
        if (grabbedEnemy == null) return;
        Vector3 throwDirection = playerCamera.transform.forward;
        grabbedEnemy.Throw(throwDirection, enemyThrowSpeed);
        grabbedEnemy = null;
        attachedToWorld = false;
    }

    private void DamageEnemiesTouchingWire()
    {
        if (!attachedToWorld && grabbedEnemy == null) return;

        Vector3 endPoint = (grabbedEnemy != null) ? grabbedEnemy.transform.position : worldAnchor;
        Collider[] hits = Physics.OverlapCapsule(wireOrigin.position, endPoint, wireDamageRadius, ~0, QueryTriggerInteraction.Ignore);

        foreach (Collider hit in hits)
        {
            EnemyStats enemy = hit.GetComponentInParent<EnemyStats>();
            if (enemy == null || enemy.IsDead) continue;
            if (nextDamageTime.TryGetValue(enemy, out float nextTime) && Time.time < nextTime) continue;

            enemy.TakeDamage(playerStats.AttackPower * wireDamageMultiplier);
            nextDamageTime[enemy] = Time.time + wireDamageCooldown;
        }
    }

    private void ReleaseWire()
    {
        if (attachedToWorld)
        {
            attachedToWorld = false;
            remainingLiftHeight = 0f;
            canSwingDash = false;

            if (playerMovement != null)
            {
                playerMovement.ReceiveWireReleaseVelocity(swingVelocity);
                playerMovement.isSwingingByWeapon = false;
            }
        }

        if (grabbedEnemy != null)
        {
            grabbedEnemy.ReleaseGrab();
            grabbedEnemy = null;
        }

        if (wireLine != null)
        {
            wireLine.enabled = false;
        }

        swingVelocity = Vector3.zero;
    }

    public override void Unequip()
    {
        ReleaseWire();
        base.Unequip();
    }
}