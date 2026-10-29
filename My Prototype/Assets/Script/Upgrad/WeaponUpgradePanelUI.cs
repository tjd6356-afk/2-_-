using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponUpgradePanelUI : MonoBehaviour
{
    // =========================================================
    // 1. 단일 노드 (버튼 + 무기 프리팹)
    // =========================================================
    [Serializable]
    public struct UpgradeNode
    {
        [Tooltip("클릭할 UI 버튼")]
        public Button nodeButton;

        [Tooltip("해당 노드를 눌렀을 때 장착될 무기 프리팹")]
        public WeaponBase weaponPrefab;
    }

    // =========================================================
    // 2. 무기 타입별 개별 트리 정보
    // =========================================================
    [Serializable]
    public struct WeaponTree
    {
        [Tooltip("어떤 무기를 들고 있을 때 이 트리를 띄울 것인가?")]
        public WeaponType weaponType;

        [Tooltip("이 트리에 해당하는 UI 그룹 부모 객체 (선택 사항)")]
        public GameObject treeUIRoot;

        [Tooltip("이 트리에 속한 업그레이드 노드들")]
        public List<UpgradeNode> nodes;
    }

    [Header("Upgrade Trees by Weapon Type")]
    [Tooltip("각 무기 타입(권총, 샷건 등)별로 트리를 추가하세요.")]
    public List<WeaponTree> weaponTrees = new List<WeaponTree>();

    // 캐싱된 플레이어 무기 매니저
    private PlayerWeaponManager currentPlayerWeaponManager;

    // =========================================================
    // 모루(AnvilInteraction)에서 호출하는 열기 함수
    // =========================================================
    public void OpenPanel(PlayerWeaponManager playerManager)
    {
        if (playerManager == null)
        {
            Debug.LogError("[업그레이드 패널] 플레이어 무기 매니저 정보가 없습니다!");
            return;
        }

        currentPlayerWeaponManager = playerManager;

        // 1. 최상단 패널 활성화
        gameObject.SetActive(true);

        // 2. 현재 플레이어가 들고 있는 무기의 타입 가져오기
        WeaponType currentType = currentPlayerWeaponManager.ActiveWeaponType;

        // 3. 일단 캔버스에 있는 모든 트리 UI 그룹을 숨김
        foreach (WeaponTree tree in weaponTrees)
        {
            if (tree.treeUIRoot != null)
                tree.treeUIRoot.SetActive(false);
        }

        // 4. 현재 무기 타입과 일치하는 트리만 찾아서 켜주고 이벤트 연결
        bool treeFound = false;
        foreach (WeaponTree tree in weaponTrees)
        {
            if (tree.weaponType == currentType)
            {
                treeFound = true;

                // 해당 무기의 전용 UI 그룹 활성화
                if (tree.treeUIRoot != null)
                    tree.treeUIRoot.SetActive(true);

                // 리스트에 등록된 노드(버튼)들에 클릭 이벤트 할당
                foreach (UpgradeNode node in tree.nodes)
                {
                    if (node.nodeButton != null && node.weaponPrefab != null)
                    {
                        // 기존 이벤트 지우기 (중복 호출 방지)
                        node.nodeButton.onClick.RemoveAllListeners();

                        // C# 클로저 버그 방지를 위해 지역 변수에 프리팹을 담아 전달
                        WeaponBase targetPrefab = node.weaponPrefab;

                        node.nodeButton.onClick.AddListener(() => OnNodeClicked(targetPrefab));
                    }
                }

                // 일치하는 트리를 찾았으므로 루프 종료
                break;
            }
        }

        if (!treeFound)
        {
            Debug.LogWarning($"[업그레이드 패널] 현재 무기 타입({currentType})에 등록된 업그레이드 트리가 없습니다!");
        }
    }

    // =========================================================
    // 무기 노드(버튼)를 클릭했을 때 실행
    // =========================================================
    private void OnNodeClicked(WeaponBase selectedWeaponPrefab)
    {
        if (currentPlayerWeaponManager == null) return;

        // PlayerWeaponManager에 이미 구현되어 있는 멋진 장착 함수 호출!
        // 알아서 기존 무기를 지우고, 새 무기를 Instantiate하고, 저장까지 해줍니다.
        currentPlayerWeaponManager.EquipWeapon(selectedWeaponPrefab);

        Debug.Log($"[업그레이드 패널] {selectedWeaponPrefab.WeaponName} 장착 완료!");
    }
}