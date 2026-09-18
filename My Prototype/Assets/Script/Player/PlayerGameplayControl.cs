using UnityEngine;

public class PlayerGameplayControl : MonoBehaviour
{
    [Header("게임 조작 중지 대상")]
    [SerializeField]
    private MonoBehaviour[] gameplayBehaviours;


    private bool isLocked;

    public bool IsLocked => isLocked;


    // =========================================================
    // 조작 잠금
    // =========================================================

    public void LockControls()
    {
        if (isLocked)
            return;


        isLocked = true;


        foreach (MonoBehaviour behaviour in gameplayBehaviours)
        {
            if (behaviour == null)
                continue;


            behaviour.enabled = false;
        }


        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;
    }


    // =========================================================
    // 조작 복구
    // =========================================================

    public void UnlockControls()
    {
        if (!isLocked)
            return;


        isLocked = false;


        foreach (MonoBehaviour behaviour in gameplayBehaviours)
        {
            if (behaviour == null)
                continue;


            behaviour.enabled = true;
        }


        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;
    }


    private void OnDisable()
    {
        // Player 자체가 파괴되는 상황을 대비
        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;
    }
}