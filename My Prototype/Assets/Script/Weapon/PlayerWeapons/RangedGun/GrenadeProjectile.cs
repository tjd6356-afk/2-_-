using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class GrenadeProjectile : MonoBehaviour
{
    [Header("Explosion Settings")]
    [Tooltip("충돌 후 폭발까지 걸리는 시간 (초)")]
    [SerializeField] private float explodeDelay = 2f;
    [SerializeField] private float explosionRadius = 4f;
    [SerializeField] private GameObject explosionVFX;

    private float damage;
    private GameObject owner;

    private bool hasImpacted = false;
    private bool hasExploded = false;

    // 무기 스크립트에서 수류탄을 던질 때 호출하는 함수
    public void Launch(Vector3 direction, float throwForce, float throwDamage, GameObject thrower)
    {
        damage = throwDamage;
        owner = thrower;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        rb.linearDamping = 0.5f;
        rb.angularDamping = 3.5f;

        // 플레이어(발사자)와 수류탄의 충돌 무시 (던지자마자 플레이어 몸에 부딪혀 튕기는 것 방지)
        Collider grenadeCollider = GetComponent<Collider>();
        if (owner != null && grenadeCollider != null)
        {
            Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
            foreach (Collider c in ownerColliders)
            {
                Physics.IgnoreCollision(grenadeCollider, c, true);
            }
        }

        // 수류탄 투척 힘 적용
        rb.AddForce(direction * throwForce, ForceMode.Impulse);

        // 자연스럽게 굴러가도록 회전력(토크) 추가
        rb.AddTorque(Random.insideUnitSphere * (throwForce * 0.5f), ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasImpacted || hasExploded) return;

        // 발사자 자신과의 충돌은 타이머를 시작하지 않음
        if (owner != null && (collision.gameObject == owner || collision.transform.IsChildOf(owner.transform)))
            return;

        // 어딘가에 부딪히는 순간 타이머 시작
        hasImpacted = true;
        Invoke(nameof(Explode), explodeDelay);
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        CancelInvoke(nameof(Explode));

        // 1. 폭발 이펙트 생성 (기존 ExplosionVFX 연동)
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

        // 2. 광역 데미지 판정 (HashSet으로 중복 피격 방지)
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

        // 수류탄 오브젝트 삭제
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}