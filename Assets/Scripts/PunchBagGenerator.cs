using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class PunchingBagSpawnEntry
{
    public PunchingBag prefab;

    [Min(0f)] public float weight = 1f;
}

public class PunchBagGenerator : MonoBehaviour
{
    [Header("Bag Pool")]
    [SerializeField] private List<PunchingBagSpawnEntry> bagPool = new();

    [Header("Spawn Point")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Rigidbody ceilingAnchorRigidbody;
    [SerializeField] private PunchBagChain chain;

    [Header("Generator Settings")]
    [SerializeField, Min(0f)] private float respawnDelay = 2f;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool fadeInOnSpawn = true;

    [Header("Point managing")]
    public int TotalPoints = 0;
    [SerializeField] private PointCounter pointCounter;

    private PunchingBag currentBag;
    private Coroutine respawnRoutine;

    private void Start()
    {
        if (spawnPoint == null)
            spawnPoint = transform;

        if (chain != null)
            chain.Build(ceilingAnchorRigidbody);

        if (spawnOnStart)
            SpawnPunchingBag();
    }

    private void OnDestroy()
    {
        if (currentBag != null)
            currentBag.OnBagDestroyed -= HandleBagDestroyed;
    }

    public void SpawnPunchingBag()
    {
        if (currentBag != null && !currentBag.IsDead)
            return;

        PunchingBag prefab = PickPrefab();
        if (prefab == null)
        {
            Debug.LogError("[PunchBagGenerator] The bag pool has no valid prefab with a weight above 0", this);
            return;
        }

        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
            respawnRoutine = null;
        }

        currentBag = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        if (chain != null && chain.IsBuilt)
        {
            currentBag.SnapHookTo(chain.EndPosition);
            currentBag.AttachToAnchor(chain.End);
        }
        else
        {
            currentBag.AttachToAnchor(ceilingAnchorRigidbody);
        }
        currentBag.OnBagDestroyed += HandleBagDestroyed;

        if (fadeInOnSpawn)
            currentBag.StartFadeIn();
    }

    private PunchingBag PickPrefab()
    {
        float totalWeight = GetTotalWeight();
        if (totalWeight <= 0f)
            return null;

        float roll = Random.value * totalWeight;
        PunchingBag lastValid = null;

        foreach (PunchingBagSpawnEntry entry in bagPool)
        {
            if (!IsValid(entry))
                continue;

            lastValid = entry.prefab;
            roll -= entry.weight;

            if (roll <= 0f)
                return entry.prefab;
        }

        return lastValid;
    }

    private float GetTotalWeight()
    {
        float total = 0f;

        foreach (PunchingBagSpawnEntry entry in bagPool)
        {
            if (IsValid(entry))
                total += entry.weight;
        }

        return total;
    }

    private static bool IsValid(PunchingBagSpawnEntry entry)
    {
        return entry != null && entry.prefab != null && entry.weight > 0f;
    }

    [ContextMenu("Log Spawn Chances")]
    private void LogSpawnChances()
    {
        float total = GetTotalWeight();
        if (total <= 0f)
        {
            Debug.LogWarning("[PunchBagGenerator] No valid entries in the bag pool.", this);
            return;
        }

        foreach (PunchingBagSpawnEntry entry in bagPool)
        {
            if (IsValid(entry))
                Debug.Log($"[PunchBagGenerator] {entry.prefab.name}: {entry.weight / total:P0}", this);
        }
    }

    private void HandleBagDestroyed(PunchingBag bag)
    {
        if (bag != null)
        {
            pointCounter.AddPoints(bag.PointsForDefeating);
            TotalPoints += bag.PointsForDefeating;
            bag.OnBagDestroyed -= HandleBagDestroyed;
        }

        if (respawnRoutine == null)
            respawnRoutine = StartCoroutine(RespawnRoutine(bag));
    }

    private IEnumerator RespawnRoutine(PunchingBag oldBag)
    {
        while (oldBag != null)
            yield return null;

        yield return new WaitForSeconds(respawnDelay);

        respawnRoutine = null;
        SpawnPunchingBag();
    }
}
