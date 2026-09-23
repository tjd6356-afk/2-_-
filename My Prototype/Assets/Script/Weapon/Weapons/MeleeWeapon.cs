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
        if (playerStats == null)
        {
            playerStats =
                GetComponent<PlayerStats>();
        }
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


        HashSet<EnemyStats> hitEnemies =
            new HashSet<EnemyStats>();


        foreach (Collider hit in hits)
        {
            EnemyStats enemy =
                hit.GetComponentInParent
                <EnemyStats>();


            if (enemy == null ||
                enemy.IsDead)
            {
                continue;
            }


            if (!hitEnemies.Add(enemy))
                continue;


            enemy.TakeDamage(
                damage
            );
        }


        Debug.Log(
            $"Melee Combo {comboStep} / Damage {damage}"
        );
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