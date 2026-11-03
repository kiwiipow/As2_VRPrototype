using UnityEngine;

public class PunchingBagSpring : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameTimer gameTimer;
    [SerializeField] private MeshRenderer bagMeshRenderer;

    [Header("Tilt Settings")]
    [SerializeField] private float tiltThresholdDegrees = 15f;

    [Header("Color Settings")]
    [SerializeField] private Color runningColor = Color.green;
    [SerializeField] private Color stoppedColor = Color.white;

    private Quaternion initialRotation;
    private bool isTriggered = false;
    private Material bagMaterialInstance;

    void Start()
    {
        initialRotation = transform.rotation;

        if (bagMeshRenderer == null)
        {
            bagMeshRenderer = GetComponent<MeshRenderer>();
        }

        if (bagMeshRenderer != null)
        {
            bagMaterialInstance = bagMeshRenderer.material;
            UpdateBagColor();
        }
    }

    void Update()
    {
        float currentTilt = Quaternion.Angle(initialRotation, transform.rotation);

        if (currentTilt >= tiltThresholdDegrees)
        {
            if (!isTriggered)
            {
                isTriggered = true;
                TriggerTimerAction();
            }
        }
        else
        {
            if (currentTilt < tiltThresholdDegrees * 0.5f)
            {
                isTriggered = false;
            }
        }

        UpdateBagColor();
    }

    private void TriggerTimerAction()
    {
        if (gameTimer == null) return;

        if (gameTimer.isRunning)
        {
            gameTimer.PauseTimer();
            Debug.Log("Timer Paused!");
        }
        else
        {
            gameTimer.StartTimer();
            Debug.Log("Timer Started!");
        }
    }

    private void UpdateBagColor()
    {
        if (bagMaterialInstance == null || gameTimer == null) return;

        Color targetColor = gameTimer.isRunning ? runningColor : stoppedColor;

        bagMaterialInstance.SetColor("_BaseColor", targetColor);
    }
}