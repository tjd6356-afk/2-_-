using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RocketProjectile : MonoBehaviour
{
    [Header("Rocket Movement")]
    [SerializeField] private float initialSpeed = 5f;     // 처음 발사 속도
    [SerializeField] private float maxSpeed = 35f;        // 최대 가속 속도
    [SerializeField] private float acceleration = 20f;    // 초당 가속되는 정도

    [Header("Explosion Settings")]
    [Tooltip("충돌 시 폭발을 일으킬 대상 태그 목록 (예: Enemy, Environment, Ground 등)")]
    [SerializeField] private List<string> impactTags = new List<string> { "Enemy", "Wall", "Floor" };

    [SerializeField] private float explosionRadius = 4f;  // 폭발 범위
    [SerializeField] private float autoExplodeTime = 2f;  // 자동 폭발 시간

    [Header("Effect")]
    [Tooltip("폭발 시 생성될 파티클 프리팹 (선택)")]
    [SerializeField] private GameObject explosionVFX;

    // 런처에서 전달받을 변수들
    private float currentSpeed;
    private float damage;
    private bool hasExploded;

    public void Launch(Vector3 direction, float attackDamage, GameObject owner)
    {
        // 방향 설정 (런처에서 LookRotation으로 맞췄지만 확실하게 한 번 더 적용)
        transform.forward = direction;

        damage = attackDamage;
        currentSpeed = initialSpeed;
        hasExploded = false;

        // 발사 후 2초 뒤 자동 폭발 실행
        Invoke(nameof(Explode), autoExplodeTime);
    }

    private void Update()
    {
        if (hasExploded) return;

        // 1. 점점 빨라지는 가속 로직
        currentSpeed += acceleration * Time.deltaTime;
        currentSpeed = Mathf.Min(currentSpeed, maxSpeed); // 최대 속도 제한

        // 2. 앞으로 이동
        transform.position += transform.forward * currentSpeed * Time.deltaTime;
    }

    // Trigger나 Collision 둘 다 대응 가능하도록 설정 (Collider의 IsTrigger 체크 여부에 따라 다름)
    private void OnTriggerEnter(Collider other)
    {
        CheckImpact(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        CheckImpact(collision.gameObject);
    }

    private void CheckImpact(GameObject hitObject)
    {
        if (hasExploded) return;

        // 태그 목록에 포함된 물체에 부딪혔을 때만 폭발
        if (impactTags.Contains(hitObject.tag))
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        // 1. 시각 효과 (VFX) 생성 및 크기 조절
        if (explosionVFX != null)
        {
            GameObject vfx = Instantiate(explosionVFX, transform.position, Quaternion.identity);

            // 폭발 범위(반지름)를 지름으로 변환하여 스케일 적용
            // 파티클의 Scaling Mode가 Local이나 Hierarchy일 때 정상 작동합니다.
            vfx.transform.localScale = Vector3.one * (explosionRadius * 2f);

            // 파티클이 재생된 후 쓰레기가 쌓이지 않도록 1초 뒤 자동 삭제
            Destroy(vfx, 1f);
        }

        // 2. 폭발 범위 내의 적(EnemyStats) 찾아서 데미지 적용
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        HashSet<EnemyStats> damagedEnemies = new HashSet<EnemyStats>();

        foreach (Collider hit in hitColliders)
        {
            EnemyStats enemy = hit.GetComponentInParent<EnemyStats>();

            if (enemy != null && !enemy.IsDead && damagedEnemies.Add(enemy))
            {
                enemy.TakeDamage(damage);
            }
        }

        // 폭발 로직이 끝났으므로 로켓 오브젝트 제거
        Destroy(gameObject);
    }

    // 에디터에서 폭발 범위를 시각적으로 볼 수 있게 해주는 기능
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}