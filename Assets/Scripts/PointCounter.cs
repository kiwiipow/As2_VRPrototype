using UnityEngine;
using System.Collections;

public class PointCounter : MonoBehaviour
{
    [Header("Score Settings")]
    [SerializeField] private int currentMainScore = 0;
    [SerializeField] private int pendingPoints = 0;

    [Header("Timing & Transfer Settings")]
    [SerializeField] private float comboTimeMargin = 1f;
    [SerializeField] private float transferSpeed = 200f;

    private float inactiveTimer;
    private bool isTransferring = false;

    void Start()
    {
        inactiveTimer = comboTimeMargin;
    }

    void Update()
    {
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

                if (pendingPoints <= 0)
                {
                    pendingPoints = 0;
                    isTransferring = false;
                }
            }
        }
    }

    public void AddPoints(int amount)
    {
        pendingPoints += amount;

        inactiveTimer = comboTimeMargin;

        if (isTransferring)
        {
            isTransferring = false;
        }
    }
}

