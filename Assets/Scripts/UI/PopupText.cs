using UnityEngine;
using TMPro;

public class PopupText : MonoBehaviour
{
    [SerializeField] private TMP_Text myText;

    [SerializeField] private float speed = 1f;
    [SerializeField] private float lifetime = 1f;

    private float currentLifetime = 0f;

    void Update()
    {
        transform.position += Vector3.up * speed * Time.deltaTime;

        currentLifetime += Time.deltaTime;

        if (currentLifetime >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    public void SetText(string text)
    {
        myText.text = text;
    }

    public void ChangeColor(Color color)
    {
        myText.color = color;
    }
}
