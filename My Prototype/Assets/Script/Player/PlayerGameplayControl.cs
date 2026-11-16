using System.Collections.Generic;
using UnityEngine;

public class PlayerGameplayControl : MonoBehaviour
{
    // =========================================================
    // Global State
    // =========================================================

    public static bool GameplayLocked
    {
        get;
        private set;
    }

    // =========================================================
    // Gameplay Scripts
    // =========================================================

    [Header("게임 조작 중지 대상")]

    [Tooltip("UI가 열렸을 때 비활성화할 플레이어 조작 스크립트들 (WeaponManager 포함 권장)")]
    [SerializeField]
    private List<MonoBehaviour> gameplayBehaviours =
        new List<MonoBehaviour>();

    // =========================================================
    // Camera
    // =========================================================

    [Header("Camera")]

    [Tooltip("UI가 열렸을 때 회전 입력을 막을 ThirdPersonCamera")]
    [SerializeField]
    private ThirdPersonCamera thirdPersonCamera;

    // =========================================================
    // Runtime
    // =========================================================

    private bool controlsLocked;
    public bool ControlsLocked => controlsLocked;

    private bool deathLocked;
    public bool DeathLocked => deathLocked;

    // UI가 열릴 때 실제로 켜져 있던 무기만 기억해 두었다가 해제 시 복구
    private readonly List<WeaponBase> disabledWeapons = new List<WeaponBase>();

    // =========================================================
    // Lock
    // =========================================================

    public void LockControls()
    {
        if (controlsLocked)
            return;

        controlsLocked = true;
        GameplayLocked = true;

        // Player 조작 Script 비활성화
        foreach (MonoBehaviour behaviour in gameplayBehaviours)
        {
            if (behaviour == null || behaviour == this)
                continue;

            behaviour.enabled = false;
        }

        // 현재 장착되어 활성화된 모든 무기 스크립트 일괄 비활성화
        disabledWeapons.Clear();
        WeaponBase[] activeWeapons = GetComponentsInChildren<WeaponBase>(false);
        for (int i = 0; i < activeWeapons.Length; i++)
        {
            WeaponBase weapon = activeWeapons[i];
            if (weapon != null && weapon.enabled)
            {
                weapon.enabled = false;
                disabledWeapons.Add(weapon);
            }
        }

        // Camera 입력 정지
        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.enabled = false;
        }

        // Mouse
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[PlayerGameplayControl] 게임 조작 잠금");
    }

    // =========================================================
    // 카메라를 찾는 코드
    // =========================================================
    public void SetCamera(ThirdPersonCamera cameraController)
    {
        thirdPersonCamera = cameraController;
    }

    // =========================================================
    // Unlock
    // =========================================================

    public void UnlockControls()
    {
        if (deathLocked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (!controlsLocked)
            return;

        controlsLocked = false;
        GameplayLocked = false;

        // Player 조작 복구
        foreach (MonoBehaviour behaviour in gameplayBehaviours)
        {
            if (behaviour == null || behaviour == this)
                continue;

            behaviour.enabled = true;
        }

        // 잠갔던 무기 스크립트만 다시 활성화
        for (int i = 0; i < disabledWeapons.Count; i++)
        {
            if (disabledWeapons[i] != null)
            {
                disabledWeapons[i].enabled = true;
            }
        }
        disabledWeapons.Clear();

        // Camera 복구
        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.enabled = true;
        }

        // Mouse 게임 상태 복구
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("[PlayerGameplayControl] 게임 조작 복구");
    }

    public void LockForDeath()
    {
        deathLocked = true;

        if (!controlsLocked)
        {
            LockControls();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[PlayerGameplayControl] Player Death Lock");
    }

    // =========================================================
    // Destroy
    // =========================================================

    private void OnDestroy()
    {
        if (controlsLocked)
        {
            GameplayLocked = false;
        }
    }
}