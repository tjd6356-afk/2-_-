using System.Collections;
using UnityEngine;

public class FarmGrowSlot : MonoBehaviour
{
    // =========================================================
    // Visual
    // =========================================================

    [Header("Grow Visual")]

    [Tooltip("성장 중에 보여줄 Model")]
    [SerializeField]
    private GameObject notGrowModel;


    [Tooltip("성장이 끝났을 때 보여줄 Model")]
    [SerializeField]
    private GameObject perfectGrowModel;


    // =========================================================
    // Growth
    // =========================================================

    [Header("Growth")]

    [Tooltip("아이템이 완전히 자라는데 필요한 시간")]
    [SerializeField]
    private float growDuration =
        10f;


    // =========================================================
    // Harvest
    // =========================================================

    [Header("Harvest")]

    [Tooltip("성장 완료 후 Item Pickup이 생성될 위치")]
    [SerializeField]
    private Transform harvestSpawnPoint;


    // =========================================================
    // State
    // =========================================================

    private enum GrowState
    {
        Empty,
        Growing,
        Ready
    }


    private GrowState state =
        GrowState.Empty;


    private ItemData plantedItemData;


    private ItemPickup spawnedHarvest;


    private Coroutine growCoroutine;


    private Coroutine harvestWatchCoroutine;


    // =========================================================
    // Properties
    // =========================================================

    public bool IsEmpty =>
        state ==
        GrowState.Empty;


    public bool IsGrowing =>
        state ==
        GrowState.Growing;


    public bool IsReady =>
        state ==
        GrowState.Ready;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        ResetVisuals();
    }


    // =========================================================
    // Plant
    // =========================================================

    public bool TryPlantFromHeld(
        PlayerHeldItemController heldController
    )
    {
        // ==========================================
        // 이미 사용 중인 밭
        // ==========================================

        if (state != GrowState.Empty)
        {
            Debug.Log(
                $"[Farm] {name} : " +
                $"이미 작물이 존재합니다."
            );

            return false;
        }


        if (heldController == null)
            return false;


        // ==========================================
        // Player가 들고 있는 Item 하나 가져오기
        // ==========================================

        if (!heldController.TryTakeFirstHeldItem(
                out ItemData itemData))
        {
            Debug.Log(
                $"[Farm] {name} : " +
                $"심을 Item을 들고 있지 않습니다."
            );

            return false;
        }


        if (itemData == null)
            return false;


        // ==========================================
        // 해당 ItemData 기억
        // ==========================================

        plantedItemData =
            itemData;


        state =
            GrowState.Growing;


        // ==========================================
        // 성장 중 Model
        // ==========================================

        if (notGrowModel != null)
        {
            notGrowModel.SetActive(
                true
            );
        }


        if (perfectGrowModel != null)
        {
            perfectGrowModel.SetActive(
                false
            );
        }


        // ==========================================
        // 성장 시작
        // ==========================================

        if (growCoroutine != null)
        {
            StopCoroutine(
                growCoroutine
            );
        }


        growCoroutine =
            StartCoroutine(
                GrowRoutine()
            );


        Debug.Log(
            $"[Farm] {name} Plant : " +
            $"{plantedItemData.ItemTitle}"
        );


        return true;
    }


    // =========================================================
    // Grow
    // =========================================================

    private IEnumerator GrowRoutine()
    {
        float duration =
            Mathf.Max(
                0f,
                growDuration
            );


        if (duration > 0f)
        {
            yield return
                new WaitForSeconds(
                    duration
                );
        }


        growCoroutine =
            null;


        CompleteGrowth();
    }


    // =========================================================
    // Growth Complete
    // =========================================================

    private void CompleteGrowth()
    {
        if (state !=
            GrowState.Growing)
        {
            return;
        }


        state =
            GrowState.Ready;


        // ==========================================
        // 성장 중 Model OFF
        // ==========================================

        if (notGrowModel != null)
        {
            notGrowModel.SetActive(
                false
            );
        }


        // ==========================================
        // 완성 Model ON
        // ==========================================

        if (perfectGrowModel != null)
        {
            perfectGrowModel.SetActive(
                true
            );
        }


        // ==========================================
        // 수확할 Item 생성
        // ==========================================

        SpawnHarvestItem();


        Debug.Log(
            $"[Farm] {name} Growth Complete : " +
            $"{plantedItemData.ItemTitle}"
        );
    }


    // =========================================================
    // Spawn Harvest
    // =========================================================

    private void SpawnHarvestItem()
    {
        if (plantedItemData == null)
            return;


        Vector3 spawnPosition =
            harvestSpawnPoint != null
            ? harvestSpawnPoint.position
            : transform.position +
              Vector3.up * 0.5f;


        spawnedHarvest =
            ItemWorldSpawner.Spawn(
                plantedItemData,
                spawnPosition,
                1
            );


        if (spawnedHarvest == null)
        {
            Debug.LogError(
                $"[Farm] {name} : " +
                $"{plantedItemData.ItemTitle}의 " +
                $"수확 Item 생성에 실패했습니다."
            );

            return;
        }


        // ==========================================
        // Pickup이 회수되었는지 감시
        // ==========================================

        if (harvestWatchCoroutine != null)
        {
            StopCoroutine(
                harvestWatchCoroutine
            );
        }


        harvestWatchCoroutine =
            StartCoroutine(
                WaitForHarvest()
            );
    }


    // =========================================================
    // Harvest Watch
    // =========================================================

    private IEnumerator WaitForHarvest()
    {
        // ItemPickup이 수집되면
        // GameObject가 Destroy된다고 가정
        while (spawnedHarvest != null)
        {
            yield return null;
        }


        harvestWatchCoroutine =
            null;


        ResetFarm();
    }


    // =========================================================
    // Reset
    // =========================================================

    private void ResetFarm()
    {
        state =
            GrowState.Empty;


        plantedItemData =
            null;


        spawnedHarvest =
            null;


        ResetVisuals();


        Debug.Log(
            $"[Farm] {name} Reset"
        );
    }


    private void ResetVisuals()
    {
        if (notGrowModel != null)
        {
            notGrowModel.SetActive(
                false
            );
        }


        if (perfectGrowModel != null)
        {
            perfectGrowModel.SetActive(
                false
            );
        }
    }


    // =========================================================
    // Player Trigger
    // =========================================================

    private void OnTriggerEnter(
        Collider other
    )
    {
        PlayerHeldItemController heldController =
            other.GetComponentInParent
            <PlayerHeldItemController>();


        if (heldController == null)
            return;


        heldController.RegisterFarmSlot(
            this
        );


        Debug.Log(
            $"[Farm] Player Enter : {name}"
        );
    }


    private void OnTriggerExit(
        Collider other
    )
    {
        PlayerHeldItemController heldController =
            other.GetComponentInParent
            <PlayerHeldItemController>();


        if (heldController == null)
            return;


        heldController.UnregisterFarmSlot(
            this
        );
    }


    // =========================================================
    // Inspector
    // =========================================================

    private void OnValidate()
    {
        growDuration =
            Mathf.Max(
                0f,
                growDuration
            );
    }
}