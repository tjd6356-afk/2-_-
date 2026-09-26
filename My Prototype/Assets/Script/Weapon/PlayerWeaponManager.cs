using System;
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

    public event Action<WeaponType>
    OnWeaponChanged;

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


    public void EquipWeapon(
    WeaponType type
)
    {
        WeaponBase targetWeapon =
            null;


        // ==========================================
        // 장착할 무기 찾기
        // ==========================================

        foreach (WeaponBase weapon in weapons)
        {
            if (weapon == null)
                continue;


            if (weapon.Type == type)
            {
                targetWeapon =
                    weapon;

                break;
            }
        }


        if (targetWeapon == null)
        {
            Debug.LogError(
                $"Weapon을 찾을 수 없습니다 : {type}"
            );

            return;
        }


        // ==========================================
        // 모든 무기 해제
        // ==========================================

        foreach (WeaponBase weapon in weapons)
        {
            if (weapon == null)
                continue;


            if (weapon == targetWeapon)
                continue;


            weapon.Unequip();
        }


        // ==========================================
        // 새 무기 장착
        // ==========================================

        currentWeapon =
            targetWeapon;


        currentWeapon.Equip();


        Debug.Log(
            $"Weapon Equipped : {type}"
        );


        // ==========================================
        // UI 등에 무기 변경 알림
        // ==========================================

        OnWeaponChanged?.Invoke(
            type
        );
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