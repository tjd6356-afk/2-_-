using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponUpgradePanelUI : MonoBehaviour
{
    [Header("UI Controls")]
    [Tooltip("패널을 닫을 UI 버튼 (X 버튼 등)")]
    [SerializeField] private Button closeButton;

    // =========================================================
    // UI 노드 구조체
    // =========================================================
    [Serializable]
    public struct UpgradeNode
    {
        public Button nodeButton;
        public WeaponBase weaponPrefab;
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

    // 모루(Anvil) 스크립트 쪽에 '나 닫혔어! 캐릭터 움직이게 해줘!' 라고 알려줄 연락처(콜백)
    private Action onCloseCallback;

    // =========================================================
    // 모루에서 호출하는 열기 함수 (닫기 콜백 추가)
    // =========================================================
    public void OpenPanel(PlayerWeaponManager playerManager, Action onClose)
    {
        if (playerManager == null) return;

        currentPlayerWeaponManager = playerManager;
        onCloseCallback = onClose; // 닫힐 때 실행할 함수 저장

        gameObject.SetActive(true);

        // 닫기 버튼 이벤트 연결
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePanel);
        }

        WeaponType currentType = currentPlayerWeaponManager.ActiveWeaponType;

        foreach (WeaponTree tree in weaponTrees)
        {
            if (tree.treeUIRoot != null)
                tree.treeUIRoot.SetActive(false);
        }

        foreach (WeaponTree tree in weaponTrees)
        {
            if (tree.weaponType == currentType)
            {
                if (tree.treeUIRoot != null)
                    tree.treeUIRoot.SetActive(true);

                foreach (UpgradeNode node in tree.nodes)
                {
                    if (node.nodeButton != null && node.weaponPrefab != null)
                    {
                        node.nodeButton.onClick.RemoveAllListeners();

                        WeaponBase targetPrefab = node.weaponPrefab;
                        node.nodeButton.onClick.AddListener(() => OnNodeClicked(targetPrefab));
                    }
                }
                break;
            }
        }
    }

    // =========================================================
    // 닫기 버튼을 눌렀을 때 실행되는 함수
    // =========================================================
    private void ClosePanel()
    {
        // AnvilInteraction 쪽에 닫힌다는 신호를 보내 캐릭터 조작 잠금을 해제시킴
        onCloseCallback?.Invoke();
    }

    private void OnNodeClicked(WeaponBase selectedWeaponPrefab)
    {
        if (currentPlayerWeaponManager == null) return;

        currentPlayerWeaponManager.EquipWeapon(selectedWeaponPrefab);
        Debug.Log($"[업그레이드 패널] {selectedWeaponPrefab.WeaponName} 장착 완료!");
    }
}