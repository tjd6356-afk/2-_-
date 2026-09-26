using TMPro;
using UnityEngine;

public class AmmoUI : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField]
    private PlayerWeaponManager weaponManager;


    [Header("Ammo Sources")]

    [Tooltip("제작자 전용 무기의 총")]
    [SerializeField]
    private PlayerShooter developerShooter;


    [Tooltip("일반 원거리 무기")]
    [SerializeField]
    private RangedGunWeapon rangedGunWeapon;


    [Header("UI")]
    [SerializeField]
    private TMP_Text ammoText;


    private void Awake()
    {
        if (ammoText == null)
        {
            ammoText =
                GetComponent<TMP_Text>();
        }
    }


    private void OnEnable()
    {
        // ==========================================
        // 무기 변경
        // ==========================================

        if (weaponManager != null)
        {
            weaponManager.OnWeaponChanged +=
                HandleWeaponChanged;
        }


        // ==========================================
        // 제작자 무기 탄약
        // ==========================================

        if (developerShooter != null)
        {
            developerShooter.OnAmmoChanged +=
                HandleDeveloperAmmoChanged;


            developerShooter.OnReloadStateChanged +=
                HandleDeveloperReloadChanged;
        }


        // ==========================================
        // 원거리 무기 탄약
        // ==========================================

        if (rangedGunWeapon != null)
        {
            rangedGunWeapon.OnAmmoChanged +=
                HandleRangedAmmoChanged;


            rangedGunWeapon.OnReloadStateChanged +=
                HandleRangedReloadChanged;
        }
    }


    private void Start()
    {
        RefreshUI();
    }


    private void OnDisable()
    {
        if (weaponManager != null)
        {
            weaponManager.OnWeaponChanged -=
                HandleWeaponChanged;
        }


        if (developerShooter != null)
        {
            developerShooter.OnAmmoChanged -=
                HandleDeveloperAmmoChanged;


            developerShooter.OnReloadStateChanged -=
                HandleDeveloperReloadChanged;
        }


        if (rangedGunWeapon != null)
        {
            rangedGunWeapon.OnAmmoChanged -=
                HandleRangedAmmoChanged;


            rangedGunWeapon.OnReloadStateChanged -=
                HandleRangedReloadChanged;
        }
    }


    // =========================================================
    // 무기 변경
    // =========================================================

    private void HandleWeaponChanged(
        WeaponType type
    )
    {
        RefreshUI();
    }


    // =========================================================
    // Developer
    // =========================================================

    private void HandleDeveloperAmmoChanged(
        int currentAmmo,
        int magazineSize,
        int reserveMagazines
    )
    {
        if (weaponManager.ActiveWeaponType ==
            WeaponType.Developer)
        {
            RefreshUI();
        }
    }


    private void HandleDeveloperReloadChanged(
        bool value
    )
    {
        if (weaponManager.ActiveWeaponType ==
            WeaponType.Developer)
        {
            RefreshUI();
        }
    }


    // =========================================================
    // Ranged Gun
    // =========================================================

    private void HandleRangedAmmoChanged(
        int currentAmmo,
        int magazineSize,
        int reserveMagazines
    )
    {
        if (weaponManager.ActiveWeaponType ==
            WeaponType.RangedGun)
        {
            RefreshUI();
        }
    }


    private void HandleRangedReloadChanged(
        bool value
    )
    {
        if (weaponManager.ActiveWeaponType ==
            WeaponType.RangedGun)
        {
            RefreshUI();
        }
    }


    // =========================================================
    // 실제 UI
    // =========================================================

    private void RefreshUI()
    {
        if (ammoText == null ||
            weaponManager == null)
        {
            return;
        }


        switch (
            weaponManager.ActiveWeaponType
        )
        {
            // ==========================================
            // 제작자 무기
            // ==========================================

            case WeaponType.Developer:

                if (developerShooter == null)
                {
                    HideAmmo();
                    return;
                }


                ShowAmmo();


                ammoText.text =
                    $"탄창 {developerShooter.ReserveMagazines}" +
                    $"\n탄약 {developerShooter.CurrentAmmo} / {developerShooter.MagazineSize}";


                if (developerShooter.IsReloading)
                {
                    ammoText.text +=
                        "\n재장전 중...";
                }

                break;


            // ==========================================
            // 원거리 총
            // ==========================================

            case WeaponType.RangedGun:

                if (rangedGunWeapon == null)
                {
                    HideAmmo();
                    return;
                }


                ShowAmmo();


                ammoText.text =
                    $"탄창 {rangedGunWeapon.ReserveMagazines}" +
                    $"\n탄약 {rangedGunWeapon.CurrentAmmo} / {rangedGunWeapon.MagazineSize}";


                if (rangedGunWeapon.IsReloading)
                {
                    ammoText.text +=
                        "\n재장전 중...";
                }

                break;


            // ==========================================
            // 근접무기 / 와이어건
            // 탄약 UI 필요 없음
            // ==========================================

            case WeaponType.Melee:
            case WeaponType.WireGun:

                HideAmmo();

                break;
        }
    }


    private void ShowAmmo()
    {
        ammoText.enabled =
            true;
    }


    private void HideAmmo()
    {
        ammoText.enabled =
            false;
    }
}