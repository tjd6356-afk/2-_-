using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class ClusterBomb : MonoBehaviour
{
    [Header("Explosion Settings")]
    [SerializeField] private float explosionRadius = 2.5f; // 자탄의 폭발 범위 (메인보다 작게)
    [SerializeField] private GameObject explosionVFX;      // 자탄용 작은 폭발 이펙트

    private float damage;
    private GameObject owner;
    private List<string> impactTags;
    private bool hasExploded = false;

    // 메인 로켓이 자탄을 생성할 때 정보와 함께 위로 튕겨냅니다.
    public void InitializeAndEject(float subDamage, GameObject projectileOwner, List<string> tags, float ejectForce)
    {
        damage = subDamage;
        owner = projectileOwner;
        impactTags = tags;

        Rigidbody rb = GetComponent<Rigidbody>();

        // 위쪽(Vector3.up)을 기준으로 부채꼴(분수) 모양으로 랜덤하게 퍼지도록 방향 계산
        Vector3 randomDir = Random.onUnitSphere;
        randomDir.y = Mathf.Abs(randomDir.y); // 무조건 위로 향하도록
        Vector3 ejectDirection = (Vector3.up * 1.5f + randomDir).normalized;

        // 분수처럼 솟구치게 힘 가하기
        rb.AddForce(ejectDirection * ejectForce, ForceMode.Impulse);

        // 3초 후에도 안 터지면 자동 폭발 (허공에 걸릴 경우 대비)
        Invoke(nameof(Explode), 3f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;

        // 발사자(플레이어) 자신과는 충돌 무시
        if (owner != null && (collision.gameObject == owner || collision.transform.IsChildOf(owner.transform)))
            return;

        // 적이거나 땅/벽이면 폭발
        IHealth health = collision.gameObject.GetComponentInParent<IHealth>();
        if (health != null || impactTags.Contains(collision.gameObject.tag))
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        CancelInvoke(nameof(Explode));

        // 1. 이펙트 생성
        if (explosionVFX != null)
        {
            GameObject vfx = Instantiate(explosionVFX, transform.position, Quaternion.identity);

            // 미니건 때 만들었던 ExplosionVFX 스크립트를 찾아 자탄의 폭발 크기를 넘겨줍니다!
            ExplosionVFX effectScript = vfx.GetComponent<ExplosionVFX>();
            if (effectScript != null)
            {
                effectScript.SetRadius(explosionRadius);
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

        // 3. 자탄 파괴
        Destroy(gameObject);
    }
}