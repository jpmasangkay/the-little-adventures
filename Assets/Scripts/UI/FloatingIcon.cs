using UnityEngine;

public class FloatingIcon : MonoBehaviour
{
    public float floatSpeed = 1f;
    public float floatHeight = 10f;
    public float rotationSpeed = 15f;
    
    private Vector3 startPos;
    private RectTransform rect;
    private float randomOffset;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            startPos = rect.anchoredPosition;
        }
        else
        {
            startPos = transform.position;
        }
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float newY = Mathf.Sin(Time.time * floatSpeed + randomOffset) * floatHeight;
        
        if (rect != null)
        {
            rect.anchoredPosition = startPos + new Vector3(0, newY, 0);
            rect.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = startPos + new Vector3(0, newY, 0);
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
    }
}
