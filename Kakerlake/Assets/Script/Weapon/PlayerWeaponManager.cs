using System;
using System.Collections.Generic;
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


    // =========================================================
    // Ranged Weapon Aim Pitch (총기류 상하 조준 회전)
    // =========================================================

    [Header("Ranged Weapon Aim Pitch (총기류 상하 조준)")]
    [Tooltip("원거리 무기 조준 시 총구가 시점을 따라 위아래로 회전할지 여부")]
    [SerializeField] private bool enableRangedAimPitch = true;

    [Tooltip("체크 시 우클릭 조준 중에만 총구가 위아래로 움직이고, 해제하면 평상시에도 항상 시점을 따라갑니다.")]
    [SerializeField] private bool pitchOnlyWhileAiming = true;

    [Tooltip("상하 조준 회전을 적용하지 않을 무기 타입 (근접 무기 제외용)")]
    [SerializeField]
    private List<WeaponType> pitchExcludedWeaponTypes =
        new List<WeaponType> { WeaponType.Melee };

    [Tooltip("조준 시 총구가 위아래로 따라가는 회전 속도")]
    [Min(1f)]
    [SerializeField] private float aimPitchRotateSpeed = 20f;

    [Tooltip("무기가 위쪽으로 들릴 수 있는 최대 각도 (음수 = 위쪽)")]
    [Range(-85f, 0f)]
    [SerializeField] private float minAimPitch = -70f;

    [Tooltip("무기가 아래쪽으로 숙여질 수 있는 최대 각도 (양수 = 아래쪽)")]
    [Range(0f, 85f)]
    [SerializeField] private float maxAimPitch = 70f;

    [Tooltip("(선택) 조준 시 Weapon Mount의 로컬 위치 오프셋 (기본 0,0,0)")]
    [SerializeField] private Vector3 aimMountPositionOffset = Vector3.zero;


    // 현재 생성되어 있는 실제 무기
    private WeaponBase currentWeapon;


    // 현재 가까이에 있는 무기 Pickup
    private WeaponPickup nearbyPickup;


    // Aim Pitch Runtime
    private ThirdPersonCamera thirdPersonCamera;
    private Vector3 defaultMountLocalPosition;
    private Quaternion defaultMountLocalRotation = Quaternion.identity;
    private float currentAimPitch;
    private Vector3 currentMountPositionOffset;


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


        if (playerCamera != null)
        {
            thirdPersonCamera =
                playerCamera.GetComponent<ThirdPersonCamera>();
        }


        if (weaponMount != null)
        {
            defaultMountLocalPosition =
                weaponMount.localPosition;

            defaultMountLocalRotation =
                weaponMount.localRotation;
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


    private void LateUpdate()
    {
        UpdateRangedAimPitch();
    }


    // =========================================================
    // 총기류 상하 조준 회전 처리
    // =========================================================

    private void UpdateRangedAimPitch()
    {
        if (weaponMount == null)
            return;

        float targetPitch = 0f;
        Vector3 targetPosOffset = Vector3.zero;

        if (CanApplyRangedAimPitch())
        {
            targetPitch = GetCameraLocalPitch();
            targetPosOffset = aimMountPositionOffset;
        }

        currentAimPitch = Mathf.LerpAngle(
            currentAimPitch,
            targetPitch,
            aimPitchRotateSpeed * Time.deltaTime
        );

        currentMountPositionOffset = Vector3.Lerp(
            currentMountPositionOffset,
            targetPosOffset,
            aimPitchRotateSpeed * Time.deltaTime
        );

        weaponMount.localRotation =
            defaultMountLocalRotation *
            Quaternion.Euler(currentAimPitch, 0f, 0f);

        weaponMount.localPosition =
            defaultMountLocalPosition +
            currentMountPositionOffset;
    }

    private bool CanApplyRangedAimPitch()
    {
        if (!enableRangedAimPitch)
            return false;

        if (currentWeapon == null)
            return false;

        if (pitchExcludedWeaponTypes != null &&
            pitchExcludedWeaponTypes.Contains(currentWeapon.WeaponType))
        {
            return false;
        }

        if (playerStats != null && playerStats.IsDead)
            return false;

        if (PlayerGameplayControl.GameplayLocked)
            return false;

        if (pitchOnlyWhileAiming)
        {
            bool isAiming =
                thirdPersonCamera != null
                ? thirdPersonCamera.IsAiming
                : (Mouse.current != null && Mouse.current.rightButton.isPressed);

            if (!isAiming)
                return false;
        }

        return true;
    }

    private float GetCameraLocalPitch()
    {
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main;
            thirdPersonCamera = playerCamera.GetComponent<ThirdPersonCamera>();
        }

        if (playerCamera == null)
            return 0f;

        Transform referenceParent =
            weaponMount.parent != null
            ? weaponMount.parent
            : transform;

        Vector3 localCamForward =
            referenceParent.InverseTransformDirection(
                playerCamera.transform.forward
            );

        if (localCamForward.sqrMagnitude < 0.0001f)
            return 0f;

        localCamForward.Normalize();

        float rawPitch =
            -Mathf.Asin(Mathf.Clamp(localCamForward.y, -0.98f, 0.98f)) *
            Mathf.Rad2Deg;

        return Mathf.Clamp(rawPitch, minAimPitch, maxAimPitch);
    }

    private void ResetWeaponMountTransform()
    {
        currentAimPitch = 0f;
        currentMountPositionOffset = Vector3.zero;

        if (weaponMount != null)
        {
            weaponMount.localPosition = defaultMountLocalPosition;
            weaponMount.localRotation = defaultMountLocalRotation;
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


        // 무기 교체 시 Mount 회전/위치 초기화
        ResetWeaponMountTransform();


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