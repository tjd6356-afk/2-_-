using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeaponManager : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField]
    private WeaponBase[] weapons;


    [Header("Start Weapon")]
    [SerializeField]
    private WeaponType startingWeapon =
        WeaponType.Developer;


    // 현재 장착 무기
    private WeaponBase currentWeapon;

    // 현재 Player가 가까이 있는 무기 Pickup
    private WeaponPickup nearbyPickup;


    // =========================================================
    // 외부 접근
    // =========================================================

    public WeaponType ActiveWeaponType
    {
        get
        {
            if (currentWeapon != null)
            {
                return currentWeapon.Type;
            }

            return WeaponType.Developer;
        }
    }


    public bool HasNearbyPickup
    {
        get
        {
            return nearbyPickup != null;
        }
    }


    // =========================================================
    // 무기 변경 이벤트
    // AmmoUI 등이 사용
    // =========================================================

    public event Action<WeaponType> OnWeaponChanged;


    // =========================================================
    // 시작
    // =========================================================

    private void Start()
    {
        // ==========================================
        // 우선 모든 무기 해제
        // ==========================================

        foreach (WeaponBase weapon in weapons)
        {
            if (weapon == null)
                continue;

            weapon.Unequip();
        }


        // ==========================================
        // 기본 시작 무기
        // ==========================================

        WeaponType weaponToEquip =
            startingWeapon;


        // ==========================================
        // 이전 Scene에서 저장한 무기가 있다면
        // 그 무기를 사용
        // ==========================================

        if (PlayerLoadoutManager.Instance != null)
        {
            weaponToEquip =
                PlayerLoadoutManager
                    .Instance
                    .EquippedWeapon;
        }


        // ==========================================
        // 저장된 무기를 장착할 수 있는지 확인
        // ==========================================

        if (!HasWeapon(weaponToEquip))
        {
            Debug.LogWarning(
                $"저장된 무기 {weaponToEquip}가 현재 Player에 없습니다. " +
                $"{startingWeapon}을 장착합니다."
            );


            weaponToEquip =
                startingWeapon;
        }


        EquipWeapon(
            weaponToEquip
        );
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        if (Keyboard.current == null)
            return;


        // ==========================================
        // 무기 Pickup 가까이에서 E
        // ==========================================

        if (nearbyPickup != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            nearbyPickup.Use(
                this
            );
        }
    }


    // =========================================================
    // 무기 존재 확인
    // =========================================================

    private bool HasWeapon(
        WeaponType type
    )
    {
        foreach (WeaponBase weapon in weapons)
        {
            if (weapon == null)
                continue;


            if (weapon.Type == type)
            {
                return true;
            }
        }


        return false;
    }


    // =========================================================
    // 무기 장착
    // =========================================================

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
        // 다른 무기 모두 해제
        // ==========================================

        foreach (WeaponBase weapon in weapons)
        {
            if (weapon == null)
                continue;


            if (weapon ==
                targetWeapon)
            {
                continue;
            }


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
        // Scene 전환용 무기 저장
        // ==========================================

        if (PlayerLoadoutManager.Instance != null)
        {
            PlayerLoadoutManager
                .Instance
                .SaveWeapon(type);
        }


        // ==========================================
        // UI 등에 무기 변경 알림
        // ==========================================

        OnWeaponChanged?.Invoke(
            type
        );
    }


    // =========================================================
    // WeaponPickup이 Player 범위에 들어왔을 때 호출
    // =========================================================

    public void RegisterPickup(
        WeaponPickup pickup
    )
    {
        if (pickup == null)
            return;


        nearbyPickup =
            pickup;


        Debug.Log(
            $"Weapon Pickup 등록 : {pickup.gameObject.name}"
        );
    }


    // =========================================================
    // WeaponPickup 범위에서 나갔을 때 호출
    // =========================================================

    public void UnregisterPickup(
        WeaponPickup pickup
    )
    {
        if (pickup == null)
            return;


        // 현재 등록되어 있는 Pickup과
        // 같은 오브젝트일 때만 제거
        if (nearbyPickup == pickup)
        {
            nearbyPickup =
                null;


            Debug.Log(
                $"Weapon Pickup 해제 : {pickup.gameObject.name}"
            );
        }
    }
}