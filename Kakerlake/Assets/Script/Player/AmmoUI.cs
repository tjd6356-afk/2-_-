using System.Collections;
using TMPro;
using UnityEngine;

public class AmmoUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private TMP_Text ammoText;

    [Tooltip("탄약 UI 전체를 숨기고 싶다면 지정. 비워도 됨.")]
    [SerializeField]
    private GameObject ammoUIRoot;


    // 런타임에 생성된 Player에서 가져옴
    private PlayerWeaponManager weaponManager;

    // 현재 장착된 탄약 무기
    private IAmmoWeapon currentAmmoWeapon;

    private Coroutine findPlayerRoutine;


    private void Awake()
    {
        if (ammoText == null)
        {
            ammoText =
                GetComponent<TMP_Text>();
        }
    }


    private void Start()
    {
        // Player가 DungeonGenerator에 의해
        // 나중에 생성될 수 있으므로 기다리면서 검색
        BeginFindPlayer();
    }


    private void OnDisable()
    {
        DisconnectWeaponManager();

        if (findPlayerRoutine != null)
        {
            StopCoroutine(
                findPlayerRoutine
            );

            findPlayerRoutine = null;
        }
    }


    // =========================================================
    // Player 찾기 시작
    // =========================================================

    private void BeginFindPlayer()
    {
        if (findPlayerRoutine != null)
        {
            StopCoroutine(
                findPlayerRoutine
            );
        }


        findPlayerRoutine =
            StartCoroutine(
                FindPlayerRoutine()
            );
    }


    // =========================================================
    // 런타임 생성 Player 기다리기
    // =========================================================

    private IEnumerator FindPlayerRoutine()
    {
        HideAmmoUI();


        while (weaponManager == null)
        {
            weaponManager =
                FindFirstObjectByType<PlayerWeaponManager>();


            if (weaponManager == null)
            {
                yield return null;
            }
        }


        BindWeaponManager(
            weaponManager
        );


        findPlayerRoutine =
            null;
    }


    // =========================================================
    // 외부에서 생성된 Player를 직접 전달할 수도 있음
    // =========================================================

    public void SetPlayer(
        GameObject player
    )
    {
        if (player == null)
            return;


        PlayerWeaponManager newManager =
            player.GetComponent<PlayerWeaponManager>();


        if (newManager == null)
        {
            newManager =
                player.GetComponentInChildren<PlayerWeaponManager>();
        }


        if (newManager == null)
        {
            Debug.LogWarning(
                "[AmmoUI] 생성된 Player에서 PlayerWeaponManager를 찾지 못했습니다."
            );

            return;
        }


        BindWeaponManager(
            newManager
        );
    }


    // =========================================================
    // PlayerWeaponManager와 연결
    // =========================================================

    private void BindWeaponManager(
        PlayerWeaponManager newManager
    )
    {
        if (newManager == null)
            return;


        // 기존 연결 해제
        DisconnectWeaponManager();


        weaponManager =
            newManager;


        weaponManager.OnWeaponChanged +=
            HandleWeaponChanged;


        Debug.Log(
            $"[AmmoUI] Player 연결 성공 : {weaponManager.gameObject.name}"
        );


        // 이미 무기를 장착한 상태일 수도 있으므로 즉시 확인
        ConnectToWeapon(
            weaponManager.CurrentWeapon
        );
    }


    // =========================================================
    // PlayerWeaponManager 연결 해제
    // =========================================================

    private void DisconnectWeaponManager()
    {
        DisconnectCurrentWeapon();


        if (weaponManager != null)
        {
            weaponManager.OnWeaponChanged -=
                HandleWeaponChanged;
        }


        weaponManager =
            null;
    }


    // =========================================================
    // 무기 변경
    // =========================================================

    private void HandleWeaponChanged(
        WeaponBase weapon
    )
    {
        ConnectToWeapon(
            weapon
        );
    }


    // =========================================================
    // 현재 무기 연결
    // =========================================================

    private void ConnectToWeapon(
        WeaponBase weapon
    )
    {
        DisconnectCurrentWeapon();


        if (weapon == null)
        {
            HideAmmoUI();

            return;
        }


        // 현재 무기가 탄약을 사용하는 무기인지 확인
        currentAmmoWeapon =
            weapon as IAmmoWeapon;


        // 근접무기 / 와이어건 등
        if (currentAmmoWeapon == null)
        {
            HideAmmoUI();

            return;
        }


        currentAmmoWeapon.OnAmmoStateChanged +=
            RefreshUI;


        ShowAmmoUI();

        RefreshUI();
    }


    // =========================================================
    // 기존 탄약 무기 연결 해제
    // =========================================================

    private void DisconnectCurrentWeapon()
    {
        if (currentAmmoWeapon == null)
            return;


        currentAmmoWeapon.OnAmmoStateChanged -=
            RefreshUI;


        currentAmmoWeapon =
            null;
    }


    // =========================================================
    // UI 갱신
    // =========================================================

    private void RefreshUI()
    {
        if (ammoText == null)
            return;


        if (currentAmmoWeapon == null)
        {
            HideAmmoUI();

            return;
        }


        ammoText.text =
            $"탄창 {currentAmmoWeapon.ReserveMagazines}" +
            $"\n탄약 {currentAmmoWeapon.CurrentAmmo} / " +
            $"{currentAmmoWeapon.MagazineSize}";


        if (currentAmmoWeapon.IsReloading)
        {
            ammoText.text +=
                "\n재장전 중...";
        }
    }


    // =========================================================
    // UI 표시
    // =========================================================

    private void ShowAmmoUI()
    {
        if (ammoUIRoot != null)
        {
            ammoUIRoot.SetActive(
                true
            );
        }


        if (ammoText != null)
        {
            ammoText.enabled =
                true;
        }
    }


    // =========================================================
    // UI 숨김
    // =========================================================

    private void HideAmmoUI()
    {
        if (ammoUIRoot != null)
        {
            // AmmoUI 스크립트가 ammoUIRoot에 붙어있다면
            // 자기 자신을 꺼버리면 다시 찾지 못하므로
            // 그 경우에는 Root를 끄지 않는다.
            if (ammoUIRoot != gameObject)
            {
                ammoUIRoot.SetActive(
                    false
                );
            }
        }


        if (ammoText != null)
        {
            ammoText.enabled =
                false;
        }
    }
}