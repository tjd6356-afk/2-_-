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
        if (other == null)
            return;


        Vector3 hitPoint =
            other.ClosestPoint(
                transform.position
            );


        CheckImpact(
            other.gameObject,
            other,
            hitPoint
        );
    }


    // =========================================================
    // Collision
    // =========================================================

    private void OnCollisionEnter(
        Collision collision
    )
    {
        if (collision == null)
            return;


        Vector3 hitPoint =
            collision.contactCount > 0
            ? collision.GetContact(0).point
            : transform.position;


        CheckImpact(
            collision.gameObject,
            collision.collider,
            hitPoint
        );
    }


    // =========================================================
    // Impact
    // =========================================================

    private void CheckImpact(
        GameObject hitObject,
        Collider hitCollider,
        Vector3 hitPoint
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
        // Door 직접 명중
        // ==========================================

        DoorController door =
            hitCollider != null
            ? hitCollider.GetComponentInParent<DoorController>()
            : hitObject.GetComponentInParent<DoorController>();


        if (door != null)
        {
            Vector3 rocketVelocity =
                transform.forward *
                currentSpeed;


            door.HitByProjectile(
                rocketVelocity,
                hitPoint
            );


            // 문에 맞았으면 바로 폭발
            Explode();

            return;
        }


        // ==========================================
        // Enemy / NPC
        // ==========================================

        IHealth health =
            hitObject.GetComponentInParent<IHealth>();


        if (health != null)
        {
            Explode();

            return;
        }


        // ==========================================
        // Wall / Floor
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
            GameObject vfx = Instantiate(explosionVFX, transform.position, Quaternion.identity);

            // ExplosionVFX 스크립트가 있다면 크기 설정
            ExplosionVFX effectScript = vfx.GetComponent<ExplosionVFX>();
            if (effectScript != null)
            {
                effectScript.SetRadius(explosionRadius);
            }
            else
            {
                // 스크립트가 없는 일반 파티클 이펙트일 경우 강제로 스케일 변경
                vfx.transform.localScale = Vector3.one * (explosionRadius * 2f);
                Destroy(vfx, 1f);
            }
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

        HashSet<DoorController> affectedDoors =
        new HashSet<DoorController>();

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


            // =====================================================
            // Door 폭발 충격
            // =====================================================

            DoorController door =
                hit.GetComponentInParent<DoorController>();


            if (door != null &&
                affectedDoors.Add(door))
            {
                Vector3 doorHitPoint =
                    hit.ClosestPoint(
                        transform.position
                    );


                // 폭발 중심이 Collider 내부인 경우
                if (
                    (
                        doorHitPoint -
                        transform.position
                    ).sqrMagnitude <
                    0.0001f
                )
                {
                    doorHitPoint =
                        hit.bounds.center;
                }


                float distance =
                    Vector3.Distance(
                        transform.position,
                        doorHitPoint
                    );


                float strength =
                    1f -
                    Mathf.Clamp01(
                        distance /
                        Mathf.Max(
                            0.01f,
                            explosionRadius
                        )
                    );


                door.HitByExplosion(
                    transform.position,
                    doorHitPoint,
                    strength
                );
            }


            // =====================================================
            // 기존 Health Damage
            // =====================================================

            IHealth health =
                hit.GetComponentInParent<IHealth>();


            if (health == null)
                continue;


            if (health.IsDead)
                continue;


            if (!damagedTargets.Add(
                    health))
            {
                continue;
            }


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