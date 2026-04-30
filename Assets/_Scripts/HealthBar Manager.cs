using System.Collections.Generic;
using UnityEngine;

public class HealthBarManager : MonoBehaviour
{
    public static HealthBarManager Instance { get; private set; }

    [SerializeField] private HealthBarUI barPrefab;
    [SerializeField] private int poolSize = 40;
    private RectTransform _canvasRect;


    private Queue<HealthBarUI> _pool = new();
    private Camera _cam;

    private void Awake()
    {
        Instance = this;
        _cam = Camera.main;
         _canvasRect = GetComponent<RectTransform>();

        for (int i = 0; i < poolSize; i++)
        {
            var bar = Instantiate(barPrefab, transform);
            bar.gameObject.SetActive(false);
            _pool.Enqueue(bar);
        }
    }

    public HealthBarUI Rent(Transform worldAnchor)
    {
        HealthBarUI bar = _pool.Count > 0
            ? _pool.Dequeue()
            : Instantiate(barPrefab, transform);

        bar.gameObject.SetActive(true);
        bar.Attach(worldAnchor, _cam, _canvasRect);  // instance call, not static
        return bar;
    }

    public void Return(HealthBarUI bar)
    {
        bar.Detach();                   // instance call, not static
        bar.gameObject.SetActive(false);
        _pool.Enqueue(bar);
    }
}