using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeaponManager : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField] private WeaponBase[] weapons;

    [Header("Start Weapon")]
    [SerializeField]
    private WeaponType startingWeapon =
        WeaponType.Developer;


    private WeaponBase currentWeapon;

    private WeaponPickup nearbyPickup;


    public WeaponType ActiveWeaponType =>
        currentWeapon != null
            ? currentWeapon.Type
            : WeaponType.Developer;


    public bool HasNearbyPickup =>
        nearbyPickup != null;


    private void Start()
    {
        // 처음에는 모든 무기 끄기
        foreach (WeaponBase weapon in weapons)
        {
            if (weapon == null)
                continue;

            weapon.Unequip();
        }

        EquipWeapon(startingWeapon);
    }


    private void Update()
    {
        if (Keyboard.current == null)
            return;


        // ==========================================
        // 무기 오브젝트 근처에서 E
        // ==========================================

        if (nearbyPickup != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            nearbyPickup.Use(this);
        }
    }


    public void EquipWeapon(WeaponType type)
    {
        foreach (WeaponBase weapon in weapons)
        {
            if (weapon == null)
                continue;


            if (weapon.Type == type)
            {
                currentWeapon = weapon;

                weapon.Equip();

                Debug.Log(
                    $"Weapon Equipped : {type}"
                );
            }
            else
            {
                weapon.Unequip();
            }
        }
    }


    public void RegisterPickup(
        WeaponPickup pickup
    )
    {
        nearbyPickup = pickup;
    }


    public void UnregisterPickup(
        WeaponPickup pickup
    )
    {
        if (nearbyPickup == pickup)
        {
            nearbyPickup = null;
        }
    }
}