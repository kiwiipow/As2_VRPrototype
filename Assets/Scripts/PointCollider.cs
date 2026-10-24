using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PointCollider : MonoBehaviour
{
    [SerializeField] private BoxCollider m_collider;

    [SerializeField, Min(0f)] private float multiplier = 2f;
    [SerializeField, Min(0f)] private float tolerance = 0.02f;

    public float Multiplier => multiplier;

    private void Reset()
    {
        m_collider = GetComponent<BoxCollider>();
    }

    private void Awake()
    {
        if (m_collider == null)
            m_collider = GetComponent<BoxCollider>();

        if (m_collider != null)
            m_collider.isTrigger = true;
    }

    public bool Contains(Vector3 worldPoint)
    {
        if (m_collider == null || !m_collider.enabled || !isActiveAndEnabled)
            return false;

        Vector3 closest = m_collider.ClosestPoint(worldPoint);
        return (closest - worldPoint).sqrMagnitude <= tolerance * tolerance;
    }

    private void OnDrawGizmos()
    {
        BoxCollider box = m_collider != null ? m_collider : GetComponent<BoxCollider>();
        if (box == null)
            return;

        Color color = multiplier > 1f ? Color.green : multiplier < 1f ? Color.red : Color.yellow;

        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = new Color(color.r, color.g, color.b, 0.2f);
        Gizmos.DrawCube(box.center, box.size);

        Gizmos.color = color;
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
