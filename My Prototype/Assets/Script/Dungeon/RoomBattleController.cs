using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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


            if (!TryGetSpawnPosition(
                    out Vector3 spawnPosition))
            {
                Debug.LogWarning(
                    $"[RoomBattle] Monster {i + 1}의 " +
                    "Spawn 위치를 찾지 못했습니다."
                );

                continue;
            }


            GameObject monster =
                Instantiate(
                    monsterPrefab,
                    spawnPosition,
                    monsterPrefab.transform.rotation,
                    runtimeRoot
                );


            monster.name =
                $"{monsterPrefab.name}_{i + 1}";


            aliveMonsters.Add(
                monster
            );
        }
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


            // ==========================================
            // 기존 Monster와 너무 가까운지 확인
            // ==========================================

            if (!IsTooCloseToOtherMonster(
                    worldPosition))
            {
                result =
                    worldPosition;

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