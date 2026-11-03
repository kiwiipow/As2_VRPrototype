using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool fadeInOnSpawn = true;

    [Header("Point managing")]
    public int TotalPoints = 0;
    [SerializeField] private PointCounter pointCounter;

    [Header("Timer managing")]
    public bool firstHitEfectuated = false;

    [Header("External Impact Effects (Vignette & Bloom)")]
    [SerializeField] private Volume postProcessVolume;
    [SerializeField, Range(0f, 1f)] private float maxVignetteIntensity = 0.5f;
    [SerializeField, Range(0f, 20f)] private float maxBloomIntensity = 2.5f;
    [SerializeField, Range(0.05f, 0.5f)] private float flashDuration = 0.15f;

    private Coroutine effectRoutine;
    private Vignette vignetteComponent;
    private Bloom bloomComponent;

    private PunchingBag currentBag;
    private Coroutine respawnRoutine;

    private void Start()
    {
        if (spawnPoint == null)
            spawnPoint = transform;

        if (chain != null)
            chain.Build(ceilingAnchorRigidbody);

        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            if (postProcessVolume.profile.TryGet(out vignetteComponent))
            {
                vignetteComponent.intensity.overrideState = true;
            }

            if (postProcessVolume.profile.TryGet(out bloomComponent))
            {
                bloomComponent.intensity.overrideState = true;
            }
        }

        if (spawnOnStart)
            SpawnPunchingBag();
    }

    private void OnDestroy()
    {
        if (currentBag != null)
        {
            currentBag.OnBagDestroyed -= HandleBagDestroyed;
            currentBag.OnHit -= HandleBagHit;
        }
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
            chain.SetLoaded(true);
        }
        else
        {
            currentBag.AttachToAnchor(ceilingAnchorRigidbody);
        }
        currentBag.OnBagDestroyed += HandleBagDestroyed;
        currentBag.OnHit += HandleBagHit;

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

    private void HandleBagHit(PunchingBag.HitInfo hitInfo)
    {
        if (!firstHitEfectuated)
        {
            firstHitEfectuated = true;

            // Start the timer here guillem
        }

        if (effectRoutine != null)
            StopCoroutine(effectRoutine);

        effectRoutine = StartCoroutine(TriggerImpactEffectsRoutine());
    }

    private IEnumerator TriggerImpactEffectsRoutine()
    {
        float initialVignette = 0f;
        float initialBloom = 0f;

        if (vignetteComponent != null)
        {
            initialVignette = vignetteComponent.intensity.value;
            vignetteComponent.intensity.value = maxVignetteIntensity;
        }

        if (bloomComponent != null)
        {
            initialBloom = bloomComponent.intensity.value;
            bloomComponent.intensity.value = maxBloomIntensity;
        }

        yield return new WaitForSecondsRealtime(flashDuration);

        float elapsed = 0f;
        float fadeDuration = 0.2f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeDuration;

            if (vignetteComponent != null)
            {
                vignetteComponent.intensity.value = Mathf.Lerp(maxVignetteIntensity, initialVignette, t);
            }

            if (bloomComponent != null)
            {
                bloomComponent.intensity.value = Mathf.Lerp(maxBloomIntensity, initialBloom, t);
            }

            yield return null;
        }

        if (vignetteComponent != null)
        {
            vignetteComponent.intensity.value = initialVignette;
        }

        if (bloomComponent != null)
        {
            bloomComponent.intensity.value = initialBloom;
        }

        effectRoutine = null;
    }

    private void HandleBagDestroyed(PunchingBag bag)
    {
        if (chain != null)
            chain.SetLoaded(false);

        if (bag != null)
        {
            pointCounter.AddPoints(bag.PointsForDefeating);
            TotalPoints += bag.PointsForDefeating;
            bag.OnBagDestroyed -= HandleBagDestroyed;
            bag.OnHit -= HandleBagHit;
        }

        if (respawnRoutine == null)
            respawnRoutine = StartCoroutine(RespawnRoutine(bag));
    }

    private IEnumerator RespawnRoutine(PunchingBag oldBag)
    {
        while (oldBag != null)
            yield return null;

        respawnRoutine = null;
        SpawnPunchingBag();
    }
}