using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class ClusterBomb : MonoBehaviour
{
    [Header("Explosion Settings")]
    [SerializeField] private float explosionRadius = 2.5f;
    [SerializeField] private GameObject explosionVFX;

    private float damage;
    private GameObject owner;
    private List<string> impactTags;
    private bool hasExploded = false;

    // ✨ 퍼짐 정도(horizontalSpread) 인자가 추가되었습니다.
    public void InitializeAndEject(float subDamage, GameObject projectileOwner, List<string> tags, float ejectForce, float horizontalSpread)
    {
        damage = subDamage;
        owner = projectileOwner;
        impactTags = tags;

        Rigidbody rb = GetComponent<Rigidbody>();

        // ✨ 수정됨: 무조건 위쪽(Up)을 향하되, 수평(X, Z)으로 퍼지는 정도를 제한합니다.
        Vector2 randomCircle = Random.insideUnitCircle; // 2D 원 안의 랜덤 좌표
        Vector3 horizontalDir = new Vector3(randomCircle.x, 0f, randomCircle.y) * horizontalSpread;

        // 최종 분출 방향 = (완벽한 위쪽 + 좁게 제한된 수평 방향)
        Vector3 ejectDirection = (Vector3.up + horizontalDir).normalized;

        // 전달받은 랜덤한 힘(ejectForce)으로 튕겨냅니다.
        rb.AddForce(ejectDirection * ejectForce, ForceMode.Impulse);

        Invoke(nameof(Explode), 3f);
    }

    // ✨ Trigger(통과형) 바닥/적에 닿았을 때도 터지도록 추가
    private void OnTriggerEnter(Collider other)
    {
        CheckImpact(other.gameObject);
    }

    // ✨ 일반적인 물리 바닥/적에 닿았을 때 터지도록 유지
    private void OnCollisionEnter(Collision collision)
    {
        CheckImpact(collision.gameObject);
    }

    // 충돌 판정 통합 함수
    private void CheckImpact(GameObject hitObject)
    {
        if (hasExploded) return;

        // 발사자(플레이어) 자신과는 충돌 무시
        if (owner != null && (hitObject == owner || hitObject.transform.IsChildOf(owner.transform)))
            return;

        // 적(IHealth)이거나, 지정된 태그(Floor, Wall 등)에 닿으면 폭발
        IHealth health = hitObject.GetComponentInParent<IHealth>();
        if (health != null || (impactTags != null && impactTags.Contains(hitObject.tag)))
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        CancelInvoke(nameof(Explode));

        // 1. 이펙트 생성 (폭발 범위 연동 포함)
        if (explosionVFX != null)
        {
            GameObject vfx = Instantiate(explosionVFX, transform.position, Quaternion.identity);

            ExplosionVFX effectScript = vfx.GetComponent<ExplosionVFX>();
            if (effectScript != null)
            {
                effectScript.SetRadius(explosionRadius);
            }
            else
            {
                vfx.transform.localScale = Vector3.one * (explosionRadius * 2f);
                Destroy(vfx, 1f);
            }
        }

        // 2. 광역 데미지
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        HashSet<IHealth> damagedTargets = new HashSet<IHealth>();

        foreach (Collider hit in hitColliders)
        {
            if (owner != null && (hit.gameObject == owner || hit.transform.IsChildOf(owner.transform)))
                continue;

            IHealth health = hit.GetComponentInParent<IHealth>();
            if (health != null && !health.IsDead)
            {
                if (damagedTargets.Add(health))
                {
                    health.TakeDamage(damage);
                }
            }
        }

        Destroy(gameObject);
    }
}