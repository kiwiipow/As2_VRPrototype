using UnityEngine;

public class SkyClouds : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 0.05f;

    private Material cloudMaterial;
    private float currentOffsetX;

    void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            cloudMaterial = rend.material;
        }
    }

    void Update()
    {
        if (cloudMaterial != null)
        {
            currentOffsetX = Mathf.Repeat(currentOffsetX + scrollSpeed * Time.deltaTime, 1f);
            Vector2 offset = cloudMaterial.mainTextureOffset;
            offset.x = currentOffsetX;
            cloudMaterial.mainTextureOffset = offset;
        }
    }
}