using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용을 위해 추가

public class WeaponUpgradePanelUI : MonoBehaviour
{
    [Header("UI Controls")]
    [Tooltip("패널을 닫을 UI 버튼 (X 버튼 등)")]
    [SerializeField] private Button closeButton;

    // =========================================================
    // ✨ 우측 상세 정보 패널 (Right Panel) UI 요소들 ✨
    // =========================================================
    [Header("Right Detail Panel")]
    [SerializeField] private GameObject rightPanelContainer; // 우측 패널 전체 컨테이너 (아무것도 안 눌렀을 때 숨기기 위함)
    [SerializeField] private TextMeshProUGUI weaponNameText; // 무기 이름 텍스트
    [SerializeField] private Image weaponIconImage;          // 무기 사진(아이콘)
    [SerializeField] private TextMeshProUGUI weaponDescText; // 무기 설명 텍스트

    [SerializeField] private TextMeshProUGUI blueprintCostText; // "필요 설계도: X장" 텍스트
    [SerializeField] private Button actionButton;               // 해금하기 / 장착하기 통합 버튼
    [SerializeField] private TextMeshProUGUI actionButtonText;  // 액션 버튼 안의 텍스트

    // =========================================================
    // UI 노드 및 트리 구조체
    // =========================================================
    [Serializable]
    public struct UpgradeNode
    {
        public Button nodeButton;
        public WeaponBase weaponPrefab;

        [Header("Node Info (우측 패널용)")]
        public Sprite weaponIcon;          // 무기 아이콘
        [TextArea(2, 5)]
        public string weaponDescription;   // 무기 설명
        public int requiredBlueprints;     // 해금에 필요한 설계도 개수
        public bool isUnlocked;            // 현재 해금 상태 (임시 변수 - 추후 세이브 데이터와 연동)
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

    // 현재 우측 패널에 띄워진(선택된) 무기의 노드 정보와 트리 인덱스를 기억하기 위한 변수
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
        int targetNodeIndex = 0; // 기본적으로 해당 트리의 첫 번째(0번) 무기를 띄움

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

                        
                        /*
                        if (playerManager.ActiveWeapon != null && 
                            weaponTrees[i].nodes[j].weaponPrefab.WeaponId == playerManager.ActiveWeapon.WeaponId)
                        {
                            targetNodeIndex = j;
                        }
                        */
                    }
                }
                break;
            }
        }

        // ✨ 핵심: 패널이 열리자마자 우측 패널에 해당 무기 정보 띄우기 (강제 클릭 효과)
        if (activeTreeIndex != -1 && weaponTrees[activeTreeIndex].nodes.Count > 0)
        {
            OnNodeClicked(activeTreeIndex, targetNodeIndex);
        }
    }

    private void ClosePanel()
    {
        onCloseCallback?.Invoke();
    }

    // =========================================================
    // 좌측 무기 버튼을 눌렀을 때: 우측 패널 정보 갱신
    // =========================================================
    private void OnNodeClicked(int treeIndex, int nodeIndex)
    {
        currentSelectedTreeIndex = treeIndex;
        currentSelectedNodeIndex = nodeIndex;

        UpgradeNode selectedNode = weaponTrees[treeIndex].nodes[nodeIndex];

        // 우측 패널 표시
        if (rightPanelContainer != null) rightPanelContainer.SetActive(true);

        // 정보 업데이트
        if (weaponNameText != null) weaponNameText.text = selectedNode.weaponPrefab.WeaponName;
        if (weaponIconImage != null) weaponIconImage.sprite = selectedNode.weaponIcon;
        if (weaponDescText != null) weaponDescText.text = selectedNode.weaponDescription;

        // 해금 여부에 따른 하단 UI 변경
        UpdateRightPanelActionUI(selectedNode);
    }

    // =========================================================
    // 해금/장착 상태에 따른 버튼 및 텍스트 갱신
    // =========================================================
    private void UpdateRightPanelActionUI(UpgradeNode node)
    {
        actionButton.onClick.RemoveAllListeners();

        if (node.isUnlocked)
        {
            // 이미 해금된 무기 -> 장착 모드
            if (blueprintCostText != null) blueprintCostText.text = "보유중";
            if (actionButtonText != null) actionButtonText.text = "장착하기";

            actionButton.onClick.AddListener(EquipSelectedWeapon);
        }
        else
        {
            // 잠긴 무기 -> 해금 모드
            if (blueprintCostText != null) blueprintCostText.text = $"필요 설계도: {node.requiredBlueprints}장";
            if (actionButtonText != null) actionButtonText.text = "해금하기";

            actionButton.onClick.AddListener(TryUnlockWeapon);
        }
    }

    // =========================================================
    // 해금하기 버튼 클릭 시 로직
    // =========================================================
    private void TryUnlockWeapon()
    {
        if (currentSelectedTreeIndex < 0 || currentSelectedNodeIndex < 0) return;

        // 원본 구조체를 복사해와서 수정
        WeaponTree tree = weaponTrees[currentSelectedTreeIndex];
        UpgradeNode node = tree.nodes[currentSelectedNodeIndex];

        // TODO: 나중에 이 부분에 실제 플레이어의 인벤토리(설계도 갯수) 확인 및 차감 로직이 들어갑니다.
        // ex) if (PlayerInventory.Blueprints >= node.requiredBlueprints) { PlayerInventory.Blueprints -= node.requiredBlueprints; ... }

        Debug.Log($"[시스템] {node.weaponPrefab.WeaponName} 무기를 해금했습니다!");

        // 해금 상태 true로 변경 후 다시 리스트에 저장 (구조체 갱신)
        node.isUnlocked = true;
        tree.nodes[currentSelectedNodeIndex] = node;
        weaponTrees[currentSelectedTreeIndex] = tree;

        // 우측 패널 UI(장착하기 버튼) 즉시 갱신
        UpdateRightPanelActionUI(node);
    }

    // =========================================================
    // 장착하기 버튼 클릭 시 로직
    // =========================================================
    private void EquipSelectedWeapon()
    {
        if (currentPlayerWeaponManager == null || currentSelectedTreeIndex < 0 || currentSelectedNodeIndex < 0) return;

        UpgradeNode node = weaponTrees[currentSelectedTreeIndex].nodes[currentSelectedNodeIndex];

        currentPlayerWeaponManager.EquipWeapon(node.weaponPrefab);
        Debug.Log($"[시스템] {node.weaponPrefab.WeaponName} 장착 완료!");
    }
}