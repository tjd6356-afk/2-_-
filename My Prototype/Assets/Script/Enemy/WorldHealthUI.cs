using UnityEngine;
using UnityEngine.UI;

public class WorldHealthUI : MonoBehaviour
{
    // =========================================================
    // References
    // =========================================================

    [Header("References")]

    [Tooltip("IHealth를 가지고 있는 오브젝트. 비워두면 부모에서 자동 탐색")]
    [SerializeField]
    private MonoBehaviour healthSource;


    [Tooltip("실제로 켜고 끌 HP UI Root")]
    [SerializeField]
    private GameObject healthUIRoot;


    [Tooltip("Filled 타입 HP Image")]
    [SerializeField]
    private Image healthFillImage;


    [Tooltip("바라볼 Camera. 비워두면 Camera.main 자동 탐색")]
    [SerializeField]
    private Camera targetCamera;


    // =========================================================
    // Visibility
    // =========================================================

    [Header("Visibility")]

    [Tooltip("체력이 최대일 때 HP UI 숨김")]
    [SerializeField]
    private bool hideWhenFullHealth = true;


    [Tooltip("이 거리보다 멀어지면 UI 완전히 숨김")]
    [SerializeField]
    private float maxVisibleDistance = 40f;


    // =========================================================
    // Distance Scale
    // =========================================================

    [Header("Distance Scale")]

    [Tooltip("이 거리 이하에서는 최대 크기")]
    [SerializeField]
    private float nearDistance = 2f;


    [Tooltip("이 거리 이상에서는 최소 크기")]
    [SerializeField]
    private float farDistance = 25f;


    [SerializeField]
    private float nearScale = 1f;


    [SerializeField]
    private float farScale = 0.4f;


    // =========================================================
    // Billboard
    // =========================================================

    [Header("Billboard")]

    [SerializeField]
    private bool faceCamera = true;


    // =========================================================
    // Runtime
    // =========================================================

    private IHealth health;

    private Vector3 originalScale;

    private bool healthAllowsVisible;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        originalScale =
            transform.localScale;


        FindHealthSource();


        FindCamera();
    }


    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        if (health == null)
        {
            Debug.LogError(
                $"[WorldHealthUI] IHealth를 찾지 못했습니다 : {name}"
            );

            return;
        }


        UpdateHealthUI(
            health.CurrentHealth,
            health.MaxHealth
        );
    }


    // =========================================================
    // Enable
    // =========================================================

    private void OnEnable()
    {
        if (health == null)
        {
            FindHealthSource();
        }


        SubscribeHealth();
    }


    // =========================================================
    // Disable
    // =========================================================

    private void OnDisable()
    {
        UnsubscribeHealth();
    }


    // =========================================================
    // Health Source 자동 탐색
    // =========================================================

    private void FindHealthSource()
    {
        health =
            null;


        // ==========================================
        // Inspector에서 직접 지정한 경우
        // ==========================================

        if (healthSource != null)
        {
            health =
                healthSource as IHealth;


            if (health != null)
            {
                return;
            }


            Debug.LogWarning(
                $"[WorldHealthUI] 지정한 Health Source가 " +
                $"IHealth를 구현하지 않았습니다 : " +
                $"{healthSource.GetType().Name}"
            );
        }


        // ==========================================
        // 부모를 올라가며 IHealth 탐색
        // ==========================================

        Transform current =
            transform;


        while (current != null)
        {
            MonoBehaviour[] behaviours =
                current.GetComponents<MonoBehaviour>();


            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IHealth foundHealth)
                {
                    health =
                        foundHealth;


                    healthSource =
                        behaviour;


                    Debug.Log(
                        $"[WorldHealthUI] Health 연결 : " +
                        $"{behaviour.GetType().Name} / " +
                        $"{current.name}"
                    );


                    return;
                }
            }


            current =
                current.parent;
        }
    }


    // =========================================================
    // Event
    // =========================================================

    private void SubscribeHealth()
    {
        if (health == null)
            return;


        health.OnHealthChanged -=
            UpdateHealthUI;


        health.OnDied -=
            HandleDeath;


        health.OnHealthChanged +=
            UpdateHealthUI;


        health.OnDied +=
            HandleDeath;
    }


    private void UnsubscribeHealth()
    {
        if (health == null)
            return;


        health.OnHealthChanged -=
            UpdateHealthUI;


        health.OnDied -=
            HandleDeath;
    }


    // =========================================================
    // Late Update
    // =========================================================

    private void LateUpdate()
    {
        if (health == null)
            return;


        if (targetCamera == null)
        {
            FindCamera();


            if (targetCamera == null)
                return;
        }


        // ==========================================
        // Camera 바라보기
        // ==========================================

        if (faceCamera)
        {
            transform.rotation =
                targetCamera.transform.rotation;
        }


        // ==========================================
        // 거리
        // ==========================================

        float distance =
            Vector3.Distance(
                transform.position,
                targetCamera.transform.position
            );


        // 너무 멀면 숨김
        if (distance >
            maxVisibleDistance)
        {
            SetVisible(
                false
            );

            return;
        }


        // 체력에 따른 표시 상태
        SetVisible(
            healthAllowsVisible
        );


        if (!healthAllowsVisible)
            return;


        // ==========================================
        // 거리별 Scale
        // ==========================================

        float t =
            Mathf.InverseLerp(
                nearDistance,
                farDistance,
                distance
            );


        float scale =
            Mathf.Lerp(
                nearScale,
                farScale,
                t
            );


        transform.localScale =
            originalScale *
            scale;
    }


    // =========================================================
    // HP Update
    // =========================================================

    private void UpdateHealthUI(
        float currentHealth,
        float maxHealth
    )
    {
        if (maxHealth <= 0f)
            return;


        float normalized =
            Mathf.Clamp01(
                currentHealth /
                maxHealth
            );


        if (healthFillImage != null)
        {
            healthFillImage.fillAmount =
                normalized;
        }


        // ==========================================
        // 죽음
        // ==========================================

        if (currentHealth <= 0f)
        {
            healthAllowsVisible =
                false;
        }

        // ==========================================
        // Full HP
        // ==========================================

        else if (hideWhenFullHealth &&
                 currentHealth >= maxHealth)
        {
            healthAllowsVisible =
                false;
        }

        // ==========================================
        // 피해 받은 상태
        // ==========================================

        else
        {
            healthAllowsVisible =
                true;
        }


        SetVisible(
            healthAllowsVisible
        );
    }


    // =========================================================
    // Death
    // =========================================================

    private void HandleDeath()
    {
        healthAllowsVisible =
            false;


        SetVisible(
            false
        );
    }


    // =========================================================
    // Visible
    // =========================================================

    private void SetVisible(
        bool visible
    )
    {
        if (healthUIRoot == null)
            return;


        if (healthUIRoot.activeSelf !=
            visible)
        {
            healthUIRoot.SetActive(
                visible
            );
        }
    }


    // =========================================================
    // Camera
    // =========================================================

    private void FindCamera()
    {
        targetCamera =
            Camera.main;
    }
}