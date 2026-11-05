using UnityEngine;

public class GameTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    public float timeRemaining = 60f;

    [HideInInspector]
    public bool isRunning = false;

    public System.Action<float> OnTimeChanged;
    public System.Action OnTimerEnded;

    private float initialTime;

    void Awake()
    {
        initialTime = timeRemaining;
    }

    void Update()
    {
        if (!isRunning) return;

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;

            if (timeRemaining <= 0)
            {
                timeRemaining = 0;
                isRunning = false;

                OnTimerEnded?.Invoke();
            }
            else
            {
                OnTimeChanged?.Invoke(timeRemaining);
            }
        }
    }

    public void StartTimer()
    {
        isRunning = true;
    }

    public void PauseTimer()
    {
        isRunning = false;
    }

    public void ResetTimer(float newTime = -1f)
    {
        isRunning = false;

        if (newTime >= 0)
        {
            initialTime = newTime;
            timeRemaining = newTime;
        }
        else
        {
            timeRemaining = initialTime;
        }

        OnTimeChanged?.Invoke(timeRemaining);
    }
}