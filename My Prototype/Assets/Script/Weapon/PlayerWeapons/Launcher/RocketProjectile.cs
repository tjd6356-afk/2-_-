using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RocketProjectile : MonoBehaviour
{
    // =========================================================
    // Movement
    // =========================================================

    [Header("Rocket Movement")]

    [SerializeField]
    private float initialSpeed = 5f;


    [SerializeField]
    private float maxSpeed = 35f;


    [SerializeField]
    private float acceleration = 20f;


    // =========================================================
    // Explosion
    // =========================================================

    [Header("Explosion Settings")]

    [Tooltip(
        "충돌 시 폭발을 일으킬 대상 태그 목록"
    )]
    [SerializeField]
    private List<string> impactTags =
        new List<string>
        {
            "Enemy",
            "Wall",
            "Floor"
        };


    [SerializeField]
    private float explosionRadius = 4f;


    [SerializeField]
    private float autoExplodeTime = 2f;


    // =========================================================
    // Effect
    // =========================================================

    [Header("Effect")]

    [SerializeField]
    private GameObject explosionVFX;


    // =========================================================
    // Runtime
    // =========================================================

    private float currentSpeed;

    private float damage;

    private bool hasExploded;


    // 이 Rocket을 발사한 캐릭터
    private GameObject owner;


    // =========================================================
    // Launch
    // =========================================================

    public void Launch(
        Vector3 direction,
        float attackDamage,
        GameObject projectileOwner
    )
    {
        direction.Normalize();


        transform.forward =
            direction;


        damage =
            attackDamage;


        owner =
            projectileOwner;


        currentSpeed =
            initialSpeed;


        hasExploded =
            false;


        // ==========================================
        // 발사자와 Rocket Collider 충돌 무시
        // ==========================================

        Collider rocketCollider =
            GetComponent<Collider>();


        if (owner != null &&
            rocketCollider != null)
        {
            Collider[] ownerColliders =
                owner.GetComponentsInChildren<Collider>();


            foreach (
                Collider ownerCollider
                in ownerColliders)
            {
                if (ownerCollider == null)
                    continue;


                Physics.IgnoreCollision(
                    rocketCollider,
                    ownerCollider,
                    true
                );
            }
        }


        // 일정 시간 후 자동 폭발
        Invoke(
            nameof(Explode),
            autoExplodeTime
        );
    }


    // =========================================================
    // Movement
    // =========================================================

    private void Update()
    {
        if (hasExploded)
            return;


        // ==========================================
        // 가속
        // ==========================================

        currentSpeed +=
            acceleration *
            Time.deltaTime;


        currentSpeed =
            Mathf.Min(
                currentSpeed,
                maxSpeed
            );


        // ==========================================
        // 이동
        // ==========================================

        transform.position +=
            transform.forward *
            currentSpeed *
            Time.deltaTime;
    }


    // =========================================================
    // Trigger
    // =========================================================

    private void OnTriggerEnter(
        Collider other
    )
    {
        CheckImpact(
            other.gameObject
        );
    }


    // =========================================================
    // Collision
    // =========================================================

    private void OnCollisionEnter(
        Collision collision
    )
    {
        CheckImpact(
            collision.gameObject
        );
    }


    // =========================================================
    // Impact
    // =========================================================

    private void CheckImpact(
        GameObject hitObject
    )
    {
        if (hasExploded)
            return;


        if (hitObject == null)
            return;


        // ==========================================
        // 발사자 자신 무시
        // ==========================================

        if (owner != null)
        {
            if (hitObject == owner ||
                hitObject.transform.IsChildOf(
                    owner.transform
                ))
            {
                return;
            }
        }


        // ==========================================
        // Enemy / NPC처럼
        // IHealth가 있으면 바로 폭발
        // ==========================================

        IHealth health =
            hitObject.GetComponentInParent<IHealth>();


        if (health != null)
        {
            Explode();

            return;
        }


        // ==========================================
        // Wall / Floor 등의 지정 Tag
        // ==========================================

        if (impactTags.Contains(
                hitObject.tag))
        {
            Explode();
        }
    }


    // =========================================================
    // Explosion
    // =========================================================

    private void Explode()
    {
        if (hasExploded)
            return;


        hasExploded =
            true;


        CancelInvoke(
            nameof(Explode)
        );


        // =====================================================
        // 1. Explosion VFX
        // =====================================================

        if (explosionVFX != null)
        {
            GameObject vfx =
                Instantiate(
                    explosionVFX,
                    transform.position,
                    Quaternion.identity
                );


            vfx.transform.localScale =
                Vector3.one *
                (
                    explosionRadius *
                    2f
                );


            Destroy(
                vfx,
                1f
            );
        }


        // =====================================================
        // 2. 폭발 범위 탐색
        // =====================================================

        Collider[] hitColliders =
            Physics.OverlapSphere(
                transform.position,
                explosionRadius
            );


        // 같은 캐릭터에 Collider가 여러 개 있어도
        // 한 번만 데미지
        HashSet<IHealth> damagedTargets =
            new HashSet<IHealth>();


        foreach (
            Collider hit in hitColliders)
        {
            if (hit == null)
                continue;


            // ==========================================
            // 발사자 자신 제외
            // ==========================================

            if (owner != null)
            {
                if (hit.gameObject == owner ||
                    hit.transform.IsChildOf(
                        owner.transform
                    ))
                {
                    continue;
                }
            }


            // ==========================================
            // IHealth 탐색
            // ==========================================

            IHealth health =
                hit.GetComponentInParent<IHealth>();


            if (health == null)
                continue;


            if (health.IsDead)
                continue;


            // ==========================================
            // 같은 대상 중복 피해 방지
            // ==========================================

            if (!damagedTargets.Add(
                    health))
            {
                continue;
            }


            // ==========================================
            // 데미지
            // ==========================================

            health.TakeDamage(
                damage
            );


            Debug.Log(
                $"[Rocket] Explosion Damage : " +
                $"{damage} / " +
                $"{hit.transform.root.name}"
            );
        }


        // =====================================================
        // Rocket 제거
        // =====================================================

        Destroy(
            gameObject
        );
    }


    // =========================================================
    // Gizmo
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            explosionRadius
        );
    }
}