using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WireGunWeapon : WeaponBase
{
    [Header("References")]
    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private CharacterController controller;

    [SerializeField]
    private PlayerStats playerStats;

    [SerializeField]
    private Transform wireOrigin;

    [SerializeField]
    private LineRenderer wireLine;


    [Header("Wire")]
    [SerializeField]
    private float maxWireDistance = 40f;

    [SerializeField]
    private LayerMask wireHitMask = ~0;


    [Header("Player Pull")]
    [SerializeField]
    private float playerPullSpeed = 14f;

    [SerializeField]
    private float stopDistance = 1.5f;


    [Header("Enemy Pull")]
    [SerializeField]
    private float enemyPullSpeed = 12f;

    [SerializeField]
    private float enemyHoldDistance = 2f;


    [Header("Throw")]
    [SerializeField]
    private float enemyThrowSpeed = 20f;


    [Header("Wire Damage")]
    [SerializeField]
    private float wireDamageRadius = 0.25f;

    [SerializeField]
    private float wireDamageMultiplier = 0.5f;

    [SerializeField]
    private float wireDamageCooldown = 0.5f;


    private bool attachedToWorld;

    private Vector3 worldAnchor;


    private EnemyWireTarget grabbedEnemy;


    private Dictionary<EnemyStats, float>
        nextDamageTime =
            new Dictionary<EnemyStats, float>();


    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;


        if (controller == null)
            controller =
                GetComponent<CharacterController>();


        if (playerStats == null)
            playerStats =
                GetComponent<PlayerStats>();


        if (wireLine != null)
        {
            wireLine.positionCount = 2;
            wireLine.useWorldSpace = true;
            wireLine.enabled = false;
        }
    }


    private void Update()
    {
        if (Mouse.current == null)
            return;


        // ==========================================
        // 좌클릭
        // ==========================================

        if (Mouse.current
            .leftButton
            .wasPressedThisFrame)
        {
            if (attachedToWorld ||
                grabbedEnemy != null)
            {
                ReleaseWire();
            }
            else
            {
                FireWire();
            }
        }


        // ==========================================
        // 잡은 Enemy 던지기
        // ==========================================

        if (grabbedEnemy != null &&
            Mouse.current
                .rightButton
                .wasPressedThisFrame)
        {
            ThrowEnemy();
        }


        if (attachedToWorld)
        {
            PullPlayer();
        }


        if (grabbedEnemy != null)
        {
            PullEnemy();
        }


        DamageEnemiesTouchingWire();
    }


    private void LateUpdate()
    {
        if (wireLine == null)
            return;


        if (!attachedToWorld &&
            grabbedEnemy == null)
        {
            wireLine.enabled =
                false;

            return;
        }


        wireLine.enabled =
            true;


        wireLine.SetPosition(
            0,
            wireOrigin.position
        );


        if (grabbedEnemy != null)
        {
            wireLine.SetPosition(
                1,
                grabbedEnemy
                    .transform
                    .position
            );
        }
        else
        {
            wireLine.SetPosition(
                1,
                worldAnchor
            );
        }
    }


    private void FireWire()
    {
        Ray ray =
            playerCamera.ViewportPointToRay(
                new Vector3(
                    0.5f,
                    0.5f,
                    0f
                )
            );


        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                maxWireDistance,
                wireHitMask,
                QueryTriggerInteraction.Ignore))
        {
            return;
        }


        EnemyWireTarget enemy =
            hit.collider
                .GetComponentInParent
                <EnemyWireTarget>();


        if (enemy != null)
        {
            grabbedEnemy =
                enemy;


            grabbedEnemy.BeginGrab();

            return;
        }


        worldAnchor =
            hit.point;


        attachedToWorld =
            true;
    }


    private void PullPlayer()
    {
        Vector3 toAnchor =
            worldAnchor -
            transform.position;


        float distance =
            toAnchor.magnitude;


        if (distance <=
            stopDistance)
        {
            return;
        }


        controller.Move(
            toAnchor.normalized *
            playerPullSpeed *
            Time.deltaTime
        );
    }


    private void PullEnemy()
    {
        Vector3 holdPoint =
            transform.position +
            Vector3.up *
            1f +
            playerCamera.transform.forward *
            enemyHoldDistance;


        grabbedEnemy.PullTowards(
            holdPoint,
            enemyPullSpeed
        );
    }


    private void ThrowEnemy()
    {
        if (grabbedEnemy == null)
            return;


        /*
         * 카메라의 정면 =
         * 화면 중앙 십자선 방향
         */
        Vector3 throwDirection =
            playerCamera
                .transform
                .forward;


        grabbedEnemy.Throw(
            throwDirection,
            enemyThrowSpeed
        );


        grabbedEnemy = null;

        attachedToWorld = false;
    }


    private void DamageEnemiesTouchingWire()
    {
        if (!attachedToWorld &&
            grabbedEnemy == null)
        {
            return;
        }


        Vector3 endPoint;


        if (grabbedEnemy != null)
        {
            endPoint =
                grabbedEnemy
                    .transform
                    .position;
        }
        else
        {
            endPoint =
                worldAnchor;
        }


        Collider[] hits =
            Physics.OverlapCapsule(
                wireOrigin.position,
                endPoint,
                wireDamageRadius,
                ~0,
                QueryTriggerInteraction.Ignore
            );


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


            if (nextDamageTime
                .TryGetValue(
                    enemy,
                    out float nextTime))
            {
                if (Time.time <
                    nextTime)
                {
                    continue;
                }
            }


            float damage =
                playerStats.AttackPower *
                wireDamageMultiplier;


            enemy.TakeDamage(
                damage
            );


            nextDamageTime[enemy] =
                Time.time +
                wireDamageCooldown;
        }
    }


    private void ReleaseWire()
    {
        attachedToWorld =
            false;


        if (grabbedEnemy != null)
        {
            grabbedEnemy.ReleaseGrab();

            grabbedEnemy = null;
        }


        if (wireLine != null)
        {
            wireLine.enabled =
                false;
        }
    }


    public override void Unequip()
    {
        ReleaseWire();

        base.Unequip();
    }
}