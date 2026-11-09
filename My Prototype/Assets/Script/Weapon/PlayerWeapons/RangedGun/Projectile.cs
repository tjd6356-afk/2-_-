using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private float speed = 30f;
    [SerializeField] private float lifeTime = 5f;

    private Rigidbody rb;

    private float damage;

    // 이 총알을 발사한 캐릭터
    private GameObject owner;


    public float Damage => damage;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }


    private void Start()
    {
        Destroy(
            gameObject,
            lifeTime
        );
    }


    // =========================================================
    // 발사
    // =========================================================

    public void Launch(
    Vector3 direction,
    float projectileDamage,
    GameObject projectileOwner
)
    {
        direction.Normalize();


        damage =
            projectileDamage;


        owner =
            projectileOwner;


        // ==========================================
        // 발사자와 총알의 충돌 자체를 무시
        // ==========================================

        Collider projectileCollider =
            GetComponent<Collider>();


        if (owner != null &&
            projectileCollider != null)
        {
            Collider[] ownerColliders =
                owner.GetComponentsInChildren<Collider>();


            foreach (Collider ownerCollider in ownerColliders)
            {
                Physics.IgnoreCollision(
                    projectileCollider,
                    ownerCollider,
                    true
                );
            }
        }


        rb.linearVelocity =
            direction *
            speed;
    }


    // =========================================================
    // 충돌
    // =========================================================

    private void OnCollisionEnter(
        Collision collision
    )
    {
        GameObject hitObject =
            collision.gameObject;


        // =====================================================
        // 자기 자신을 쏘는 것 방지
        // =====================================================

        if (owner != null)
        {
            if (hitObject == owner ||
                hitObject.transform.IsChildOf(owner.transform))
            {
                return;
            }
        }


        // =====================================================
        // Player가 쏜 총알
        //
        // EnemyStats / NPCStats처럼
        // IHealth를 가진 대상에게 피해
        // =====================================================

        if (owner != null &&
            owner.GetComponent<PlayerStats>() != null)
        {
            IHealth health =
                hitObject.GetComponentInParent<IHealth>();


            if (health != null)
            {
                health.TakeDamage(
                    damage
                );
            }
        }


        // =====================================================
        // Enemy가 쏜 총알
        // Player 또는 NPC에게 데미지
        // =====================================================

        else if (owner != null &&
                 owner.GetComponent<EnemyStats>() != null)
        {
            // ==========================================
            // Player 확인
            // ==========================================

            PlayerStats playerStats =
                hitObject.GetComponentInParent<PlayerStats>();


            if (playerStats != null)
            {
                playerStats.TakeDamage(
                    damage
                );


                Destroy(
                    gameObject
                );

                return;
            }


            // ==========================================
            // NPC 확인
            // ==========================================

            NPCStats npcStats =
                hitObject.GetComponentInParent<NPCStats>();


            if (npcStats != null)
            {
                npcStats.TakeDamage(
                    damage
                );


                Destroy(
                    gameObject
                );

                return;
            }
        }


        Destroy(
            gameObject
        );
    }
}