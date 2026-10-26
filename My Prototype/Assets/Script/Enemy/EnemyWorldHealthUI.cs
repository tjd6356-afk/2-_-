using UnityEngine;
using UnityEngine.UI;

public class EnemyWorldHealthUI : MonoBehaviour
{
    // =========================================================
    // References
    // =========================================================

    [Header("References")]

    [Tooltip("체력을 읽을 EnemyStats")]
    [SerializeField]
    private EnemyStats enemyStats;


    [Tooltip("전체 HP UI Root")]
    [SerializeField]
    private GameObject healthUIRoot;


    [Tooltip("Filled 타입의 HP Image")]
    [SerializeField]
    private Image healthFillImage;


    [Tooltip("플레이어 카메라. 비어있으면 자동으로 찾음")]
    [SerializeField]
    private Camera targetCamera;


    // =========================================================
    // Visibility
    // =========================================================

    [Header("Visibility")]

    [Tooltip("최대 체력일 때 HP UI를 숨길지")]
    [SerializeField]
    private bool hideWhenFullHealth = true;


    [Tooltip("이 거리보다 멀어지면 HP UI를 완전히 숨김")]
    [SerializeField]
    private float maxVisibleDistance = 40f;


    // =========================================================
    // Distance Scale
    // =========================================================

    [Header("Distance Scale")]

    [Tooltip("이 거리에서는 최대 크기")]
    [SerializeField]
    private float nearDistance = 2f;


    [Tooltip("이 거리에서는 최소 크기")]
    [SerializeField]
    private float farDistance = 25f;


    [Tooltip("가까이 있을 때 UI Scale")]
    [SerializeField]
    private float nearScale = 1f;


    [Tooltip("멀리 있을 때 UI Scale")]
    [SerializeField]
    private float farScale = 0.4f;


    // =========================================================
    // Billboard
    // =========================================================

    [Header("Billboard")]

    [Tooltip("HP UI가 항상 카메라를 바라보게 함")]
    [SerializeField]
    private bool faceCamera = true;


    // =========================================================
    // Runtime
    // =========================================================

    private Vector3 originalScale;

    private bool healthAllowsVisible;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        // EnemyStats 자동 탐색
        if (enemyStats == null)
        {
            enemyStats =
                GetComponentInParent<EnemyStats>();
        }


        if (healthUIRoot == null)
        {
            healthUIRoot =
                gameObject;
        }


        originalScale =
            transform.localScale;
    }


    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        FindCamera();


        if (enemyStats == null)
        {
            Debug.LogError(
                $"[EnemyHealthUI] EnemyStats를 찾을 수 없습니다 : {name}"
            );

            return;
        }


        // 처음 HP 상태를 즉시 반영
        UpdateHealthUI(
            enemyStats.CurrentHealth,
            enemyStats.MaxHealth
        );
    }


    // =========================================================
    // Event 등록
    // =========================================================

    private void OnEnable()
    {
        if (enemyStats == null)
        {
            enemyStats =
                GetComponentInParent<EnemyStats>();
        }


        if (enemyStats != null)
        {
            enemyStats.OnHealthChanged +=
                UpdateHealthUI;


            enemyStats.OnDied +=
                HandleDeath;
        }
    }


    private void OnDisable()
    {
        if (enemyStats != null)
        {
            enemyStats.OnHealthChanged -=
                UpdateHealthUI;


            enemyStats.OnDied -=
                HandleDeath;
        }
    }


    // =========================================================
    // Camera / Distance
    // =========================================================

    private void LateUpdate()
    {
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
        // Camera 거리
        // ==========================================

        float distance =
            Vector3.Distance(
                transform.position,
                targetCamera.transform.position
            );


        // 최대 표시 거리 밖
        if (distance >
            maxVisibleDistance)
        {
            SetVisible(
                false
            );

            return;
        }


        // 체력 조건에 의해 보이면 표시
        SetVisible(
            healthAllowsVisible
        );


        if (!healthAllowsVisible)
            return;


        // ==========================================
        // 거리 → Scale
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


        float normalizedHealth =
            Mathf.Clamp01(
                currentHealth /
                maxHealth
            );


        if (healthFillImage != null)
        {
            healthFillImage.fillAmount =
                normalizedHealth;
        }


        // ==========================================
        // 표시 조건
        // ==========================================

        if (currentHealth <= 0f)
        {
            healthAllowsVisible =
                false;
        }

        else if (hideWhenFullHealth &&
                 currentHealth >= maxHealth)
        {
            healthAllowsVisible =
                false;
        }

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
    // Visibility
    // =========================================================

    private void SetVisible(
        bool visible
    )
    {
        if (healthUIRoot == null)
            return;


        // healthUIRoot가 자기 자신이면
        // 스크립트까지 꺼지는 문제가 있으므로
        // CanvasRenderer만 관리하는 방식 대신
        // 자식 Root를 사용하는 것을 권장
        if (healthUIRoot == gameObject)
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
    // Camera Find
    // =========================================================

    private void FindCamera()
    {
        targetCamera =
            Camera.main;
    }
}