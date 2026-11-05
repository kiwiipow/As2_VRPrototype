using UnityEngine;

public class GrabGloves : MonoBehaviour
{
    public OVRInput.Controller controller = OVRInput.Controller.LTouch;
    public OVRInput.Button grabButton = OVRInput.Button.PrimaryIndexTrigger; 
    public OVRInput.Button releaseButton = OVRInput.Button.One;

    public float grabRadius = 0;
    public LayerMask grabLayers = 0;
    public bool dropWithPhysics = true;
    public Vector3 dropVelocity = UnityEngine.Vector3.zero;
    public Vector3 holdOffset = new Vector3(0f, 0f, -0.1f);

    Rigidbody held;
    Transform originalParent;
    bool isKinematic;
    bool hasGravity;
    public Vector3 heldRotation;

    //Controller mesh refs
    public SkinnedMeshRenderer[] leftMeshes;
    public SkinnedMeshRenderer[] rightMeshes;
    void Update()
    {
        if(held == null && OVRInput.GetDown(grabButton, controller))
        {
            
                Grab();
            
        }
        else if (held != null && OVRInput.GetDown(releaseButton, controller))
        {
            
                Release();
            
        }

    }
    void Grab()
    {
        var hits = Physics.OverlapSphere(transform.position, grabRadius, grabLayers);
        Rigidbody best = null;
        float bestDist = float.MaxValue;

        foreach (var h in hits)
        {
            var rb = h.attachedRigidbody;
            if (rb == null)
            {
                continue;
            }
            float d = Vector3.Distance(transform.position, rb.worldCenterOfMass);
            if (d < bestDist) 
            { 
                bestDist = d; best = rb; 
            }
        }
        if (best == null)
        {
            return;
        }

        held = best;
        originalParent = held.transform.parent;
        isKinematic = held.isKinematic;
        hasGravity = held.useGravity;
        held.isKinematic = true;
        held.useGravity = false;
        held.transform.SetParent(transform, true);
        held.transform.localPosition = holdOffset;
        held.transform.localRotation = Quaternion.Euler(heldRotation.x, heldRotation.y, heldRotation.z);


        if (controller == OVRInput.Controller.LTouch)
        {
            foreach (var m in leftMeshes)
            {
                if (m != null) m.enabled = false;
            }

        }
        else if (controller == OVRInput.Controller.RTouch)
        {
            foreach (var m in rightMeshes)
            {
                if (m != null) m.enabled = false;
            }
        }
    }

    void Release()
    {
        held.transform.SetParent(originalParent, true);
        held.isKinematic = false;
        held.useGravity = true;
        held.linearVelocity = dropVelocity;
        held.angularVelocity = Vector3.zero;
        
        held = null;

        if (controller == OVRInput.Controller.LTouch)
        {
            foreach (var m in leftMeshes)
            {
                if (m != null) m.enabled = true;
            }

        }
        else if (controller == OVRInput.Controller.RTouch)
        {
            foreach (var m in rightMeshes)
            {
                if (m != null) m.enabled = true;
            }
        }
    }
}
