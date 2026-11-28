using UnityEngine;

public class CombatHitStop : MonoBehaviour
{
    public static CombatHitStop Instance { get; private set; }

    [Header("Melee")]
    [SerializeField, Min(0f)] private float meleeDuration = 0.08f;
    [SerializeField, Range(0.01f, 1f)] private float meleeTimeScale = 0.05f;
    [SerializeField, Min(0f)] private float meleeCooldown = 0.12f;

    [Header("Other Weapons")]
    [SerializeField] private bool enableWeakHitStop = true;
    [SerializeField, Min(0f)] private float weakDuration = 0.025f;
    [SerializeField, Range(0.01f, 1f)] private float weakTimeScale = 0.75f;
    [SerializeField, Min(0f)] private float weakCooldown = 0.18f;

    private bool active;
    private bool strong;
    private float originalScale;
    private float appliedScale;
    private float endTime;
    private float nextMeleeTime;
    private float nextWeakTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }
        Instance = this;
    }

    public static void Request(bool melee)
    {
        if (Instance != null && Instance.isActiveAndEnabled)
            Instance.Play(melee);
    }

    private void Play(bool melee)
    {
        if (!melee && !enableWeakHitStop) return;

        float now = Time.unscaledTime;
        if (active && now >= endTime) Restore();

        // An external pause or time-scale controller has priority.
        if (Time.timeScale <= 0f) return;
        if (active && !Mathf.Approximately(Time.timeScale, appliedScale))
        {
            active = false;
            return;
        }

        if (melee ? now < nextMeleeTime : now < nextWeakTime) return;
        // Multiple targets or rapid rounds must not keep extending the stop.
        // A strong hit may replace a currently active weak hit, never reverse.
        if (active && (strong || !melee)) return;

        float duration = melee ? meleeDuration : weakDuration;
        if (duration <= 0f) return;

        if (!active) originalScale = Time.timeScale;
        strong = melee;
        active = true;
        float factor = melee ? meleeTimeScale : weakTimeScale;
        appliedScale = originalScale * factor;
        Time.timeScale = appliedScale;
        endTime = now + duration;

        if (melee)
            nextMeleeTime = now + Mathf.Max(duration, meleeCooldown);
        else
            nextWeakTime = now + Mathf.Max(duration, weakCooldown);
    }

    private void Update()
    {
        if (!active) return;
        if (!Mathf.Approximately(Time.timeScale, appliedScale))
        {
            active = false;
            return;
        }
        if (Time.unscaledTime >= endTime) Restore();
    }

    private void Restore()
    {
        if (!active) return;
        if (Mathf.Approximately(Time.timeScale, appliedScale))
            Time.timeScale = originalScale;
        active = false;
        strong = false;
    }

    private void OnDisable() => Restore();

    private void OnDestroy()
    {
        Restore();
        if (Instance == this) Instance = null;
    }
}
