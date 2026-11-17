using UnityEngine;

public class PlayerLoadoutManager : MonoBehaviour
{
    public static PlayerLoadoutManager Instance
    {
        get;
        private set;
    }


    // =========================================================
    // Current Weapon
    // =========================================================

    [Header("Current Weapon")]

    [SerializeField]
    private string equippedWeaponId;


    [SerializeField]
    private WeaponBase equippedWeaponPrefab;


    public string EquippedWeaponId =>
        equippedWeaponId;


    public WeaponBase EquippedWeaponPrefab =>
        equippedWeaponPrefab;


    public bool HasSavedWeapon =>
        equippedWeaponPrefab != null ||
        !string.IsNullOrEmpty(equippedWeaponId);


    // =========================================================
    // Singleton
    // =========================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);

            return;
        }


        Instance =
            this;


        DontDestroyOnLoad(
            gameObject
        );
    }


    // =========================================================
    // Weapon 저장
    // =========================================================

    public void SaveWeapon(
        WeaponBase weaponPrefab
    )
    {
        if (weaponPrefab == null)
            return;


        equippedWeaponId =
            weaponPrefab.WeaponId;


        // 중요한 부분:
        // 생성된 Clone이 아니라
        // EquipWeapon에 전달된 원본 Prefab을 저장
        equippedWeaponPrefab =
            weaponPrefab;


        Debug.Log(
            $"[PlayerLoadout] Weapon Saved : " +
            $"{equippedWeaponId} / {weaponPrefab.name}"
        );
    }


    // =========================================================
    // 저장된 Weapon 가져오기
    // =========================================================

    public WeaponBase GetSavedWeapon()
    {
        // 1순위 : 직접 저장한 Prefab
        if (equippedWeaponPrefab != null)
        {
            return equippedWeaponPrefab;
        }


        // 2순위 : WeaponCatalog에서 ID 검색
        if (!string.IsNullOrEmpty(
                equippedWeaponId) &&
            WeaponCatalog.Instance != null)
        {
            WeaponBase catalogWeapon =
                WeaponCatalog.Instance.GetWeapon(
                    equippedWeaponId
                );


            if (catalogWeapon != null)
            {
                equippedWeaponPrefab =
                    catalogWeapon;


                return catalogWeapon;
            }
        }


        return null;
    }


    // =========================================================
    // 새 게임 등에서 초기화
    // =========================================================

    public void ClearWeapon()
    {
        equippedWeaponId =
            string.Empty;


        equippedWeaponPrefab =
            null;


        Debug.Log(
            "[PlayerLoadout] Weapon Data Cleared"
        );
    }
}