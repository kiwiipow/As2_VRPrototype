using UnityEngine;
using UnityEngine.UI;

public class TimerVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameTimer gameTimer;
    [SerializeField] private Image radialImage;

    [Header("Audio")]
    [SerializeField] private AudioSource beep;
    [SerializeField] private AudioSource fight;
    [SerializeField] private AudioSource bell;

    [Header("Color Settings")]
    [SerializeField] private Color safeColor = Color.green;
    [SerializeField] private Color warningYellow = Color.yellow;
    [SerializeField] private Color warningOrange = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color dangerRed = Color.red;
    [SerializeField] private float colorTransitionStart = 0.5f;

    [Header("Pulse Settings")]
    [SerializeField] private bool syncPulseWithColorStart = true;
    [SerializeField] private float pulseStartThreshold = 0.5f;
    [SerializeField] private float maxPulseScale = 1.25f;
    [SerializeField] private float minPulseInterval = 0.1f;
    [SerializeField] private float maxPulseInterval = 0.8f;

    private float initialDuration;
    private Vector3 originalScale;
    private float pulseTimer;
    private bool wasPulsingLastFrame;
    private bool timerHasStarted = false;

    void Start()
    {
        if (gameTimer != null)
        {
            initialDuration = gameTimer.timeRemaining;
            gameTimer.OnTimeChanged += UpdateVisual;
            gameTimer.OnTimerEnded += HandleTimerEnded;
        }

        if (radialImage != null)
        {
            radialImage.fillAmount = 1f;
            radialImage.color = safeColor;
            originalScale = radialImage.transform.localScale;
        }
    }

    private void UpdateVisual(float currentTime)
    {
        if (radialImage == null || initialDuration <= 0) return;

        // Detects the exact moment the timer starts running
        if (!timerHasStarted && gameTimer.isRunning)
        {
            timerHasStarted = true;

            if (fight != null) fight.Play();
            if (bell != null) bell.Play();
        }

        float timeRatio = Mathf.Clamp01(currentTime / initialDuration);

        radialImage.fillAmount = timeRatio;

        float activePulseThreshold = syncPulseWithColorStart ? colorTransitionStart : pulseStartThreshold;

        // Color transition logic
        if (timeRatio > colorTransitionStart)
        {
            radialImage.color = safeColor;
        }
        else
        {
            float colorProgress = 1f - (timeRatio / colorTransitionStart);

            if (colorProgress < 0.5f)
            {
                float t = colorProgress * 2f;
                radialImage.color = Color.Lerp(warningYellow, warningOrange, t);
            }
            else
            {
                float t = (colorProgress - 0.5f) * 2f;
                radialImage.color = Color.Lerp(warningOrange, dangerRed, t);
            }
        }

        bool isPulsingNow = (timeRatio <= activePulseThreshold && timeRatio > 0f);

        if (isPulsingNow)
        {
            if (!wasPulsingLastFrame)
            {
                pulseTimer = maxPulseInterval;
            }

            float linearDanger = 1f - (timeRatio / activePulseThreshold);

            // Logarithmic curve mapping for smooth organic acceleration
            float dangerFactor = Mathf.Log(1f + 9f * linearDanger) / Mathf.Log(10f);

            float currentInterval = Mathf.Lerp(maxPulseInterval, minPulseInterval, dangerFactor);

            pulseTimer += Time.deltaTime;
            if (pulseTimer >= currentInterval)
            {
                pulseTimer = 0f;

                float currentPopDuration = Mathf.Lerp(0.3f, 0.08f, dangerFactor);
                StartCoroutine(PulseAnimation(radialImage.transform, currentPopDuration));

                if (beep != null)
                {
                    beep.Play();
                }
            }
        }
        else
        {
            radialImage.transform.localScale = originalScale;
            pulseTimer = 0f;
        }

        wasPulsingLastFrame = isPulsingNow;
    }

    private void HandleTimerEnded()
    {
        if (radialImage != null)
        {
            radialImage.fillAmount = 1f;
            radialImage.color = safeColor;
            radialImage.transform.localScale = originalScale;
        }

        wasPulsingLastFrame = false;
        pulseTimer = 0f;
        timerHasStarted = false; // Reset so sounds can trigger again if the timer is restarted

        // Play bell when the timer ends
        if (bell != null)
        {
            bell.Play();
        }
    }

    private System.Collections.IEnumerator PulseAnimation(Transform target, float duration)
    {
        float elapsed = 0f;
        Vector3 targetScale = originalScale * maxPulseScale;

        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration / 2f);
            target.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration / 2f);
            target.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        target.localScale = originalScale;
    }

    void OnDestroy()
    {
        if (gameTimer != null)
        {
            gameTimer.OnTimeChanged -= UpdateVisual;
            gameTimer.OnTimerEnded -= HandleTimerEnded;
        }
    }
}