using TMPro;
using UnityEngine;

public class AmmoUI : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private PlayerWeaponManager weaponManager;

    [SerializeField]
    private TMP_Text ammoText;


    // 현재 연결되어 있는 탄약 무기
    private IAmmoWeapon currentAmmoWeapon;


    private void Awake()
    {
        if (ammoText == null)
        {
            ammoText =
                GetComponent<TMP_Text>();
        }


        if (weaponManager == null)
        {
            weaponManager =
                FindFirstObjectByType
                <PlayerWeaponManager>();
        }
    }


    private void OnEnable()
    {
        if (weaponManager != null)
        {
            weaponManager.OnWeaponChanged +=
                HandleWeaponChanged;
        }
    }


    private void Start()
    {
        /*
         * OnWeaponChanged를 놓친 경우를 대비.
         *
         * 예를 들어 UI의 Start 순서보다
         * PlayerWeaponManager가 먼저 실행됐을 수도 있음.
         */

        if (weaponManager != null)
        {
            ConnectToWeapon(
                weaponManager.CurrentWeapon
            );
        }
        else
        {
            HideAmmoUI();
        }
    }


    private void OnDisable()
    {
        if (weaponManager != null)
        {
            weaponManager.OnWeaponChanged -=
                HandleWeaponChanged;
        }


        DisconnectCurrentWeapon();
    }


    // =========================================================
    // 무기가 바뀜
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
    // 현재 무기와 UI 연결
    // =========================================================

    private void ConnectToWeapon(
        WeaponBase weapon
    )
    {
        // 이전 무기 이벤트 해제
        DisconnectCurrentWeapon();


        if (weapon == null)
        {
            HideAmmoUI();

            return;
        }


        // ==========================================
        // 현재 무기가 탄약을 사용하는 무기인가?
        // ==========================================

        currentAmmoWeapon =
            weapon as IAmmoWeapon;


        // 근접 / 와이어 등
        if (currentAmmoWeapon == null)
        {
            HideAmmoUI();

            return;
        }


        // ==========================================
        // 탄약 무기
        // ==========================================

        currentAmmoWeapon.OnAmmoStateChanged +=
            RefreshUI;


        ShowAmmoUI();


        RefreshUI();
    }


    // =========================================================
    // 기존 무기 이벤트 연결 제거
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
    // UI 실제 갱신
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


        ammoText.enabled =
            true;


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


    private void ShowAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.enabled =
                true;
        }
    }


    private void HideAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.enabled =
                false;
        }
    }
}