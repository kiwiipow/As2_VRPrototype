using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class GameOverScreen : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameTimer gameTimer;
    [SerializeField] private PunchBagGenerator generator;

    [Header("Records")]
    [SerializeField] private bool ignoreZeroScores = true;

    [Header("Desaturation")]
    [SerializeField] private Volume desaturationVolume;
    [SerializeField, Min(0f)] private float fadeDuration = 0.8f;

    [Header("Floating Canvas")]
    [SerializeField] private GameObject canvasRoot;
    [SerializeField] private float distanceFromPlayer = 1.5f;
    [SerializeField] private float heightOffset = -0.1f;
    [SerializeField, Min(0f)] private float showDelay = 0.6f;
    [SerializeField, Min(0f)] private float canvasFadeDuration = 0.4f;

    [Header("Follow Player")]
    [SerializeField] private bool followPlayer = true;
    [SerializeField, Min(0f)] private float positionFollowSpeed = 6f;
    [SerializeField, Min(0f)] private float rotationFollowSpeed = 3f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private Button closeButton;

    [Header("UI Interaction (punch / poke)")]
    [SerializeField] private GameObject[] uiInteractors;
    [SerializeField, Min(0f)] private float pressDelay = 0.8f;

    private CanvasGroup canvasGroup;
    private Camera cam;
    private bool isShowing;
    private float smoothedYaw;
    private Coroutine volumeRoutine;
    private Coroutine canvasRoutine;
    private Coroutine showRoutine;

    private void Awake()
    {
        if (canvasRoot != null)
        {
            canvasGroup = canvasRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = canvasRoot.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        cam = Camera.main;

        HideInstantly();

        if (desaturationVolume != null)
            desaturationVolume.weight = 0f;

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (gameTimer != null)
            gameTimer.OnTimerEnded += HandleTimerEnded;
    }

    private void OnDestroy()
    {
        if (gameTimer != null)
            gameTimer.OnTimerEnded -= HandleTimerEnded;

        PunchingBag.HitsBlocked = false;
    }

    private void HandleTimerEnded()
    {
        if (isShowing || showRoutine != null)
            return;

        showRoutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        PunchingBag.HitsBlocked = true;

        int finalScore = generator != null ? generator.TotalPoints : 0;

        int rank = 0;
        if (!ignoreZeroScores || finalScore > 0)
            rank = RecordsManager.AddRecord(finalScore);

        FadeVolumeTo(1f);
        yield return new WaitForSecondsRealtime(showDelay);

        if (scoreText != null)
            scoreText.text = finalScore.ToString();

        if (rankText != null)
        {
            rankText.gameObject.SetActive(rank > 0);
            if (rank == 1)
                rankText.text = "NEW RECORD!";
            else if (rank > 1)
                rankText.text = "#" + rank;
        }

        SetRaysActive(true);

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasRoot.SetActive(true);
        SnapToPlayer();

        isShowing = true;
        showRoutine = null;

        FadeCanvasTo(1f, false);
        StartCoroutine(EnablePressRoutine());
    }

    private IEnumerator EnablePressRoutine()
    {
        yield return new WaitForSecondsRealtime(pressDelay);

        if (isShowing)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    private void LateUpdate()
    {
        if (!followPlayer || canvasRoot == null || !canvasRoot.activeSelf)
            return;

        FollowPlayer(false);
    }

    private void SnapToPlayer()
    {
        FollowPlayer(true);
    }

    private void FollowPlayer(bool snap)
    {
        if (cam == null)
            cam = Camera.main;
        if (cam == null)
            return;

        Transform head = cam.transform;

        Vector3 flatForward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (flatForward.sqrMagnitude > 0.001f)
        {
            float targetYaw = Mathf.Atan2(flatForward.x, flatForward.z) * Mathf.Rad2Deg;
            if (snap)
                smoothedYaw = targetYaw;
            else
                smoothedYaw = Mathf.LerpAngle(smoothedYaw, targetYaw, 1f - Mathf.Exp(-rotationFollowSpeed * Time.unscaledDeltaTime));
        }

        Quaternion yawRotation = Quaternion.Euler(0f, smoothedYaw, 0f);
        Vector3 targetPosition = head.position + yawRotation * new Vector3(0f, heightOffset, distanceFromPlayer);

        Transform t = canvasRoot.transform;
        if (snap)
            t.position = targetPosition;
        else
            t.position = Vector3.Lerp(t.position, targetPosition, 1f - Mathf.Exp(-positionFollowSpeed * Time.unscaledDeltaTime));

        t.rotation = yawRotation;
    }

    public void Close()
    {
        if (!isShowing)
            return;

        isShowing = false;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        FadeCanvasTo(0f, true);
        FadeVolumeTo(0f);
    }

    private void HideInstantly()
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasRoot.SetActive(false);
        SetRaysActive(false);
    }

    private void SetRaysActive(bool active)
    {
        if (uiInteractors == null)
            return;

        foreach (GameObject interactor in uiInteractors)
        {
            if (interactor != null)
                interactor.SetActive(active);
        }
    }

    private void FadeCanvasTo(float target, bool finishClosing)
    {
        if (canvasRoutine != null)
            StopCoroutine(canvasRoutine);

        canvasRoutine = StartCoroutine(CanvasFadeRoutine(target, finishClosing));
    }

    private IEnumerator CanvasFadeRoutine(float target, bool finishClosing)
    {
        float start = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < canvasFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / canvasFadeDuration);
            yield return null;
        }

        canvasGroup.alpha = target;

        if (finishClosing)
        {
            canvasRoot.SetActive(false);
            SetRaysActive(false);
            PunchingBag.HitsBlocked = false;
        }

        canvasRoutine = null;
    }

    private void FadeVolumeTo(float targetWeight)
    {
        if (desaturationVolume == null)
            return;

        if (volumeRoutine != null)
            StopCoroutine(volumeRoutine);

        volumeRoutine = StartCoroutine(VolumeFadeRoutine(targetWeight));
    }

    private IEnumerator VolumeFadeRoutine(float target)
    {
        float start = desaturationVolume.weight;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            desaturationVolume.weight = Mathf.Lerp(start, target, elapsed / fadeDuration);
            yield return null;
        }

        desaturationVolume.weight = target;
        volumeRoutine = null;
    }
}
