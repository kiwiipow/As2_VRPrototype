using System.Collections.Generic;
using UnityEngine;

public class PunchBagChain : MonoBehaviour
{
    [Header("Shape")]
    [SerializeField, Min(1)] private int linkCount = 8;

    [SerializeField, Min(0.01f)] private float linkLength = 0.12f;

    [SerializeField, Min(0.005f)] private float linkThickness = 0.025f;

    [SerializeField] private Vector3 anchorLocalOffset = Vector3.zero;

    [Header("Visuals")]
    [SerializeField] private GameObject linkPrefab;

    [SerializeField] private Material linkMaterial;

    [SerializeField] private bool alternateLinkRotation = true;

    [Header("Physics")]
    [SerializeField, Min(0.001f)] private float linkMass = 1f;
    [SerializeField, Min(0f)] private float linearDamping = 0.05f;
    [SerializeField, Min(0f)] private float angularDamping = 0.25f;

    [Header("Unloaded Behaviour")]
    [SerializeField, Min(1f)] private float unloadedGravityMultiplier = 3f;
    [SerializeField, Min(0f)] private float unloadedLinearDamping = 0f;
    [SerializeField, Min(0f)] private float unloadedAngularDamping = 0.1f;

    [SerializeField, Range(5f, 177f)] private float maxBendAngle = 80f;

    [SerializeField, Min(0.0005f)] private float projectionDistance = 0.005f;

    [SerializeField, Min(1)] private int solverIterations = 32;
    [SerializeField, Min(1)] private int solverVelocityIterations = 16;

    private readonly List<Rigidbody> links = new();

    private bool isLoaded;

    public bool IsBuilt => links.Count > 0;
    public Rigidbody End => IsBuilt ? links[links.Count - 1] : null;

    public Vector3 EndPosition => IsBuilt
        ? End.transform.TransformPoint(Vector3.down * (linkLength * 0.5f))
        : transform.position;

    public void Build(Rigidbody anchorBody)
    {
        if (IsBuilt || anchorBody == null)
            return;

        Vector3 top = anchorBody.transform.TransformPoint(anchorLocalOffset);

        Rigidbody previous = anchorBody;
        Vector3 previousConnectedAnchor = anchorBody.transform.InverseTransformPoint(top);

        for (int i = 0; i < linkCount; i++)
        {
            Vector3 center = top + Vector3.down * (linkLength * (i + 0.5f));
            Rigidbody link = CreateLink(i, center);

            ConfigureJoint(link.gameObject.AddComponent<ConfigurableJoint>(), previous, previousConnectedAnchor);

            links.Add(link);
            previous = link;
            previousConnectedAnchor = Vector3.down * (linkLength * 0.5f);
        }

        ApplyDamping();
    }

    public void SetLoaded(bool loaded)
    {
        isLoaded = loaded;
        ApplyDamping();
    }

    private void ApplyDamping()
    {
        foreach (Rigidbody link in links)
        {
            link.linearDamping = isLoaded ? linearDamping : unloadedLinearDamping;
            link.angularDamping = isLoaded ? angularDamping : unloadedAngularDamping;
        }
    }

    private void FixedUpdate()
    {
        if (isLoaded || unloadedGravityMultiplier <= 1f)
            return;

        Vector3 extraGravity = Physics.gravity * (unloadedGravityMultiplier - 1f);

        foreach (Rigidbody link in links)
            link.AddForce(extraGravity, ForceMode.Acceleration);
    }

    private Rigidbody CreateLink(int index, Vector3 position)
    {
        GameObject linkObject = new GameObject($"ChainLink_{index}");
        linkObject.transform.SetParent(transform, true);
        linkObject.transform.SetPositionAndRotation(position, Quaternion.identity);

        Rigidbody body = linkObject.AddComponent<Rigidbody>();
        body.mass = linkMass;
        body.linearDamping = linearDamping;
        body.angularDamping = angularDamping;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.solverIterations = solverIterations;
        body.solverVelocityIterations = solverVelocityIterations;

        CreateVisual(linkObject.transform, index);

        return body;
    }

    private void CreateVisual(Transform parent, int index)
    {
        Quaternion rotation = alternateLinkRotation && index % 2 == 1
            ? Quaternion.Euler(0f, 90f, 0f)
            : Quaternion.identity;

        GameObject visual;

        if (linkPrefab != null)
        {
            visual = Instantiate(linkPrefab, parent);
        }
        else
        {
            visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.transform.SetParent(parent, false);
            visual.transform.localScale = new Vector3(linkThickness, linkLength * 0.5f, linkThickness);

            Destroy(visual.GetComponent<Collider>());

            if (linkMaterial != null)
                visual.GetComponent<Renderer>().sharedMaterial = linkMaterial;
        }

        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = rotation;
    }

    private void ConfigureJoint(ConfigurableJoint joint, Rigidbody connectedBody, Vector3 connectedAnchor)
    {
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedBody = connectedBody;
        joint.anchor = Vector3.up * (linkLength * 0.5f);
        joint.connectedAnchor = connectedAnchor;

        joint.axis = Vector3.up;
        joint.secondaryAxis = Vector3.right;

        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;

        joint.angularXMotion = ConfigurableJointMotion.Free;
        joint.angularYMotion = ConfigurableJointMotion.Limited;
        joint.angularZMotion = ConfigurableJointMotion.Limited;

        SoftJointLimit bendLimit = new SoftJointLimit { limit = maxBendAngle };
        joint.angularYLimit = bendLimit;
        joint.angularZLimit = bendLimit;

        joint.enableCollision = false;
        joint.enablePreprocessing = false;
        joint.projectionMode = JointProjectionMode.PositionAndRotation;
        joint.projectionDistance = projectionDistance;
    }
}
