using UnityEngine;
using System.Collections;
using TMPro;

public class PointCounter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameTimer gameTimer;

    [Header("Audio")]
    [SerializeField] private AudioSource points;

    [Header("Score Settings")]
    [SerializeField] private int currentMainScore = 0;
    [SerializeField] private int pendingPoints = 0;

    [Header("Timing & Transfer Settings")]
    [SerializeField] private float comboTimeMargin = 1f;
    [SerializeField] private float transferSpeed = 200f;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI mainScoreText;
    [SerializeField] private TextMeshProUGUI pendingPointsText;

    [Header("Popup Animation Settings")]
    [SerializeField] private float popupScaleMultiplier = 1.3f;
    [SerializeField] private float popupDuration = 0.15f;

    private float inactiveTimer;
    private bool isTransferring = false;
    private Vector3 pendingOriginalScale = Vector3.one;
    private Coroutine popupCoroutine;

    void Start()
    {
        inactiveTimer = comboTimeMargin;

        if (pendingPointsText != null)
        {
            pendingOriginalScale = pendingPointsText.transform.localScale;
        }

        if (gameTimer != null)
        {
            gameTimer.OnTimerEnded += HandleTimerEnded;
        }

        UpdateUI();
    }

    void Update()
    {
        bool valuesChanged = false;

        if (pendingPoints > 0)
        {
            if (!isTransferring)
            {
                inactiveTimer -= Time.deltaTime;

                if (inactiveTimer <= 0f)
                {
                    isTransferring = true;
                }
            }
            else
            {
                int pointsThisFrame = Mathf.CeilToInt(transferSpeed * Time.deltaTime);
                int transferAmount = Mathf.Min(pointsThisFrame, pendingPoints);

                currentMainScore += transferAmount;
                pendingPoints -= transferAmount;
                valuesChanged = true;

                if (pendingPoints <= 0)
                {
                    pendingPoints = 0;
                    isTransferring = false;
                }
            }
        }

        if (valuesChanged)
        {
            UpdateUI();
        }
    }

    public void AddPoints(int amount)
    {
        pendingPoints += amount;
        inactiveTimer = comboTimeMargin;

        if (points != null)
        {
            points.Play();
        }

        if (isTransferring)
        {
            isTransferring = false;
        }

        UpdateUI();

        if (pendingPointsText != null)
        {
            if (popupCoroutine != null)
            {
                StopCoroutine(popupCoroutine);
            }
            popupCoroutine = StartCoroutine(PopupAnimation(pendingPointsText.transform));
        }
    }

    private void HandleTimerEnded()
    {
        StartCoroutine(ResetPointsAfterDelay(3f));
    }

    private IEnumerator ResetPointsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        currentMainScore = 0;
        pendingPoints = 0;
        isTransferring = false;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (mainScoreText != null)
        {
            mainScoreText.text = currentMainScore.ToString();
        }

        if (pendingPointsText != null)
        {
            if (pendingPoints > 0)
            {
                pendingPointsText.gameObject.SetActive(true);
                pendingPointsText.text = "+ " + pendingPoints;
            }
            else
            {
                pendingPointsText.gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator PopupAnimation(Transform targetTransform)
    {
        float elapsedTime = 0f;
        Vector3 targetScale = pendingOriginalScale * popupScaleMultiplier;

        while (elapsedTime < popupDuration / 2f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (popupDuration / 2f);
            targetTransform.localScale = Vector3.Lerp(pendingOriginalScale, targetScale, t);
            yield return null;
        }

        elapsedTime = 0f;
        while (elapsedTime < popupDuration / 2f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (popupDuration / 2f);
            targetTransform.localScale = Vector3.Lerp(targetScale, pendingOriginalScale, t);
            yield return null;
        }

        targetTransform.localScale = pendingOriginalScale;
    }

    void OnDestroy()
    {
        if (gameTimer != null)
        {
            gameTimer.OnTimerEnded -= HandleTimerEnded;
        }
    }
}