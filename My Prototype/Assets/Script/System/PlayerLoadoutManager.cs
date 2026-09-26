using UnityEngine;

public class PlayerLoadoutManager : MonoBehaviour
{
    public static PlayerLoadoutManager Instance
    {
        get;
        private set;
    }


    [Header("Saved Weapon")]

    [SerializeField]
    private WeaponType equippedWeapon =
        WeaponType.Developer;


    public WeaponType EquippedWeapon =>
        equippedWeapon;


    private void Awake()
    {
        // 이미 존재하면 중복 제거
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }


        Instance = this;


        // 씬을 이동해도 삭제되지 않음
        DontDestroyOnLoad(
            gameObject
        );
    }


    // =========================================================
    // 현재 장착 무기 저장
    // =========================================================

    public void SaveWeapon(
        WeaponType weaponType
    )
    {
        equippedWeapon =
            weaponType;


        Debug.Log(
            $"[Loadout] Weapon Saved : {weaponType}"
        );
    }
}