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

        damage = projectileDamage;

        owner = projectileOwner;

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
        // =====================================================

        if (owner != null &&
            owner.GetComponent<PlayerStats>() != null)
        {
            EnemyStats enemyStats =
                hitObject.GetComponentInParent<EnemyStats>();


            if (enemyStats != null)
            {
                enemyStats.TakeDamage(
                    damage
                );
            }
        }


        // =====================================================
        // Enemy가 쏜 총알
        // =====================================================

        else if (owner != null &&
                 owner.GetComponent<EnemyStats>() != null)
        {
            PlayerStats playerStats =
                hitObject.GetComponentInParent<PlayerStats>();


            if (playerStats != null)
            {
                playerStats.TakeDamage(
                    damage
                );
            }
        }


        Destroy(gameObject);
    }
}