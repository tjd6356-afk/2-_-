using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponUpgradePanelUI : MonoBehaviour
{
    [Header("UI Controls")]
    [Tooltip("패널을 닫을 UI 버튼 (X 버튼 등)")]
    [SerializeField] private Button closeButton;

    // =========================================================
    // ✨ 우측 상세 정보 패널 (Right Panel) UI 요소들
    // =========================================================
    [Header("Right Detail Panel")]
    [SerializeField] private GameObject rightPanelContainer;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private Image weaponIconImage;
    [SerializeField] private TextMeshProUGUI weaponDescText;
    [SerializeField] private TextMeshProUGUI materialCostText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonText;

    // =========================================================
    // ✨ 수정됨: 필요 소재(다중) 구조체 추가
    // =========================================================
    [Serializable]
    public struct MaterialCost
    {
        public ItemData requiredItem;
        public int requiredAmount;
    }

    // =========================================================
    // UI 노드 및 트리 구조체
    // =========================================================
    [Serializable]
    public struct UpgradeNode
    {
        public Button nodeButton;
        public WeaponBase weaponPrefab;

        [Header("Node Info (우측 패널용)")]
        public Sprite weaponIcon;
        [TextArea(2, 5)]
        public string weaponDescription;

        // ✨ 수정됨: 여러 개의 소재를 등록할 수 있도록 List로 변경
        [Header("Unlock Requirements")]
        public List<MaterialCost> requiredMaterials;

        public bool isUnlocked;
    }

    [Serializable]
    public struct WeaponTree
    {
        public WeaponType weaponType;
        public GameObject treeUIRoot;
        public List<UpgradeNode> nodes;
    }

    [Header("Upgrade Trees by Weapon Type")]
    public List<WeaponTree> weaponTrees = new List<WeaponTree>();

    private PlayerWeaponManager currentPlayerWeaponManager;
    private Action onCloseCallback;

    private int currentSelectedTreeIndex = -1;
    private int currentSelectedNodeIndex = -1;

    public void OpenPanel(PlayerWeaponManager playerManager, Action onClose)
    {
        if (playerManager == null) return;

        currentPlayerWeaponManager = playerManager;
        onCloseCallback = onClose;

        gameObject.SetActive(true);
        if (rightPanelContainer != null) rightPanelContainer.SetActive(false);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePanel);
        }

        WeaponType currentType = currentPlayerWeaponManager.ActiveWeaponType;

        int activeTreeIndex = -1;
        int targetNodeIndex = 0;

        for (int i = 0; i < weaponTrees.Count; i++)
        {
            if (weaponTrees[i].treeUIRoot != null)
                weaponTrees[i].treeUIRoot.SetActive(false);
        }

        for (int i = 0; i < weaponTrees.Count; i++)
        {
            if (weaponTrees[i].weaponType == currentType)
            {
                activeTreeIndex = i;
                if (weaponTrees[i].treeUIRoot != null)
                    weaponTrees[i].treeUIRoot.SetActive(true);

                for (int j = 0; j < weaponTrees[i].nodes.Count; j++)
                {
                    if (weaponTrees[i].nodes[j].nodeButton != null && weaponTrees[i].nodes[j].weaponPrefab != null)
                    {
                        Button btn = weaponTrees[i].nodes[j].nodeButton;
                        btn.onClick.RemoveAllListeners();

                        int treeIdx = i;
                        int nodeIdx = j;
                        btn.onClick.AddListener(() => OnNodeClicked(treeIdx, nodeIdx));

                        if (currentPlayerWeaponManager.CurrentWeapon != null &&
                            weaponTrees[i].nodes[j].weaponPrefab.WeaponId == currentPlayerWeaponManager.CurrentWeapon.WeaponId)
                        {
                            targetNodeIndex = j;
                        }
                    }
                }
                break;
            }
        }

        if (activeTreeIndex != -1 && weaponTrees[activeTreeIndex].nodes.Count > 0)
        {
            OnNodeClicked(activeTreeIndex, targetNodeIndex);
        }
    }

    private void ClosePanel()
    {
        onCloseCallback?.Invoke();
    }

    private void OnNodeClicked(int treeIndex, int nodeIndex)
    {
        currentSelectedTreeIndex = treeIndex;
        currentSelectedNodeIndex = nodeIndex;

        UpgradeNode selectedNode = weaponTrees[treeIndex].nodes[nodeIndex];

        if (rightPanelContainer != null) rightPanelContainer.SetActive(true);

        if (weaponNameText != null) weaponNameText.text = selectedNode.weaponPrefab.WeaponName;
        if (weaponIconImage != null) weaponIconImage.sprite = selectedNode.weaponIcon;
        if (weaponDescText != null) weaponDescText.text = selectedNode.weaponDescription;

        UpdateRightPanelActionUI(selectedNode);
    }

    // =========================================================
    // 해금/장착 상태 갱신 (다중 인벤토리 검사)
    // =========================================================
    private void UpdateRightPanelActionUI(UpgradeNode node)
    {
        actionButton.onClick.RemoveAllListeners();

        if (node.isUnlocked)
        {
            if (materialCostText != null) materialCostText.text = "해금됨";
            if (actionButtonText != null) actionButtonText.text = "장착하기";

            if (currentPlayerWeaponManager.CurrentWeapon != null &&
                currentPlayerWeaponManager.CurrentWeapon.WeaponId == node.weaponPrefab.WeaponId)
            {
                actionButton.interactable = false;
                if (actionButtonText != null) actionButtonText.text = "장착됨";
            }
            else
            {
                actionButton.interactable = true;
                actionButton.onClick.AddListener(EquipSelectedWeapon);
            }
        }
        else
        {
            if (actionButtonText != null) actionButtonText.text = "해금하기";

            // ✨ 다중 소재 검사 로직
            if (node.requiredMaterials != null && node.requiredMaterials.Count > 0)
            {
                bool hasAllMaterials = true;
                string costString = "";

                foreach (MaterialCost cost in node.requiredMaterials)
                {
                    if (cost.requiredItem == null) continue;

                    int currentAmount = InventoryManager.Instance.GetItemAmount(cost.requiredItem);
                    costString += $"{cost.requiredItem.ItemTitle}: {currentAmount} / {cost.requiredAmount}\n";

                    if (currentAmount < cost.requiredAmount)
                    {
                        hasAllMaterials = false;
                    }
                }

                if (materialCostText != null)
                    materialCostText.text = costString.TrimEnd(); // 마지막 줄바꿈 제거

                // 소재가 하나라도 부족하면 버튼 비활성화
                actionButton.interactable = hasAllMaterials;
                if (hasAllMaterials)
                {
                    actionButton.onClick.AddListener(TryUnlockWeapon);
                }
            }
            else
            {
                // 필요 소재 리스트가 비어있을 경우 (무료 해금)
                if (materialCostText != null) materialCostText.text = "필요 소재: 없음";
                actionButton.interactable = true;
                actionButton.onClick.AddListener(TryUnlockWeapon);
            }
        }
    }

    // =========================================================
    // 해금하기 버튼 클릭 시 다중 차감
    // =========================================================
    private void TryUnlockWeapon()
    {
        if (currentSelectedTreeIndex < 0 || currentSelectedNodeIndex < 0) return;

        WeaponTree tree = weaponTrees[currentSelectedTreeIndex];
        UpgradeNode node = tree.nodes[currentSelectedNodeIndex];

        // 1. 등록된 모든 소재 차감 진행
        if (node.requiredMaterials != null && node.requiredMaterials.Count > 0)
        {
            // 이중 안전 장치: 차감 전 모든 소재가 정말 다 있는지 한 번 더 확인
            foreach (MaterialCost cost in node.requiredMaterials)
            {
                if (cost.requiredItem == null) continue;
                if (InventoryManager.Instance.GetItemAmount(cost.requiredItem) < cost.requiredAmount)
                {
                    Debug.LogWarning("[업그레이드 패널] 소재가 부족합니다.");
                    return;
                }
            }

            // 확인 후 실제 차감
            foreach (MaterialCost cost in node.requiredMaterials)
            {
                if (cost.requiredItem == null || cost.requiredAmount <= 0) continue;
                InventoryManager.Instance.RemoveItem(cost.requiredItem, cost.requiredAmount);
            }
        }

        Debug.Log($"[업그레이드 패널] {node.weaponPrefab.WeaponName} 무기를 해금했습니다!");

        node.isUnlocked = true;
        tree.nodes[currentSelectedNodeIndex] = node;
        weaponTrees[currentSelectedTreeIndex] = tree;

        UpdateRightPanelActionUI(node);
    }

    private void EquipSelectedWeapon()
    {
        if (currentPlayerWeaponManager == null || currentSelectedTreeIndex < 0 || currentSelectedNodeIndex < 0) return;

        UpgradeNode node = weaponTrees[currentSelectedTreeIndex].nodes[currentSelectedNodeIndex];

        currentPlayerWeaponManager.EquipWeapon(node.weaponPrefab);
        Debug.Log($"[업그레이드 패널] {node.weaponPrefab.WeaponName} 장착 완료!");

        UpdateRightPanelActionUI(node);
    }
}