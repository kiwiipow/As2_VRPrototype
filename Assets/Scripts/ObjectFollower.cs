using UnityEngine;

public class ObjectFollower : MonoBehaviour
{
    [SerializeField] private Transform target;

    [SerializeField] private Vector3 offset;

    void FixedUpdate()
    {
        transform.position = target.position + offset;
    }
}
