using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RoomBattleController : MonoBehaviour
{
    [System.Serializable]
    public class MonsterSpawnEntry
    {
        [Tooltip("생성할 몬스터 프리팹")]
        public GameObject prefab;

        [Tooltip("체크 시 바닥 낙하 대신 전투 시작 후 일정 시간 뒤 공중에서 파티클과 함께 소환됩니다.")]
        public bool isFlying;
    }

    // =========================================================
    // Monster
    // =========================================================

    [Header("Monster Prefabs")]

    [Tooltip("이 방에서 랜덤으로 생성될 몬스터 목록 (공중형 여부 직접 지정)")]
    [SerializeField]
    private List<MonsterSpawnEntry> monsterPrefabs =
        new List<MonsterSpawnEntry>();

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
        new Vector3(10f, 1f, 10f);

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
    // Ground Monster Spawn Presentation (지상형 낙하 연출)
    // =========================================================

    [Header("Ground Monster Spawn (지상형 낙하 연출)")]

    [Tooltip("지상형 몬스터가 최종 Spawn 위치보다 얼마나 높은 곳에서 떨어질지")]
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
    // Flying Monster Spawn Presentation (공중형 지연 소환 연출)
    // =========================================================

    [Header("Flying Monster Spawn (공중형 지연 소환 연출)")]

    [Tooltip("전투 시작 후 공중형 몬스터가 소환되기까지의 최소 지연 시간 (초)")]
    [SerializeField]
    private float aerialSpawnDelayMin = 1.6f;

    [Tooltip("전투 시작 후 공중형 몬스터가 소환되기까지의 최대 지연 시간 (초)")]
    [SerializeField]
    private float aerialSpawnDelayMax = 2.2f;

    [Tooltip("지상 적 없이 공중 적만 스폰될 때 지연 시간에서 차감할 시간 (초)")]
    [SerializeField]
    private float aerialOnlyDelayReduction = 1.9f;

    [Tooltip("공중형 몬스터 소환 시 터질 파티클 이펙트 프리팹")]
    [SerializeField]
    private GameObject aerialSpawnEffectPrefab;

    [Tooltip("공중형 몬스터가 바닥 기준 소환될 최소 높이")]
    [SerializeField]
    private float aerialSpawnHeightMin = 2.0f;

    [Tooltip("공중형 몬스터가 바닥 기준 소환될 최대 높이")]
    [SerializeField]
    private float aerialSpawnHeightMax = 3.2f;

    [Tooltip("공중 소환 후 AI가 활성화되기 전 최소 대기시간 (초)")]
    [SerializeField]
    private float aerialReadyDelayMin = 0.4f;

    [Tooltip("공중 소환 후 AI가 활성화되기 전 최대 대기시간 (초)")]
    [SerializeField]
    private float aerialReadyDelayMax = 0.7f;

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
        pendingMonsterSpawns = 0;

        if (monsterCheckRoutine != null)
        {
            StopCoroutine(monsterCheckRoutine);
            monsterCheckRoutine = null;
        }

        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            if (col != null && col.isTrigger)
            {
                col.enabled = false;
            }
        }

        Debug.Log($"[RoomBattle] Battle 비활성화 : {transform.root.name}");
        enabled = false;
    }

    // =========================================================
    // Runtime
    // =========================================================

    private bool battleStarted;
    private bool battleCompleted;
    private int pendingMonsterSpawns;

    private readonly List<GameObject> aliveMonsters = new List<GameObject>();
    private readonly List<GameObject> spawnedBarriers = new List<GameObject>();

    private Transform runtimeRoot;
    private Coroutine monsterCheckRoutine;

    public bool BattleStarted => battleStarted;
    public bool BattleCompleted => battleCompleted;

    // =========================================================
    // Player Trigger
    // =========================================================

    private void OnTriggerEnter(Collider other)
    {
        if (battleDisabled)
            return;

        if (battleStarted || battleCompleted)
            return;

        PlayerStats player = other.GetComponentInParent<PlayerStats>();
        if (player == null)
            return;

        Debug.Log($"[RoomBattle] Player 진입 : {transform.root.name}");
        StartBattle();
    }

    // =========================================================
    // Battle Start
    // =========================================================

    private void StartBattle()
    {
        if (battleStarted || battleCompleted)
            return;

        battleStarted = true;

        CreateRuntimeRoot();
        SpawnBarriers();
        SpawnMonsters();

        // 즉시 생성된 적도 없고 지연 소환 대기 중인 적도 없을 때만 즉시 클리어
        if (aliveMonsters.Count == 0 && pendingMonsterSpawns == 0)
        {
            Debug.LogWarning($"[RoomBattle] 생성된 Monster가 없습니다 : {transform.root.name}");
            CompleteBattle();
            return;
        }

        monsterCheckRoutine = StartCoroutine(CheckMonstersRoutine());
        Debug.Log($"[RoomBattle] 전투 시작 / 즉시 소환: {aliveMonsters.Count}, 지연 소환 대기: {pendingMonsterSpawns}");
    }

    // =========================================================
    // Runtime Root
    // =========================================================

    private void CreateRuntimeRoot()
    {
        if (runtimeRoot != null)
            return;

        GameObject root = new GameObject("BattleRuntime");
        Transform parent = transform.parent != null ? transform.parent : transform;

        root.transform.SetParent(parent);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;

        runtimeRoot = root.transform;
    }

    // =========================================================
    // Monster Spawn
    // =========================================================

    private void SpawnMonsters()
    {
        if (monsterSpawnArea == null)
        {
            Debug.LogError($"[RoomBattle] MonsterSpawnArea가 없습니다 : {gameObject.name}");
            return;
        }

        if (monsterPrefabs == null || monsterPrefabs.Count == 0)
        {
            Debug.LogError($"[RoomBattle] Monster Prefab List가 비어있습니다 : {gameObject.name}");
            return;
        }

        aliveMonsters.Clear();
        pendingMonsterSpawns = 0;

        // 1. 이번 전투에서 소환할 몬스터 목록을 먼저 추첨
        List<MonsterSpawnEntry> selectedEntries = new List<MonsterSpawnEntry>(monstersToSpawn);
        for (int i = 0; i < monstersToSpawn; i++)
        {
            MonsterSpawnEntry entry = GetRandomMonsterEntry();
            selectedEntries.Add(entry);
        }

        // 2. 지상형 몬스터 먼저 즉시 소환
        for (int i = 0; i < selectedEntries.Count; i++)
        {
            MonsterSpawnEntry entry = selectedEntries[i];
            if (entry == null || entry.prefab == null || entry.isFlying)
                continue;

            int monsterIndex = i + 1;

            if (!TryGetSpawnPosition(out Vector3 groundPosition))
            {
                Debug.LogWarning($"[RoomBattle] Monster {monsterIndex}의 Spawn 위치를 찾지 못했습니다.");
                continue;
            }

            Vector3 airPosition = groundPosition + Vector3.up * monsterSpawnHeight;

            GameObject monster = Instantiate(
                entry.prefab,
                airPosition,
                entry.prefab.transform.rotation,
                runtimeRoot
            );

            monster.name = $"{entry.prefab.name}_{monsterIndex}";
            aliveMonsters.Add(monster);

            StartCoroutine(MonsterDropRoutine(monster, groundPosition));
        }

        // 3. 지상 적이 한 마리도 없다면 공중 적 지연 시간 단축
        bool hasGroundMonster = aliveMonsters.Count > 0;
        float delayReduction = hasGroundMonster ? 0f : aerialOnlyDelayReduction;

        // 4. 공중형 몬스터 소환 예약
        for (int i = 0; i < selectedEntries.Count; i++)
        {
            MonsterSpawnEntry entry = selectedEntries[i];
            if (entry == null || entry.prefab == null || !entry.isFlying)
                continue;

            int monsterIndex = i + 1;
            pendingMonsterSpawns++;
            StartCoroutine(DelayedAerialSpawnRoutine(entry.prefab, monsterIndex, delayReduction));
        }
    }

    // =========================================================
    // 공중형 Monster 지연 소환 루틴
    // =========================================================

    private IEnumerator DelayedAerialSpawnRoutine(
        GameObject prefab,
        int monsterIndex,
        float delayReduction
    )
    {
        float spawnDelay = Mathf.Max(
            0f,
            Random.Range(aerialSpawnDelayMin, aerialSpawnDelayMax) - delayReduction
        );

        if (spawnDelay > 0f)
        {
            yield return new WaitForSeconds(spawnDelay);
        }

        if (battleDisabled || battleCompleted || prefab == null)
        {
            pendingMonsterSpawns = Mathf.Max(0, pendingMonsterSpawns - 1);
            yield break;
        }

        if (!TryGetSpawnPosition(out Vector3 groundPosition))
        {
            Debug.LogWarning($"[RoomBattle] 공중 Monster {monsterIndex}의 Spawn 위치를 찾지 못했습니다.");
            pendingMonsterSpawns = Mathf.Max(0, pendingMonsterSpawns - 1);
            yield break;
        }

        float spawnHeight = Random.Range(aerialSpawnHeightMin, aerialSpawnHeightMax);
        Vector3 aerialPosition = groundPosition + Vector3.up * spawnHeight;

        GameObject monster = Instantiate(
            prefab,
            aerialPosition,
            prefab.transform.rotation,
            runtimeRoot
        );

        monster.name = $"{prefab.name}_{monsterIndex}";

        // 살아있는 목록에 먼저 등록한 뒤 대기 카운트 차감 (클리어 판정 공백 방지)
        aliveMonsters.Add(monster);
        pendingMonsterSpawns = Mathf.Max(0, pendingMonsterSpawns - 1);

        yield return StartCoroutine(AerialMonsterSpawnRoutine(monster, aerialPosition));
    }

    // =========================================================
    // 공중형 Monster 소환 연출 (파티클 + AI 대기)
    // =========================================================

    private IEnumerator AerialMonsterSpawnRoutine(
        GameObject monster,
        Vector3 aerialPosition
    )
    {
        if (monster == null)
            yield break;

        FlyingEnemyAI flyingAI = monster.GetComponentInChildren<FlyingEnemyAI>(true);
        EnemyAI groundAI = monster.GetComponentInChildren<EnemyAI>(true);
        EnemyShooter shooter = monster.GetComponentInChildren<EnemyShooter>(true);
        NavMeshAgent agent = monster.GetComponentInChildren<NavMeshAgent>(true);

        if (flyingAI != null)
            flyingAI.enabled = false;

        if (groundAI != null)
            groundAI.enabled = false;

        if (shooter != null)
            shooter.enabled = false;

        if (agent != null)
            agent.enabled = false;

        Rigidbody rb = monster.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (aerialSpawnEffectPrefab != null)
        {
            Instantiate(
                aerialSpawnEffectPrefab,
                aerialPosition,
                aerialSpawnEffectPrefab.transform.rotation,
                runtimeRoot
            );
        }

        Debug.Log($"[RoomBattle] 공중 Monster 소환 : {monster.name} (높이: {aerialPosition.y:F2})");

        float readyDelay = Random.Range(aerialReadyDelayMin, aerialReadyDelayMax);
        if (readyDelay > 0f)
        {
            yield return new WaitForSeconds(readyDelay);
        }

        if (monster == null)
            yield break;

        if (flyingAI != null)
        {
            flyingAI.enabled = true;
        }

        Debug.Log($"[RoomBattle] 공중 Monster 전투 시작 : {monster.name}");
    }

    // =========================================================
    // 지상형 Monster 실제 낙하 Spawn
    // =========================================================

    private IEnumerator MonsterDropRoutine(
        GameObject monster,
        Vector3 groundPosition
    )
    {
        if (monster == null)
            yield break;

        EnemyAI enemyAI = monster.GetComponentInChildren<EnemyAI>(true);
        EnemyShooter enemyShooter = monster.GetComponentInChildren<EnemyShooter>(true);
        NavMeshAgent agent = monster.GetComponentInChildren<NavMeshAgent>(true);

        if (enemyAI != null)
            enemyAI.enabled = false;

        if (enemyShooter != null)
            enemyShooter.enabled = false;

        if (agent != null)
            agent.enabled = false;

        Rigidbody rb = monster.GetComponent<Rigidbody>();
        bool createdTemporaryRigidbody = false;

        if (rb == null)
        {
            rb = monster.AddComponent<Rigidbody>();
            createdTemporaryRigidbody = true;
        }

        bool oldUseGravity = rb.useGravity;
        bool oldIsKinematic = rb.isKinematic;
        RigidbodyConstraints oldConstraints = rb.constraints;
        CollisionDetectionMode oldCollisionDetection = rb.collisionDetectionMode;

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        MonsterSpawnLandingDetector detector =
            monster.AddComponent<MonsterSpawnLandingDetector>();
        detector.Initialize(groundMask);

        float fallTimer = 0f;
        while (!detector.HasLanded && fallTimer < maximumFallTime)
        {
            if (monster == null)
                yield break;

            fallTimer += Time.deltaTime;
            yield return null;
        }

        if (monster == null)
            yield break;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true;

        Debug.Log($"[RoomBattle] Monster 착지 : {monster.name}");

        float readyDelay = Random.Range(monsterReadyDelayMin, monsterReadyDelayMax);
        yield return new WaitForSeconds(readyDelay);

        if (monster == null)
            yield break;

        if (detector != null)
        {
            Destroy(detector);
        }

        if (createdTemporaryRigidbody)
        {
            Destroy(rb);
            yield return null;
        }
        else
        {
            rb.useGravity = oldUseGravity;
            rb.isKinematic = oldIsKinematic;
            rb.constraints = oldConstraints;
            rb.collisionDetectionMode = oldCollisionDetection;
        }

        if (monster == null)
            yield break;

        monster.transform.position = groundPosition;

        if (agent != null)
        {
            agent.enabled = true;
            if (agent.isOnNavMesh)
            {
                agent.Warp(groundPosition);
            }
        }

        if (enemyShooter != null)
            enemyShooter.enabled = true;

        if (enemyAI != null)
            enemyAI.enabled = true;

        Debug.Log($"[RoomBattle] Monster 전투 시작 : {monster.name}");
    }

    // =========================================================
    // Monster Prefab Random
    // =========================================================

    private MonsterSpawnEntry GetRandomMonsterEntry()
    {
        if (monsterPrefabs == null || monsterPrefabs.Count == 0)
            return null;

        for (int i = 0; i < monsterPrefabs.Count * 2; i++)
        {
            MonsterSpawnEntry candidate =
                monsterPrefabs[Random.Range(0, monsterPrefabs.Count)];

            if (candidate != null && candidate.prefab != null)
            {
                return candidate;
            }
        }

        return null;
    }

    // =========================================================
    // Spawn Position 찾기
    // =========================================================

    private bool TryGetSpawnPosition(out Vector3 result)
    {
        result = monsterSpawnArea.position;

        for (int attempt = 0; attempt < maxSpawnPositionAttempts; attempt++)
        {
            float randomX = Random.Range(-spawnAreaSize.x * 0.5f, spawnAreaSize.x * 0.5f);
            float randomZ = Random.Range(-spawnAreaSize.z * 0.5f, spawnAreaSize.z * 0.5f);

            Vector3 localPosition = new Vector3(randomX, 0f, randomZ);
            Vector3 worldPosition = monsterSpawnArea.TransformPoint(localPosition);

            Vector3 rayOrigin = worldPosition + Vector3.up * groundRayHeight;
            if (Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    groundRayHeight * 2f,
                    groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                worldPosition = hit.point;
            }

            if (!NavMesh.SamplePosition(
                    worldPosition,
                    out NavMeshHit navHit,
                    3f,
                    NavMesh.AllAreas))
            {
                continue;
            }

            Vector3 navMeshPosition = navHit.position;

            if (!IsTooCloseToOtherMonster(navMeshPosition))
            {
                result = navMeshPosition;
                return true;
            }
        }

        return false;
    }

    // =========================================================
    // Monster 간격 검사
    // =========================================================

    private bool IsTooCloseToOtherMonster(Vector3 position)
    {
        float minDistanceSqr = minimumMonsterSpacing * minimumMonsterSpacing;

        foreach (GameObject monster in aliveMonsters)
        {
            if (monster == null)
                continue;

            Vector3 difference = monster.transform.position - position;
            difference.y = 0f;

            if (difference.sqrMagnitude < minDistanceSqr)
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
            Debug.LogWarning($"[RoomBattle] Barrier Prefab이 없습니다 : {gameObject.name}");
            return;
        }

        foreach (Transform point in barrierSpawnPoints)
        {
            if (point == null)
                continue;

            GameObject barrier = Instantiate(
                barrierPrefab,
                point.position,
                point.rotation,
                runtimeRoot
            );

            spawnedBarriers.Add(barrier);
        }

        Debug.Log($"[RoomBattle] Barrier 생성 : {spawnedBarriers.Count}");
    }

    // =========================================================
    // Monster 감시
    // =========================================================

    private IEnumerator CheckMonstersRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.2f);

        while (battleStarted && !battleCompleted)
        {
            aliveMonsters.RemoveAll(
                monster => monster == null || !monster.activeInHierarchy
            );

            // 살아있는 몬스터가 없고 지연 소환 대기 중인 몬스터도 없을 때만 클리어
            if (aliveMonsters.Count == 0 && pendingMonsterSpawns == 0)
            {
                monsterCheckRoutine = null;
                CompleteBattle();
                yield break;
            }

            yield return wait;
        }

        monsterCheckRoutine = null;
    }

    // =========================================================
    // Battle Complete
    // =========================================================

    private void CompleteBattle()
    {
        if (battleCompleted)
            return;

        battleCompleted = true;
        Debug.Log($"[RoomBattle] 방 클리어 : {transform.root.name}");

        foreach (GameObject barrier in spawnedBarriers)
        {
            if (barrier != null)
            {
                Destroy(barrier);
            }
        }

        spawnedBarriers.Clear();
    }

    // =========================================================
    // Spawn Area Gizmo
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (!drawSpawnArea || monsterSpawnArea == null)
            return;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = monsterSpawnArea.localToWorldMatrix;

        Gizmos.DrawWireCube(Vector3.zero, spawnAreaSize);

        Gizmos.matrix = previousMatrix;

        foreach (Transform point in barrierSpawnPoints)
        {
            if (point == null)
                continue;

            Gizmos.DrawWireSphere(point.position, 0.35f);
        }
    }

    private void OnValidate()
    {
        aerialSpawnDelayMin = Mathf.Max(0f, aerialSpawnDelayMin);
        aerialSpawnDelayMax = Mathf.Max(aerialSpawnDelayMin, aerialSpawnDelayMax);
        aerialOnlyDelayReduction = Mathf.Max(0f, aerialOnlyDelayReduction);

        aerialSpawnHeightMin = Mathf.Max(0.5f, aerialSpawnHeightMin);
        aerialSpawnHeightMax = Mathf.Max(aerialSpawnHeightMin, aerialSpawnHeightMax);

        aerialReadyDelayMin = Mathf.Max(0f, aerialReadyDelayMin);
        aerialReadyDelayMax = Mathf.Max(aerialReadyDelayMin, aerialReadyDelayMax);
    }
}