using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

[RequireComponent(typeof(Rigidbody))]
public class PunchingBag : MonoBehaviour
{
    public readonly struct HitInfo
    {
        public readonly int Damage;
        public readonly float Speed;
        public readonly float ZoneMultiplier;
        public readonly Vector3 Direction;
        public readonly Vector3 Point;

        public HitInfo(int damage, float speed, float zoneMultiplier, Vector3 direction, Vector3 point)
        {
            Damage = damage;
            Speed = speed;
            ZoneMultiplier = zoneMultiplier;
            Direction = direction;
            Point = point;
        }
    }

    [Header("Durability")]
    [SerializeField, Min(1)] private int maxDurability = 100;

    [Header("Damage")]
    [SerializeField, Min(0f)] private float damagePerSpeed = 3f;
    [SerializeField, Min(0f)] private float minHitSpeed = 1f;
    [SerializeField, Min(0.1f)] private float maxHitSpeed = 12f;
    [SerializeField, Min(0f)] private float minTimeBetweenHits = 0.1f;
    [SerializeField] private LayerMask gloveLayerMask;

    [Header("Damage Multiplier Zones")]
    [SerializeField] private List<PointCollider> pointColliders = new();

    [Header("Physics")]
    [SerializeField] private CapsuleCollider m_collider;
    [SerializeField] private ConfigurableJoint joint;
    [SerializeField] private Rigidbody m_rigidbody;
    [SerializeField, Min(0f)] private float launchForce = 2f;
    [SerializeField] private LayerMask excludeLayersOnDeath;

    [Header("Fade Settings")]
    [SerializeField, Min(0f)] private float fadeInDuration = 1f;
    [SerializeField, Min(0f)] private float fadeOutDelay = 1.5f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 2f;

    [Header("Impact Effects")]
    [SerializeField] private ParticleSystem normalHitParticlePrefab;
    [SerializeField] private ParticleSystem multiplierHitParticlePrefab;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip normalHitClip;
    [SerializeField] private AudioClip multiplierHitClip;
    [SerializeField] private AudioClip bagNormalHitClip;
    [SerializeField] private AudioClip bagMultiplierHitClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField, Range(0.5f, 2f)] private float minPitch = 0.8f;
    [SerializeField, Range(0.5f, 2f)] private float maxPitch = 1.2f;
    [SerializeField, Range(0f, 1f)] private float minVolume = 0.3f;
    [SerializeField, Range(0f, 1f)] private float maxVolume = 1f;

    public event Action<PunchingBag> OnBagDestroyed;
    public event Action<HitInfo> OnHit;

    [Header("Rewards settings")]
    public int PointsForDefeating = 1;

    private struct MaterialEntry
    {
        public Material Material;
        public Color OriginalColor;
    }

    private const string TransparentKeyword = "_SURFACE_TYPE_TRANSPARENT";
    private const float ChainProjectionDistance = 0.005f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    private static readonly int BlendId = Shader.PropertyToID("_Blend");
    private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
    private static readonly int SrcBlendAlphaId = Shader.PropertyToID("_SrcBlendAlpha");
    private static readonly int DstBlendAlphaId = Shader.PropertyToID("_DstBlendAlpha");
    private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");

    private static readonly List<InputDevice> hapticDevices = new();

    private readonly List<MaterialEntry> materialEntries = new();

    private int currentDurability;
    private bool isDead;
    private bool isInvulnerable;
    private bool isFadingIn;
    private float nextAllowedHitTime;
    private Vector3 lastHitDirection = Vector3.forward;

    public int CurrentDurability => currentDurability;
    public float DurabilityNormalized => (float)currentDurability / maxDurability;
    public bool IsDead => isDead;
    public ConfigurableJoint Joint => joint;
    public Vector3 HookWorldPosition => joint != null ? joint.transform.TransformPoint(joint.anchor) : transform.position;

    [Header("Popup Text")]
    [SerializeField] private PopupText popUpTextPrefab;

    private void Awake()
    {
        currentDurability = maxDurability;
        isDead = false;
        isInvulnerable = false;

        if (m_rigidbody == null)
            m_rigidbody = GetComponent<Rigidbody>();

        if (m_collider == null)
            m_collider = GetComponent<CapsuleCollider>();

        if (joint == null)
            joint = GetComponent<ConfigurableJoint>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        int glovesLayer = LayerMask.NameToLayer("BoxingGloves");
        if (glovesLayer != -1)
        {
            if (gloveLayerMask == 0)
                gloveLayerMask = 1 << glovesLayer;

            if (excludeLayersOnDeath == 0)
                excludeLayersOnDeath = 1 << glovesLayer;
        }

        CacheMaterials();

        pointColliders.RemoveAll(p => p == null);
        if (pointColliders.Count == 0)
            pointColliders.AddRange(GetComponentsInChildren<PointCollider>(true));
    }

    private void OnDestroy()
    {
        foreach (MaterialEntry entry in materialEntries)
        {
            if (entry.Material != null)
                Destroy(entry.Material);
        }

        materialEntries.Clear();
    }

    public void SnapHookTo(Vector3 worldPosition)
    {
        transform.position += worldPosition - HookWorldPosition;
    }

    public void AttachToAnchor(Rigidbody anchor)
    {
        if (joint == null || anchor == null)
            return;

        Vector3 worldAnchor = joint.transform.TransformPoint(joint.anchor);

        joint.autoConfigureConnectedAnchor = false;
        joint.connectedBody = anchor;
        joint.connectedAnchor = anchor.transform.InverseTransformPoint(worldAnchor);

        if (!anchor.isKinematic)
        {
            joint.enablePreprocessing = false;
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = ChainProjectionDistance;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead || isInvulnerable)
            return;

        Collider other = collision.collider;

        if (gloveLayerMask != 0 && ((1 << other.gameObject.layer) & gloveLayerMask) == 0)
            return;

        Vector3 point = collision.GetContact(0).point;

        float speed;
        Vector3 direction;

        if (TryGetGloveVelocity(other, out Vector3 velocity))
        {
            speed = velocity.magnitude;
            direction = velocity;
        }
        else
        {
            speed = collision.relativeVelocity.magnitude;
            direction = Vector3.ProjectOnPlane(transform.position - point, Vector3.up);
        }

        Hit(speed, direction, point, other);
    }

    private static bool TryGetGloveVelocity(Collider other, out Vector3 velocity)
    {
        GloveVelocityTracker tracker = other.GetComponentInParent<GloveVelocityTracker>();
        if (tracker != null)
        {
            velocity = tracker.Velocity;
            return true;
        }

        Rigidbody body = other.attachedRigidbody;
        if (body != null && !body.isKinematic)
        {
            velocity = body.linearVelocity;
            return true;
        }

        velocity = Vector3.zero;
        return false;
    }

    public bool Hit(float speed, Vector3 direction, Vector3 worldPoint)
    {
        return Hit(speed, direction, worldPoint, null);
    }

    public bool Hit(float speed, Vector3 direction, Vector3 worldPoint, Collider gloveCollider)
    {
        if (isDead || isInvulnerable || Time.time < nextAllowedHitTime)
            return false;

        if (speed < minHitSpeed)
            return false;

        nextAllowedHitTime = Time.time + minTimeBetweenHits;

        speed = Mathf.Min(speed, maxHitSpeed);

        if (direction.sqrMagnitude > 0.001f)
            lastHitDirection = direction.normalized;

        float zoneMultiplier = GetZoneMultiplier(worldPoint);
        float rawDamage = speed * damagePerSpeed * zoneMultiplier;
        int damage = rawDamage <= 0f ? 0 : Mathf.Max(1, Mathf.RoundToInt(rawDamage));

        currentDurability = Mathf.Max(0, currentDurability - damage);

        PlayImpactEffects(speed, zoneMultiplier, lastHitDirection, worldPoint, gloveCollider);

        OnHit?.Invoke(new HitInfo(damage, speed, zoneMultiplier, lastHitDirection, worldPoint));

        popUpTextPrefab.SetText(damage.ToString());

        if (currentDurability <= 0)
            Die();

        return true;
    }

    private void PlayImpactEffects(float speed, float zoneMultiplier, Vector3 direction, Vector3 point, Collider gloveCollider)
    {
        bool isMultiplier = zoneMultiplier > 1f;
        float speedNormalized = Mathf.Clamp01(Mathf.InverseLerp(minHitSpeed, maxHitSpeed, speed));

        ParticleSystem particlePrefab = isMultiplier ? multiplierHitParticlePrefab : normalHitParticlePrefab;
        if (particlePrefab == null)
            particlePrefab = normalHitParticlePrefab != null ? normalHitParticlePrefab : multiplierHitParticlePrefab;

        if (particlePrefab != null)
        {
            Quaternion rotation = direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
            ParticleSystem particleInstance = Instantiate(particlePrefab, point, rotation);

            var main = particleInstance.main;
            float scaleMultiplier = Mathf.Lerp(0.75f, 1.5f, speedNormalized);
            main.startSizeMultiplier *= scaleMultiplier;
            main.startSpeedMultiplier *= scaleMultiplier;

            var emission = particleInstance.emission;
            emission.rateOverTimeMultiplier *= Mathf.Lerp(0.8f, 2f, speedNormalized);

            float destroyDelay = main.duration + main.startLifetime.constantMax;
            Destroy(particleInstance.gameObject, destroyDelay);
        }

        AudioClip clip = isMultiplier ? multiplierHitClip : normalHitClip;
        if (clip == null)
            clip = normalHitClip != null ? normalHitClip : multiplierHitClip;

        if (clip != null)
        {
            float volume = Mathf.Lerp(minVolume, maxVolume, speedNormalized);
            float pitch = Mathf.Lerp(minPitch, maxPitch, speedNormalized);

            if (audioSource != null)
            {
                audioSource.pitch = pitch;
                audioSource.PlayOneShot(clip, volume);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clip, point, volume);
            }
        }

        AudioClip bagClip = isMultiplier ? bagMultiplierHitClip : bagNormalHitClip;
        if (bagClip == null)
            bagClip = bagNormalHitClip != null ? bagNormalHitClip : bagMultiplierHitClip;

        if (bagClip != null)
        {
            float volume = Mathf.Lerp(minVolume, maxVolume, speedNormalized);
            float pitch = Mathf.Lerp(minPitch, maxPitch, speedNormalized);

            if (audioSource != null)
            {
                audioSource.pitch = pitch;
                audioSource.PlayOneShot(bagClip, volume);
            }
            else
            {
                AudioSource.PlayClipAtPoint(bagClip, point, volume);
            }
        }

        TriggerHapticFeedback(gloveCollider, speedNormalized, zoneMultiplier);
    }

    private static void TriggerHapticFeedback(Collider gloveCollider, float speedNormalized, float zoneMultiplier)
    {
        float amplitude = Mathf.Clamp01(Mathf.Lerp(0.3f, 1f, speedNormalized) * (zoneMultiplier > 1f ? 1.25f : 1f));
        float duration = Mathf.Lerp(0.05f, 0.2f, speedNormalized);

        if (gloveCollider != null)
        {
            string searchString = gloveCollider.gameObject.name.ToLower();
            Transform parent = gloveCollider.transform.parent;
            while (parent != null)
            {
                searchString += " " + parent.name.ToLower();
                parent = parent.parent;
            }

            if (searchString.Contains("left") || searchString.Contains("_l"))
            {
                SendHapticToNode(XRNode.LeftHand, amplitude, duration);
                return;
            }

            if (searchString.Contains("right") || searchString.Contains("_r"))
            {
                SendHapticToNode(XRNode.RightHand, amplitude, duration);
                return;
            }
        }

        SendHapticToNode(XRNode.LeftHand, amplitude, duration);
        SendHapticToNode(XRNode.RightHand, amplitude, duration);
    }

    private static void SendHapticToNode(XRNode node, float amplitude, float duration)
    {
        hapticDevices.Clear();
        InputDevices.GetDevicesAtXRNode(node, hapticDevices);

        foreach (InputDevice device in hapticDevices)
        {
            if (device.isValid)
                device.SendHapticImpulse(0u, amplitude, duration);
        }
    }

    private float GetZoneMultiplier(Vector3 worldPoint)
    {
        bool found = false;
        float best = 1f;

        foreach (PointCollider zone in pointColliders)
        {
            if (zone == null || !zone.Contains(worldPoint))
                continue;

            best = found ? Mathf.Max(best, zone.Multiplier) : zone.Multiplier;
            found = true;
        }

        return best;
    }

    private void CacheMaterials()
    {
        materialEntries.Clear();

        foreach (Renderer rend in GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material mat in rend.materials)
            {
                materialEntries.Add(new MaterialEntry
                {
                    Material = mat,
                    OriginalColor = mat.HasProperty(BaseColorId) ? mat.GetColor(BaseColorId) : Color.white
                });
            }
        }
    }

    public void StartFadeIn()
    {
        if (isFadingIn || isDead)
            return;

        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        isFadingIn = true;
        isInvulnerable = true;

        yield return FadeRoutine(0f, 1f, fadeInDuration, setOpaqueOnComplete: true);

        isInvulnerable = false;
        isFadingIn = false;
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        if (deathClip != null)
        {
            if (audioSource != null)
            {
                audioSource.pitch = 1f;
                audioSource.PlayOneShot(deathClip, maxVolume);
            }
            else
            {
                AudioSource.PlayClipAtPoint(deathClip, transform.position, maxVolume);
            }
        }

        if (joint != null)
        {
            Destroy(joint);
            joint = null;
        }

        if (m_collider != null && excludeLayersOnDeath != 0)
            m_collider.excludeLayers = excludeLayersOnDeath;

        if (m_rigidbody != null)
        {
            m_rigidbody.isKinematic = false;
            m_rigidbody.AddForce(lastHitDirection * launchForce, ForceMode.Impulse);
        }

        OnBagDestroyed?.Invoke(this);

        StartCoroutine(FadeOutAndDestroy());
    }

    private IEnumerator FadeOutAndDestroy()
    {
        yield return new WaitForSeconds(fadeOutDelay);
        yield return FadeRoutine(1f, 0f, fadeOutDuration, setOpaqueOnComplete: false);
        Destroy(gameObject);
    }

    private IEnumerator FadeRoutine(float startAlpha, float targetAlpha, float duration, bool setOpaqueOnComplete)
    {
        SetTransparent(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            ApplyAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        ApplyAlpha(targetAlpha);

        if (setOpaqueOnComplete)
            SetTransparent(false);
    }

    private void ApplyAlpha(float alpha)
    {
        foreach (MaterialEntry entry in materialEntries)
        {
            Color color = entry.OriginalColor;
            color.a *= alpha;
            entry.Material.SetColor(BaseColorId, color);
        }
    }

    private void SetTransparent(bool transparent)
    {
        foreach (MaterialEntry entry in materialEntries)
            ConfigureSurface(entry.Material, transparent);
    }

    private static void ConfigureSurface(Material mat, bool transparent)
    {
        mat.SetFloat(SurfaceId, transparent ? 1f : 0f);
        mat.SetFloat(BlendId, 0f);
        mat.SetOverrideTag("RenderType", transparent ? "Transparent" : "Opaque");

        mat.SetInt(SrcBlendId, (int)(transparent ? BlendMode.SrcAlpha : BlendMode.One));
        mat.SetInt(DstBlendId, (int)(transparent ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
        mat.SetInt(SrcBlendAlphaId, (int)BlendMode.One);
        mat.SetInt(DstBlendAlphaId, (int)(transparent ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
        mat.SetInt(ZWriteId, transparent ? 0 : 1);

        if (transparent)
            mat.EnableKeyword(TransparentKeyword);
        else
            mat.DisableKeyword(TransparentKeyword);

        mat.renderQueue = (int)(transparent ? RenderQueue.Transparent : RenderQueue.Geometry);
    }
}