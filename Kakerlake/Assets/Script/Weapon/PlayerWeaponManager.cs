using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeaponManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform weaponMount;

    [SerializeField] private PlayerStats playerStats;

    [SerializeField] private Camera playerCamera;

    [SerializeField] private CharacterController characterController;


    [Header("Start Weapon")]
    [SerializeField] private WeaponBase startingWeaponPrefab;


    // 현재 생성되어 있는 실제 무기
    private WeaponBase currentWeapon;


    // 현재 가까이에 있는 무기 Pickup
    private WeaponPickup nearbyPickup;

    public WeaponType ActiveWeaponType
    {
        get
        {
            if (currentWeapon != null)
            {
                return currentWeapon.WeaponType;
            }

            return WeaponType.Developer;
        }
    }

    public WeaponBase CurrentWeapon =>
        currentWeapon;


    public bool HasNearbyPickup =>
        nearbyPickup != null;


    // UI나 다른 시스템에서 사용
    public event Action<WeaponBase> OnWeaponChanged;


    private void Awake()
    {
        if (playerStats == null)
        {
            playerStats =
                GetComponent<PlayerStats>();
        }


        if (characterController == null)
        {
            characterController =
                GetComponent<CharacterController>();
        }


        if (playerCamera == null)
        {
            playerCamera =
                Camera.main;
        }
    }


    private void Start()
    {
        WeaponBase weaponToEquip =
            null;


        // =========================================================
        // 1순위 : 이전 Scene에서 선택했던 Weapon
        // =========================================================

        if (PlayerLoadoutManager.Instance != null)
        {
            weaponToEquip =
                PlayerLoadoutManager
                    .Instance
                    .GetSavedWeapon();


            if (weaponToEquip != null)
            {
                Debug.Log(
                    $"[PlayerWeaponManager] 저장된 Weapon 사용 : " +
                    $"{weaponToEquip.WeaponName}"
                );
            }
        }


        // =========================================================
        // 2순위 : 저장된 Weapon이 없을 때만 기본 Weapon
        // =========================================================

        if (weaponToEquip == null)
        {
            weaponToEquip =
                startingWeaponPrefab;


            if (weaponToEquip != null)
            {
                Debug.Log(
                    $"[PlayerWeaponManager] 저장된 Weapon 없음. " +
                    $"Starting Weapon 사용 : " +
                    $"{weaponToEquip.WeaponName}"
                );
            }
        }


        // =========================================================
        // 장착
        // =========================================================

        if (weaponToEquip != null)
        {
            EquipWeapon(
                weaponToEquip
            );
        }
        else
        {
            Debug.LogWarning(
                "[PlayerWeaponManager] 장착할 Weapon이 없습니다."
            );
        }
    }


    private void Update()
    {
        if (Keyboard.current == null)
            return;


        // ==========================================
        // 무기 Pickup 근처에서 E
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
    // 새로운 무기 장착
    // =========================================================

    public void EquipWeapon(
        WeaponBase weaponPrefab
    )
    {
        if (weaponPrefab == null)
            return;


        // 같은 무기라면 다시 만들지 않음
        if (currentWeapon != null &&
            currentWeapon.WeaponId ==
            weaponPrefab.WeaponId)
        {
            return;
        }


        // ==========================================
        // 기존 무기 제거
        // ==========================================

        if (currentWeapon != null)
        {
            currentWeapon.Unequip();


            Destroy(
                currentWeapon.gameObject
            );


            currentWeapon =
                null;
        }


        // ==========================================
        // 새로운 무기 Prefab 생성
        // ==========================================

        WeaponBase newWeapon =
            Instantiate(
                weaponPrefab,
                weaponMount
            );


        // Mount 기준으로 위치 초기화
        newWeapon.transform.localPosition =
            Vector3.zero;


        newWeapon.transform.localRotation =
            Quaternion.identity;


        currentWeapon =
            newWeapon;


        // ==========================================
        // Player 정보 전달
        // ==========================================

        currentWeapon.Initialize(
            this,
            playerStats,
            playerCamera,
            characterController
        );


        // ==========================================
        // Scene 전환용 저장
        // ==========================================

        if (PlayerLoadoutManager.Instance != null)
        {
            PlayerLoadoutManager
                .Instance
                .SaveWeapon(
                    weaponPrefab
                );
        }


        Debug.Log(
            $"Weapon Equipped : {currentWeapon.WeaponName}"
        );


        // UI 등에 알림
        OnWeaponChanged?.Invoke(
            currentWeapon
        );
    }


    // =========================================================
    // Pickup 등록
    // =========================================================

    public void RegisterPickup(
        WeaponPickup pickup
    )
    {
        nearbyPickup =
            pickup;
    }


    public void UnregisterPickup(
        WeaponPickup pickup
    )
    {
        if (nearbyPickup ==
            pickup)
        {
            nearbyPickup =
                null;
        }
    }
}