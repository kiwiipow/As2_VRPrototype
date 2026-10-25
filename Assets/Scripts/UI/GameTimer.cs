using UnityEngine;

public class GameTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [Tooltip("Starting time in seconds.")]
    public float timeRemaining = 60f;

    [HideInInspector]
    public bool isRunning = false;

    public System.Action<float> OnTimeChanged;
    public System.Action OnTimerEnded;

    void Update()
    {
        if (isRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                OnTimeChanged?.Invoke(timeRemaining);
            }
            else
            {
                timeRemaining = 0;
                isRunning = false;

                OnTimerEnded?.Invoke();
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
        if (newTime >= 0)
        {
            timeRemaining = newTime;
        }
        isRunning = false;
    }
}
