using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ClusterRocketProjectile : MonoBehaviour
{
    [Header("Rocket Movement")]
    [SerializeField] private float initialSpeed = 5f;
    [SerializeField] private float maxSpeed = 35f;
    [SerializeField] private float acceleration = 20f;

    [Header("Explosion Settings")]
    [Tooltip("충돌 시 폭발을 일으킬 대상 태그 목록")]
    [SerializeField] private List<string> impactTags = new List<string> { "Enemy", "Wall", "Floor" };
    [SerializeField] private float explosionRadius = 4f;
    [SerializeField] private float autoExplodeTime = 2f;

    [Header("Effect")]
    [SerializeField] private GameObject explosionVFX;

    // =========================================================
    // ✨ 클러스터 폭탄(자탄) 세팅 ✨
    // =========================================================
    [Header("Cluster Settings")]
    [Tooltip("분수처럼 뿜어질 자탄 프리팹")]
    [SerializeField] private ClusterBomb clusterPrefab;

    [Tooltip("한 번에 흩뿌릴 자탄의 개수")]
    [SerializeField] private int clusterCount = 6;

    [Tooltip("자탄의 폭발 데미지 (메인 로켓 데미지의 몇 배? 예: 0.3 = 30%)")]
    [SerializeField] private float clusterDamageMultiplier = 0.3f;

    [Tooltip("자탄이 위로 뿜어져 나가는 최소 힘 (높이)")]
    [SerializeField] private float minClusterEjectForce = 7f;

    [Tooltip("자탄이 위로 뿜어져 나가는 최대 힘 (높이)")]
    [SerializeField] private float maxClusterEjectForce = 15f;

    [Tooltip("자탄이 옆으로 퍼지는 정도 (0 = 수직 상승, 값이 클수록 넓게 퍼짐)")]
    [SerializeField, Range(0f, 2f)] private float horizontalSpread = 0.4f;
    // =========================================================


    private float currentSpeed;
    private float damage;
    private bool hasExploded;
    private GameObject owner;

    public void Launch(Vector3 direction, float attackDamage, GameObject projectileOwner)
    {
        direction.Normalize();
        transform.forward = direction;
        damage = attackDamage;
        owner = projectileOwner;
        currentSpeed = initialSpeed;
        hasExploded = false;

        Collider rocketCollider = GetComponent<Collider>();
        if (owner != null && rocketCollider != null)
        {
            Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
            foreach (Collider ownerCollider in ownerColliders)
            {
                if (ownerCollider == null) continue;
                Physics.IgnoreCollision(rocketCollider, ownerCollider, true);
            }
        }

        Invoke(nameof(Explode), autoExplodeTime);
    }

    private void Update()
    {
        if (hasExploded) return;
        currentSpeed += acceleration * Time.deltaTime;
        currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
        transform.position += transform.forward * currentSpeed * Time.deltaTime;
    }

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

    private void CheckImpact(
    GameObject hitObject,
    Collider hitCollider,
    Vector3 hitPoint
)
    {
        if (hasExploded ||
            hitObject == null)
        {
            return;
        }


        // ==========================================
        // 발사자 자신 제외
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


            Explode();

            return;
        }


        // ==========================================
        // Health / 지정 Tag
        // ==========================================

        IHealth health =
            hitObject.GetComponentInParent<IHealth>();

        if (health != null ||
            impactTags.Contains(
                hitObject.tag))
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        CancelInvoke(nameof(Explode));

        // 1. 메인 Explosion VFX
        if (explosionVFX != null)
        {
            GameObject vfx = Instantiate(explosionVFX, transform.position, Quaternion.identity);
            vfx.transform.localScale = Vector3.one * (explosionRadius * 2f);
            Destroy(vfx, 1f);
        }

        // 2. 메인 폭발 데미지
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        HashSet<IHealth> damagedTargets = new HashSet<IHealth>();

        HashSet<DoorController> affectedDoors = new HashSet<DoorController>();

        foreach (
        Collider hit in hitColliders)
        {
            if (hit == null)
                continue;


            // ==========================================
            // 발사자 제외
            // ==========================================

            if (owner != null &&
                (
                    hit.gameObject == owner ||
                    hit.transform.IsChildOf(
                        owner.transform
                    )
                ))
            {
                continue;
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
            // 기존 Damage
            // =====================================================

            IHealth health =
                hit.GetComponentInParent<IHealth>();


            if (health == null ||
                health.IsDead)
            {
                continue;
            }


            if (damagedTargets.Add(
                    health))
            {
                health.TakeDamage(
                    damage
                );
            }
        }

        // =====================================================
        // ✨ 3. 자탄(Cluster Bombs) 분수처럼 흩뿌리기 ✨
        // =====================================================
        if (clusterPrefab != null)
        {
            for (int i = 0; i < clusterCount; i++)
            {
                Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
                ClusterBomb bomb = Instantiate(clusterPrefab, spawnPos, Quaternion.identity);

                float subDamage = damage * clusterDamageMultiplier;

                // ✨ 수정됨: 최소~최대 사이의 랜덤한 폭발력을 계산하여 전달 ✨
                float randomForce = Random.Range(minClusterEjectForce, maxClusterEjectForce);

                // 퍼짐 정도(horizontalSpread)도 같이 넘겨줍니다.
                bomb.InitializeAndEject(subDamage, owner, impactTags, randomForce, horizontalSpread);
            }
        }

        Destroy(gameObject);
    }
}