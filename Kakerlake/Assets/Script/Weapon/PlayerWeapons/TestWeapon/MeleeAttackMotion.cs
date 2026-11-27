using UnityEngine;

// Player에 장착. PlayerMovement가 수평 이동을 계산할 때 한 번씩 호출한다.
// 이 컴포넌트 자체는 Rigidbody/Transform 위치를 직접 변경하지 않는다.
public class MeleeAttackMotion : MonoBehaviour
{
    [Tooltip("선택: 플레이어 외형 전용 회전 Pivot. 카메라/무기/Collider가 포함된 루트는 연결하지 마세요.")]
    [SerializeField] private Transform playerVisual;

    private TestWeapon owner;
    private Vector3 direction;
    private float remainingDistance;
    private float moveSpeed;
    private bool spinning;
    private Quaternion originalVisualRotation;

    public void Hold(TestWeapon weapon)
    {
        if (weapon == null) return;
        if (owner != null && owner != weapon) RestoreVisual();
        owner = weapon;
        remainingDistance = 0f;
        moveSpeed = 0f;
        direction = Vector3.zero;
    }

    public void BeginAdvance(TestWeapon weapon, Vector3 forward, float distance, float duration)
    {
        if (weapon == null || owner != weapon) return;
        forward.y = 0f;
        direction = forward.normalized;
        remainingDistance = Mathf.Max(0f, distance);
        moveSpeed = remainingDistance / Mathf.Max(0.01f, duration);
    }

    // 일반 이동과 더하는 것이 아니라 수평 속도를 대체한다.
    // FixedUpdate라면 Time.fixedDeltaTime, Update라면 Time.deltaTime 사용.
    public bool TryGetAttackVelocity(float deltaTime, out Vector3 velocity)
    {
        velocity = Vector3.zero;
        if (owner == null || !owner.isActiveAndEnabled || PlayerGameplayControl.GameplayLocked)
        {
            Clear();
            return false;
        }
        if (deltaTime <= 0f) return true;
        float step = Mathf.Min(remainingDistance, moveSpeed * deltaTime);
        remainingDistance -= step;
        velocity = direction * (step / deltaTime);
        return true;
    }

    public void BeginVisualSpin(TestWeapon weapon)
    {
        if (owner != weapon || playerVisual == null || playerVisual == transform || spinning) return;
        // 무기를 포함하는 부모를 회전하면 검이 두 번 회전하므로 제외한다.
        if (weapon.transform.IsChildOf(playerVisual))
        {
            Debug.LogWarning("[MeleeAttackMotion] Player Visual에는 무기 오브젝트의 부모를 연결하지 마세요.", this);
            return;
        }
        originalVisualRotation = playerVisual.localRotation;
        spinning = true;
    }

    public void SetVisualSpin(TestWeapon weapon, float angle)
    {
        if (owner != weapon || !spinning || playerVisual == null) return;
        playerVisual.localRotation = Quaternion.AngleAxis(angle, Vector3.up) * originalVisualRotation;
    }

    public void Release(TestWeapon weapon)
    {
        if (owner != weapon) return;
        Clear();
    }

    private void RestoreVisual()
    {
        if (spinning && playerVisual != null) playerVisual.localRotation = originalVisualRotation;
        spinning = false;
    }

    private void Clear()
    {
        RestoreVisual();
        owner = null;
        remainingDistance = 0f;
        moveSpeed = 0f;
        direction = Vector3.zero;
    }

    private void OnDisable() => Clear();
}
