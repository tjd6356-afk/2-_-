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
        // ==========================================
        // 이전 Scene에서 저장된 무기가 있다면
        // WeaponCatalog에서 찾아서 장착
        // ==========================================

        WeaponBase weaponToEquip =
            startingWeaponPrefab;


        if (PlayerLoadoutManager.Instance != null &&
            WeaponCatalog.Instance != null)
        {
            string savedWeaponId =
                PlayerLoadoutManager
                    .Instance
                    .EquippedWeaponId;


            WeaponBase savedPrefab =
                WeaponCatalog.Instance
                    .GetWeapon(savedWeaponId);


            if (savedPrefab != null)
            {
                weaponToEquip =
                    savedPrefab;
            }
        }


        if (weaponToEquip != null)
        {
            EquipWeapon(
                weaponToEquip
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
                    currentWeapon.WeaponId
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