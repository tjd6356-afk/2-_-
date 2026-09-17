using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMeleeAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;
    [SerializeField] private Transform meleePoint;


    [Header("Melee Attack")]
    [Tooltip("근접 공격 범위")]
    [SerializeField] private float attackRadius = 1.2f;

    [Tooltip("Player AttackPower에 곱해지는 근접 공격 데미지 배율")]
    [SerializeField] private float meleeDamageMultiplier = 1f;

    [Tooltip("근접 공격 사이의 대기 시간")]
    [SerializeField] private float attackCooldown = 0.6f;

    [Tooltip("근접 공격으로 검사할 Layer")]
    [SerializeField] private LayerMask meleeLayerMask = ~0;


    private float nextAttackTime;


    private void Awake()
    {
        if (playerStats == null)
        {
            playerStats =
                GetComponent<PlayerStats>();
        }


        if (thirdPersonCamera == null &&
            Camera.main != null)
        {
            thirdPersonCamera =
                Camera.main.GetComponent<ThirdPersonCamera>();
        }
    }


    private void Update()
    {
        if (Mouse.current == null)
            return;


        // =====================================================
        // 우클릭 조준 중에는 근접공격 금지
        // =====================================================

        if (thirdPersonCamera != null &&
            thirdPersonCamera.IsAiming)
        {
            return;
        }


        // =====================================================
        // 일반 좌클릭
        // =====================================================

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryMeleeAttack();
        }
    }


    // =========================================================
    // 근접 공격 시도
    // =========================================================

    private void TryMeleeAttack()
    {
        if (Time.time < nextAttackTime)
            return;


        if (meleePoint == null)
            return;


        if (playerStats == null)
            return;


        nextAttackTime =
            Time.time +
            attackCooldown;


        PerformMeleeAttack();
    }


    // =========================================================
    // 실제 근접 공격
    // =========================================================

    private void PerformMeleeAttack()
    {
        float damage =
            playerStats.AttackPower *
            meleeDamageMultiplier;


        Collider[] hits =
            Physics.OverlapSphere(
                meleePoint.position,
                attackRadius,
                meleeLayerMask,
                QueryTriggerInteraction.Ignore
            );


        foreach (Collider hit in hits)
        {
            // =============================================
            // EnemyStats 찾기
            // =============================================

            EnemyStats enemyStats =
                hit.GetComponentInParent<EnemyStats>();


            if (enemyStats == null)
                continue;


            if (enemyStats.IsDead)
                continue;


            // =============================================
            // 데미지
            // =============================================

            enemyStats.TakeDamage(
                damage
            );


            Debug.Log(
                $"근접 공격 성공! {enemyStats.gameObject.name} / Damage : {damage}"
            );
        }
    }


    // =========================================================
    // Scene에서 공격 범위 표시
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (meleePoint == null)
            return;


        Gizmos.DrawWireSphere(
            meleePoint.position,
            attackRadius
        );
    }
}