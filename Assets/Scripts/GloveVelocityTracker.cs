using UnityEngine;


public class GloveVelocityTracker : MonoBehaviour
{
    [Tooltip("0 = raw velocity, closer to 1 = smoother but laggier.")]
    [SerializeField, Range(0f, 0.95f)] private float smoothing = 0.4f;

    private Vector3 lastPosition;

    public Vector3 Velocity { get; private set; }
    public float Speed => Velocity.magnitude;

    private void OnEnable()
    {
        lastPosition = transform.position;
        Velocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        Vector3 position = transform.position;
        Vector3 rawVelocity = (position - lastPosition) / Time.fixedDeltaTime;

        Velocity = Vector3.Lerp(rawVelocity, Velocity, smoothing);
        lastPosition = position;
    }
}
