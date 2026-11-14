using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MeleeWeapon : WeaponBase
{
    [Header("References")]
    [SerializeField]
    private PlayerStats playerStats;

    [SerializeField]
    private Transform attackPoint;


    [Header("Hit")]
    [SerializeField]
    private float attackRadius = 1.3f;

    [SerializeField]
    private LayerMask enemyMask = ~0;


    [Header("Combo Damage")]
    [SerializeField]
    private float attack1Multiplier = 1f;

    [SerializeField]
    private float attack2Multiplier = 1.25f;

    [SerializeField]
    private float attack3Multiplier = 1.7f;


    [Header("Combo Timing")]
    [Tooltip("1타와 2타, 2타와 3타 사이 기본 간격")]
    [SerializeField]
    private float baseAttackInterval = 0.4f;

    [Tooltip("3타 이후 기본 후딜")]
    [SerializeField]
    private float finalRecoveryTime = 1f;

    [Tooltip("너무 오래 공격하지 않으면 1타부터 다시 시작")]
    [SerializeField]
    private float comboResetTime = 1f;


    private int comboStep;

    private float nextAttackTime;

    private float lastAttackTime;


    private void Awake()
    {
        FindPlayerStats();
    }


    private void Update()
    {
        if (Mouse.current == null)
            return;


        if (Mouse.current
            .leftButton
            .wasPressedThisFrame)
        {
            TryAttack();
        }
    }


    private void TryAttack()
    {
        if (playerStats == null)
        {
            FindPlayerStats();


            if (playerStats == null)
            {
                Debug.LogError(
                    "[MeleeWeapon] 공격 실패 : PlayerStats가 없습니다."
                );

                return;
            }
        }

        if (Time.time <
            nextAttackTime)
        {
            return;
        }


        float speed =
            Mathf.Max(
                0.1f,
                playerStats.MeleeAttackSpeed
            );


        float resetTime =
            comboResetTime /
            speed;


        // 너무 늦게 다음 공격을 눌렀으면
        // 다시 1타부터
        if (Time.time -
            lastAttackTime >
            resetTime)
        {
            comboStep = 0;
        }


        comboStep++;


        float damageMultiplier;


        switch (comboStep)
        {
            case 1:
                damageMultiplier =
                    attack1Multiplier;
                break;

            case 2:
                damageMultiplier =
                    attack2Multiplier;
                break;

            default:
                damageMultiplier =
                    attack3Multiplier;
                break;
        }


        PerformAttack(
            damageMultiplier
        );


        lastAttackTime =
            Time.time;


        // ==========================================
        // 3타 완료
        // ==========================================

        if (comboStep >= 3)
        {
            comboStep = 0;


            nextAttackTime =
                Time.time +
                (
                    finalRecoveryTime /
                    speed
                );
        }

        // ==========================================
        // 다음 콤보
        // ==========================================

        else
        {
            nextAttackTime =
                Time.time +
                (
                    baseAttackInterval /
                    speed
                );
        }
    }


    private void PerformAttack(
    float multiplier
)
    {
        if (attackPoint == null)
            return;


        // ==========================================
        // PlayerStats 확인
        // ==========================================

        if (playerStats == null)
        {
            FindPlayerStats();


            if (playerStats == null)
                return;
        }


        float damage =
            playerStats.AttackPower *
            multiplier;


        Collider[] hits =
            Physics.OverlapSphere(
                attackPoint.position,
                attackRadius,
                enemyMask,
                QueryTriggerInteraction.Ignore
            );


        // ==========================================
        // 같은 대상 중복 타격 방지
        // ==========================================

        HashSet<IHealth> hitTargets =
            new HashSet<IHealth>();


        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;


            IHealth health =
                hit.GetComponentInParent<IHealth>();


            if (health == null)
                continue;


            // ==========================================
            // 자기 Player는 공격하지 않음
            // ==========================================

            PlayerStats hitPlayer =
                hit.GetComponentInParent<PlayerStats>();


            if (hitPlayer == playerStats)
                continue;


            // ==========================================
            // 이미 한 번 맞은 대상
            // ==========================================

            if (!hitTargets.Add(
                    health))
            {
                continue;
            }


            if (health.IsDead)
                continue;


            health.TakeDamage(
                damage
            );
        }


        Debug.Log(
            $"Melee Combo {comboStep} / Damage {damage}"
        );
    }

    private void FindPlayerStats()
    {
        if (playerStats != null)
            return;


        // 1순위:
        // 장착된 Weapon의 부모에서 PlayerStats 검색
        playerStats =
            GetComponentInParent<PlayerStats>();


        // 2순위:
        // 그래도 없으면 Scene의 PlayerStats 검색
        if (playerStats == null)
        {
            playerStats =
                FindFirstObjectByType<PlayerStats>();
        }


        if (playerStats != null)
        {
            Debug.Log(
                $"[MeleeWeapon] PlayerStats 자동 연결 성공 : " +
                $"{playerStats.gameObject.name}"
            );
        }
        else
        {
            Debug.LogError(
                "[MeleeWeapon] PlayerStats를 찾지 못했습니다."
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;


        Gizmos.DrawWireSphere(
            attackPoint.position,
            attackRadius
        );
    }
}