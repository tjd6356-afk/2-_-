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

    [Tooltip(
        "UI가 열렸을 때 비활성화할 플레이어 조작 스크립트들"
    )]
    [SerializeField]
    private List<MonoBehaviour> gameplayBehaviours =
        new List<MonoBehaviour>();


    // =========================================================
    // Camera
    // =========================================================

    [Header("Camera")]

    [Tooltip(
        "UI가 열렸을 때 회전 입력을 막을 ThirdPersonCamera"
    )]
    [SerializeField]
    private ThirdPersonCamera thirdPersonCamera;


    // =========================================================
    // Runtime
    // =========================================================

    private bool controlsLocked;


    public bool ControlsLocked =>
        controlsLocked;


    private bool deathLocked;


    public bool DeathLocked =>
        deathLocked;

    // =========================================================
    // Lock
    // =========================================================

    public void LockControls()
    {
        if (controlsLocked)
            return;


        controlsLocked =
            true;


        GameplayLocked =
            true;


        // ==========================================
        // Player 조작 Script 비활성화
        // ==========================================

        foreach (
            MonoBehaviour behaviour
            in gameplayBehaviours)
        {
            if (behaviour == null)
                continue;


            // 자기 자신은 끄면 안 됨
            if (behaviour == this)
                continue;


            behaviour.enabled =
                false;
        }


        // ==========================================
        // Camera 입력 정지
        // ==========================================

        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.enabled =
                false;
        }


        // ==========================================
        // Mouse
        // ==========================================

        Cursor.lockState =
            CursorLockMode.None;


        Cursor.visible =
            true;


        Debug.Log(
            "[PlayerGameplayControl] 게임 조작 잠금"
        );
    }

    // =========================================================
    // 카메라를 찾는 코드
    // =========================================================
    public void SetCamera(
    ThirdPersonCamera cameraController
)
    {
        thirdPersonCamera =
            cameraController;
    }


    // =========================================================
    // Unlock
    // =========================================================

    public void UnlockControls()
    {
        if (deathLocked)
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible =
                true;

            return;
        }

        if (!controlsLocked)
            return;


        controlsLocked =
            false;


        GameplayLocked =
            false;


        // ==========================================
        // Player 조작 복구
        // ==========================================

        foreach (
            MonoBehaviour behaviour
            in gameplayBehaviours)
        {
            if (behaviour == null)
                continue;


            if (behaviour == this)
                continue;


            behaviour.enabled =
                true;
        }


        // ==========================================
        // Camera 복구
        // ==========================================

        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.enabled =
                true;
        }


        // ==========================================
        // Mouse 게임 상태 복구
        // ==========================================

        Cursor.lockState =
            CursorLockMode.Locked;


        Cursor.visible =
            false;


        Debug.Log(
            "[PlayerGameplayControl] 게임 조작 복구"
        );
    }

    public void LockForDeath()
    {
        deathLocked =
            true;


        // 이미 Inventory 때문에 잠겨있지 않은 경우
        if (!controlsLocked)
        {
            LockControls();
        }


        // 이미 잠겨있더라도 Cursor 상태는 확실하게
        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
            true;


        Debug.Log(
            "[PlayerGameplayControl] Player Death Lock"
        );
    }

    // =========================================================
    // Destroy
    // =========================================================

    private void OnDestroy()
    {
        if (controlsLocked)
        {
            GameplayLocked =
                false;
        }
    }
}