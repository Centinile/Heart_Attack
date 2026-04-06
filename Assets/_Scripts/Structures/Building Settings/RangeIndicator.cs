using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RangeIndicator : MonoBehaviour
{
    private LineRenderer lineRenderer;
    [SerializeField] private int segments = 50; // Higher = smoother circle

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false; // Makes it follow the tower move/spawn
        lineRenderer.loop = true;
        lineRenderer.positionCount = segments;
        gameObject.SetActive(false); // Hidden by default
    }

    public void Show(float radius)
    {
        float angle = 0f;
        for (int i = 0; i < segments; i++)
        {
            float x = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
            float y = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;

            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
            angle += (360f / segments);
        }
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}