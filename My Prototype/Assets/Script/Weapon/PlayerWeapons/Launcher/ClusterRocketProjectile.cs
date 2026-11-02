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

    [Tooltip("자탄이 위로 뿜어져 나가는 힘")]
    [SerializeField] private float clusterEjectForce = 10f;
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

    private void OnTriggerEnter(Collider other) => CheckImpact(other.gameObject);
    private void OnCollisionEnter(Collision collision) => CheckImpact(collision.gameObject);

    private void CheckImpact(GameObject hitObject)
    {
        if (hasExploded || hitObject == null) return;

        if (owner != null)
        {
            if (hitObject == owner || hitObject.transform.IsChildOf(owner.transform)) return;
        }

        IHealth health = hitObject.GetComponentInParent<IHealth>();
        if (health != null || impactTags.Contains(hitObject.tag))
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

        foreach (Collider hit in hitColliders)
        {
            if (hit == null) continue;

            if (owner != null && (hit.gameObject == owner || hit.transform.IsChildOf(owner.transform))) continue;

            IHealth health = hit.GetComponentInParent<IHealth>();
            if (health == null || health.IsDead) continue;

            if (damagedTargets.Add(health))
            {
                health.TakeDamage(damage);
            }
        }

        // =====================================================
        // ✨ 3. 자탄(Cluster Bombs) 분수처럼 흩뿌리기 ✨
        // =====================================================
        if (clusterPrefab != null)
        {
            for (int i = 0; i < clusterCount; i++)
            {
                // 약간 위쪽에서 겹치지 않게 생성
                Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
                ClusterBomb bomb = Instantiate(clusterPrefab, spawnPos, Quaternion.identity);

                // 자탄 초기화 및 발사
                float subDamage = damage * clusterDamageMultiplier;
                bomb.InitializeAndEject(subDamage, owner, impactTags, clusterEjectForce);
            }
        }

        Destroy(gameObject);
    }
}