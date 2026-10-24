using System.Collections.Generic;
using UnityEngine;

public class PunchBagChain : MonoBehaviour
{
    [Header("Shape")]
    [Tooltip("Number of links between the ceiling anchor and the bag hook.")]
    [SerializeField, Min(1)] private int linkCount = 8;

    [Tooltip("Length of each link in meters. Match it to the length of your link mesh (along its Y axis).")]
    [SerializeField, Min(0.01f)] private float linkLength = 0.12f;

    [Tooltip("Thickness of the fallback capsule visual. Ignored when a link prefab is assigned.")]
    [SerializeField, Min(0.005f)] private float linkThickness = 0.025f;

    [Tooltip("Where the chain starts, in the local space of the ceiling anchor.")]
    [SerializeField] private Vector3 anchorLocalOffset = Vector3.zero;

    [Header("Visuals")]
    [Tooltip("Optional link model, authored along its Y axis. If empty, a plain capsule is used.")]
    [SerializeField] private GameObject linkPrefab;

    [Tooltip("Material for the fallback capsule. Ignored when a link prefab is assigned.")]
    [SerializeField] private Material linkMaterial;

    [Tooltip("Turns every second link 90 degrees around the chain so it reads as interlocked rings.")]
    [SerializeField] private bool alternateLinkRotation = true;

    [Header("Physics")]
    [SerializeField, Min(0.001f)] private float linkMass = 0.5f;
    [SerializeField, Min(0f)] private float linearDamping = 0.2f;
    [SerializeField, Min(0f)] private float angularDamping = 0.5f;

    [Tooltip("How far a link can bend relative to its neighbour, in degrees.")]
    [SerializeField, Range(5f, 177f)] private float maxBendAngle = 80f;

    [Tooltip("Higher values keep the chain stiffer and less jittery when a heavy bag hangs from it.")]
    [SerializeField, Min(1)] private int solverIterations = 16;
    [SerializeField, Min(1)] private int solverVelocityIterations = 8;

    private readonly List<Rigidbody> links = new();

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
    }
}
