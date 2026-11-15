using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RoomBattleController : MonoBehaviour
{
    // =========================================================
    // Monster
    // =========================================================

    [Header("Monster Prefabs")]

    [Tooltip("이 방에서 랜덤으로 생성될 몬스터 프리팹")]
    [SerializeField]
    private List<GameObject> monsterPrefabs =
        new List<GameObject>();


    [Tooltip("생성할 몬스터 수")]
    [SerializeField]
    private int monstersToSpawn = 3;

    private bool battleDisabled;

    // =========================================================
    // Spawn Area
    // =========================================================

    [Header("Monster Spawn Area")]

    [Tooltip("몬스터 생성 범위의 중심이 되는 Empty")]
    [SerializeField]
    private Transform monsterSpawnArea;


    [Tooltip("몬스터가 생성될 X / Y / Z 범위")]
    [SerializeField]
    private Vector3 spawnAreaSize =
        new Vector3(
            10f,
            1f,
            10f
        );


    [Tooltip("바닥을 찾기 위해 위에서 Raycast를 쏘는 높이")]
    [SerializeField]
    private float groundRayHeight = 5f;


    [Tooltip("몬스터가 생성될 바닥 Layer")]
    [SerializeField]
    private LayerMask groundMask = ~0;


    [Tooltip("몬스터끼리 너무 겹치지 않도록 하는 최소 거리")]
    [SerializeField]
    private float minimumMonsterSpacing = 1.5f;


    [Tooltip("적절한 Spawn 위치를 찾는 최대 시도 횟수")]
    [SerializeField]
    private int maxSpawnPositionAttempts = 20;

    // =========================================================
    // Monster Spawn Presentation
    // =========================================================

    [Header("Monster Spawn Presentation")]

    [Tooltip("몬스터가 최종 Spawn 위치보다 얼마나 높은 곳에서 나타날지")]
    [SerializeField]
    private float monsterSpawnHeight = 20f;


    [Tooltip("착지 후 AI가 활성화되기 전 최소 대기시간")]
    [SerializeField]
    private float monsterReadyDelayMin = 0.5f;


    [Tooltip("착지 후 AI가 활성화되기 전 최대 대기시간")]
    [SerializeField]
    private float monsterReadyDelayMax = 0.7f;

    [Tooltip("혹시 착지를 감지하지 못했을 때 무한 대기를 막는 시간")]
    [SerializeField]
    private float maximumFallTime = 5f;

    // =========================================================
    // Battle Barrier
    // =========================================================

    [Header("Battle Barrier")]

    [Tooltip("전투 시작 시 입구를 막을 벽 Prefab")]
    [SerializeField]
    private GameObject barrierPrefab;


    [Tooltip("벽을 생성할 위치")]
    [SerializeField]
    private List<Transform> barrierSpawnPoints =
        new List<Transform>();


    // =========================================================
    // Debug
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool drawSpawnArea = true;


    // =========================================================
    // 이 Room의 전투 시스템 완전 비활성화
    // Player 시작방 / StageExit 방에서 사용
    // =========================================================

    public void DisableBattleRoom()
    {
        battleDisabled = true;

        // 혹시 실행 중인 감시 Coroutine이 있으면 종료
        if (monsterCheckRoutine != null)
        {
            StopCoroutine(monsterCheckRoutine);
            monsterCheckRoutine = null;
        }

        // 같은 오브젝트에 붙어있는 Trigger Collider 비활성화
        Collider[] colliders =
            GetComponents<Collider>();

        foreach (Collider col in colliders)
        {
            if (col != null &&
                col.isTrigger)
            {
                col.enabled = false;
            }
        }

        Debug.Log(
            $"[RoomBattle] Battle 비활성화 : {transform.root.name}"
        );

        // RoomBattleController 자체도 더 이상 Update/Trigger 처리 안 함
        enabled = false;
    }


    // =========================================================
    // Runtime
    // =========================================================

    private bool battleStarted;

    private bool battleCompleted;


    private readonly List<GameObject> aliveMonsters =
        new List<GameObject>();


    private readonly List<GameObject> spawnedBarriers =
        new List<GameObject>();


    private Transform runtimeRoot;

    private Coroutine monsterCheckRoutine;


    public bool BattleStarted =>
        battleStarted;


    public bool BattleCompleted =>
        battleCompleted;


    // =========================================================
    // Player Trigger
    // =========================================================

    private void OnTriggerEnter(
        Collider other
    )
    {
        if (battleDisabled)
            return;

        // 이미 진행했거나 완료한 방
        if (battleStarted ||
            battleCompleted)
        {
            return;
        }


        // 기존 프로젝트의 PlayerStats를 이용해서
        // Player인지 확인
        PlayerStats player =
            other.GetComponentInParent<PlayerStats>();


        if (player == null)
            return;


        Debug.Log(
            $"[RoomBattle] Player 진입 : {transform.root.name}"
        );


        StartBattle();
    }


    // =========================================================
    // Battle Start
    // =========================================================

    private void StartBattle()
    {
        if (battleStarted ||
            battleCompleted)
        {
            return;
        }


        battleStarted =
            true;


        CreateRuntimeRoot();


        // ==========================================
        // 입구 먼저 막기
        // ==========================================

        SpawnBarriers();


        // ==========================================
        // Monster 생성
        // ==========================================

        SpawnMonsters();


        // 생성된 적이 하나도 없다면
        // 바로 방 클리어
        if (aliveMonsters.Count == 0)
        {
            Debug.LogWarning(
                $"[RoomBattle] 생성된 Monster가 없습니다 : {transform.root.name}"
            );


            CompleteBattle();

            return;
        }


        // ==========================================
        // Monster 사망 감시
        // ==========================================

        monsterCheckRoutine =
            StartCoroutine(
                CheckMonstersRoutine()
            );


        Debug.Log(
            $"[RoomBattle] 전투 시작 / Monster : {aliveMonsters.Count}"
        );
    }


    // =========================================================
    // Runtime Root
    // =========================================================

    private void CreateRuntimeRoot()
    {
        if (runtimeRoot != null)
            return;


        GameObject root =
            new GameObject(
                "BattleRuntime"
            );


        Transform parent =
            transform.parent != null
                ? transform.parent
                : transform;


        root.transform.SetParent(
            parent
        );


        root.transform.localPosition =
            Vector3.zero;


        root.transform.localRotation =
            Quaternion.identity;


        runtimeRoot =
            root.transform;
    }


    // =========================================================
    // Monster Spawn
    // =========================================================

    private void SpawnMonsters()
    {
        if (monsterSpawnArea == null)
        {
            Debug.LogError(
                $"[RoomBattle] MonsterSpawnArea가 없습니다 : {gameObject.name}"
            );

            return;
        }


        if (monsterPrefabs == null ||
            monsterPrefabs.Count == 0)
        {
            Debug.LogError(
                $"[RoomBattle] Monster Prefab List가 비어있습니다 : {gameObject.name}"
            );

            return;
        }


        aliveMonsters.Clear();


        for (int i = 0;
             i < monstersToSpawn;
             i++)
        {
            GameObject monsterPrefab =
                GetRandomMonsterPrefab();


            if (monsterPrefab == null)
                continue;


            // =====================================================
            // 원래의 정상적인 바닥 Spawn 위치
            // =====================================================

            if (!TryGetSpawnPosition(
                    out Vector3 groundPosition))
            {
                Debug.LogWarning(
                    $"[RoomBattle] Monster {i + 1}의 " +
                    $"Spawn 위치를 찾지 못했습니다."
                );

                continue;
            }


            // =====================================================
            // 실제 생성 위치는 바닥보다 위쪽
            // =====================================================

            Vector3 airPosition =
                groundPosition +
                Vector3.up *
                monsterSpawnHeight;


            GameObject monster =
                Instantiate(
                    monsterPrefab,
                    airPosition,
                    monsterPrefab.transform.rotation,
                    runtimeRoot
                );


            monster.name =
                $"{monsterPrefab.name}_{i + 1}";


            aliveMonsters.Add(
                monster
            );


            // =====================================================
            // 실제 중력 낙하 연출 시작
            // =====================================================

            StartCoroutine(
                MonsterDropRoutine(
                    monster,
                    groundPosition
                )
            );
        }
    }

    // =========================================================
    // Monster 실제 낙하 Spawn
    // =========================================================

    private IEnumerator MonsterDropRoutine(
        GameObject monster,
        Vector3 groundPosition
    )
    {
        if (monster == null)
            yield break;


        // =====================================================
        // Components
        // =====================================================

        EnemyAI enemyAI =
            monster.GetComponentInChildren<EnemyAI>(
                true
            );


        EnemyShooter enemyShooter =
            monster.GetComponentInChildren<EnemyShooter>(
                true
            );


        NavMeshAgent agent =
            monster.GetComponentInChildren<NavMeshAgent>(
                true
            );


        // =====================================================
        // 등장 중에는 AI 완전 정지
        // =====================================================

        if (enemyAI != null)
        {
            enemyAI.enabled =
                false;
        }


        if (enemyShooter != null)
        {
            enemyShooter.enabled =
                false;
        }


        // =====================================================
        // NavMeshAgent OFF
        //
        // 낙하 중에는 물리가 위치를 담당
        // =====================================================

        if (agent != null)
        {
            agent.enabled =
                false;
        }


        // =====================================================
        // Rigidbody 준비
        // =====================================================

        Rigidbody rb =
            monster.GetComponent<Rigidbody>();


        bool createdTemporaryRigidbody =
            false;


        // Enemy에 Rigidbody가 없다면
        // 등장 연출 동안만 임시 생성
        if (rb == null)
        {
            rb =
                monster.AddComponent<Rigidbody>();


            createdTemporaryRigidbody =
                true;
        }


        // =====================================================
        // 기존 Rigidbody 설정 저장
        // =====================================================

        bool oldUseGravity =
            rb.useGravity;


        bool oldIsKinematic =
            rb.isKinematic;


        RigidbodyConstraints oldConstraints =
            rb.constraints;


        CollisionDetectionMode oldCollisionDetection =
            rb.collisionDetectionMode;


        // =====================================================
        // 실제 낙하 설정
        // =====================================================

        rb.useGravity =
            true;


        rb.isKinematic =
            false;


        // 떨어지면서 적이 옆으로 뒤집히는 것 방지
        rb.constraints =
            RigidbodyConstraints.FreezeRotation;


        // 빠르게 떨어져도 바닥 관통 방지
        rb.collisionDetectionMode =
            CollisionDetectionMode.Continuous;


        rb.linearVelocity =
            Vector3.zero;


        rb.angularVelocity =
            Vector3.zero;


        // =====================================================
        // Landing Detector
        // =====================================================

        MonsterSpawnLandingDetector detector =
            monster.AddComponent
            <MonsterSpawnLandingDetector>();


        detector.Initialize(
            groundMask
        );


        // =====================================================
        // 실제 낙하
        // =====================================================

        float fallTimer =
            0f;


        while (!detector.HasLanded &&
               fallTimer < maximumFallTime)
        {
            if (monster == null)
                yield break;


            fallTimer +=
                Time.deltaTime;


            yield return null;
        }


        if (monster == null)
            yield break;


        // =====================================================
        // 착지
        // =====================================================

        rb.linearVelocity =
            Vector3.zero;


        rb.angularVelocity =
            Vector3.zero;


        // 바닥에 착지한 뒤에는 더 이상 움직이지 않게
        rb.useGravity =
            false;


        rb.isKinematic =
            true;


        Debug.Log(
            $"[RoomBattle] Monster 착지 : {monster.name}"
        );


        // =====================================================
        // 플레이어 준비 시간
        // =====================================================

        float readyDelay =
            Random.Range(
                monsterReadyDelayMin,
                monsterReadyDelayMax
            );


        yield return new WaitForSeconds(
            readyDelay
        );


        if (monster == null)
            yield break;


        // =====================================================
        // Landing Detector 제거
        // =====================================================

        if (detector != null)
        {
            Destroy(
                detector
            );
        }


        // =====================================================
        // Rigidbody 복구 / 제거
        // =====================================================

        if (createdTemporaryRigidbody)
        {
            Destroy(
                rb
            );


            // Destroy가 실제 적용될 때까지 한 프레임
            yield return null;
        }
        else
        {
            rb.useGravity =
                oldUseGravity;


            rb.isKinematic =
                oldIsKinematic;


            rb.constraints =
                oldConstraints;


            rb.collisionDetectionMode =
                oldCollisionDetection;
        }


        if (monster == null)
            yield break;


        // =====================================================
        // 원래 NavMesh 위치로 최종 보정
        //
        // 기존 시스템에서 정상적으로 사용하던 Spawn 위치이므로
        // 전투 시작점은 이 위치가 됨
        // =====================================================

        monster.transform.position =
            groundPosition;


        // =====================================================
        // NavMeshAgent 활성화
        // =====================================================

        if (agent != null)
        {
            agent.enabled =
                true;


            if (agent.isOnNavMesh)
            {
                agent.Warp(
                    groundPosition
                );
            }
        }


        // =====================================================
        // 공격 시스템 활성화
        // =====================================================

        if (enemyShooter != null)
        {
            enemyShooter.enabled =
                true;
        }


        if (enemyAI != null)
        {
            enemyAI.enabled =
                true;
        }


        Debug.Log(
            $"[RoomBattle] Monster 전투 시작 : {monster.name}"
        );
    }

    // =========================================================
    // Monster Prefab Random
    // =========================================================

    private GameObject GetRandomMonsterPrefab()
    {
        if (monsterPrefabs == null ||
            monsterPrefabs.Count == 0)
        {
            return null;
        }


        // None 슬롯을 피하기 위해
        // 몇 번 검색
        for (int i = 0;
             i < monsterPrefabs.Count * 2;
             i++)
        {
            GameObject candidate =
                monsterPrefabs[
                    Random.Range(
                        0,
                        monsterPrefabs.Count
                    )
                ];


            if (candidate != null)
            {
                return candidate;
            }
        }


        return null;
    }


    // =========================================================
    // Spawn Position 찾기
    // =========================================================

    private bool TryGetSpawnPosition(
        out Vector3 result
    )
    {
        result =
            monsterSpawnArea.position;


        for (int attempt = 0;
             attempt < maxSpawnPositionAttempts;
             attempt++)
        {
            float randomX =
                Random.Range(
                    -spawnAreaSize.x * 0.5f,
                    spawnAreaSize.x * 0.5f
                );


            float randomZ =
                Random.Range(
                    -spawnAreaSize.z * 0.5f,
                    spawnAreaSize.z * 0.5f
                );


            // MonsterSpawnArea의 Local 방향 기준
            Vector3 localPosition =
                new Vector3(
                    randomX,
                    0f,
                    randomZ
                );


            Vector3 worldPosition =
                monsterSpawnArea.TransformPoint(
                    localPosition
                );


            // ==========================================
            // 바닥 찾기
            // ==========================================

            Vector3 rayOrigin =
                worldPosition +
                Vector3.up *
                groundRayHeight;


            if (Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    groundRayHeight * 2f,
                    groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                worldPosition =
                    hit.point;
            }


            // =========================================================
            // NavMesh 위의 가장 가까운 위치 검색
            // =========================================================

            if (!NavMesh.SamplePosition(
                    worldPosition,
                    out NavMeshHit navHit,
                    3f,
                    NavMesh.AllAreas))
            {
                // 주변 3m 안에 NavMesh가 없음
                continue;
            }


            Vector3 navMeshPosition =
                navHit.position;


            // 다른 Monster와 너무 가까운지 확인
            if (!IsTooCloseToOtherMonster(
                    navMeshPosition))
            {
                result =
                    navMeshPosition;

                return true;
            }
        }


        return false;
    }


    // =========================================================
    // Monster 간격 검사
    // =========================================================

    private bool IsTooCloseToOtherMonster(
        Vector3 position
    )
    {
        float minDistanceSqr =
            minimumMonsterSpacing *
            minimumMonsterSpacing;


        foreach (
            GameObject monster in aliveMonsters)
        {
            if (monster == null)
                continue;


            Vector3 difference =
                monster.transform.position -
                position;


            // Y축은 제외하고 평면 거리 검사
            difference.y =
                0f;


            if (difference.sqrMagnitude <
                minDistanceSqr)
            {
                return true;
            }
        }


        return false;
    }


    // =========================================================
    // Barrier Spawn
    // =========================================================

    private void SpawnBarriers()
    {
        spawnedBarriers.Clear();


        if (barrierPrefab == null)
        {
            Debug.LogWarning(
                $"[RoomBattle] Barrier Prefab이 없습니다 : {gameObject.name}"
            );

            return;
        }


        foreach (
            Transform point in barrierSpawnPoints)
        {
            if (point == null)
                continue;


            GameObject barrier =
                Instantiate(
                    barrierPrefab,
                    point.position,
                    point.rotation,
                    runtimeRoot
                );


            spawnedBarriers.Add(
                barrier
            );
        }


        Debug.Log(
            $"[RoomBattle] Barrier 생성 : {spawnedBarriers.Count}"
        );
    }


    // =========================================================
    // Monster 감시
    // =========================================================

    private IEnumerator CheckMonstersRoutine()
    {
        WaitForSeconds wait =
            new WaitForSeconds(
                0.2f
            );


        while (battleStarted &&
               !battleCompleted)
        {
            // Enemy가 Destroy되거나 비활성화됐다면
            // 살아있는 목록에서 제거
            aliveMonsters.RemoveAll(
                monster =>
                    monster == null ||
                    !monster.activeInHierarchy
            );


            if (aliveMonsters.Count == 0)
            {
                monsterCheckRoutine =
                    null;


                CompleteBattle();

                yield break;
            }


            yield return wait;
        }


        monsterCheckRoutine =
            null;
    }


    // =========================================================
    // Battle Complete
    // =========================================================

    private void CompleteBattle()
    {
        if (battleCompleted)
            return;


        battleCompleted =
            true;


        Debug.Log(
            $"[RoomBattle] 방 클리어 : {transform.root.name}"
        );


        // ==========================================
        // 입구 벽 제거
        // ==========================================

        foreach (
            GameObject barrier in spawnedBarriers)
        {
            if (barrier != null)
            {
                Destroy(
                    barrier
                );
            }
        }


        spawnedBarriers.Clear();
    }


    // =========================================================
    // Spawn Area Gizmo
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (!drawSpawnArea ||
            monsterSpawnArea == null)
        {
            return;
        }


        Matrix4x4 previousMatrix =
            Gizmos.matrix;


        Gizmos.matrix =
            monsterSpawnArea.localToWorldMatrix;


        Gizmos.DrawWireCube(
            Vector3.zero,
            spawnAreaSize
        );


        Gizmos.matrix =
            previousMatrix;


        // Barrier Point도 확인
        foreach (
            Transform point in barrierSpawnPoints)
        {
            if (point == null)
                continue;


            Gizmos.DrawWireSphere(
                point.position,
                0.35f
            );
        }
    }
}