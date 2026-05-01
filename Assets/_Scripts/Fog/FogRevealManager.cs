using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class FogRevealManager : MonoBehaviour
{
    public static FogRevealManager Instance { get; private set; }

    [SerializeField] private VisualEffect fogVFX;
    [SerializeField] private float baseRadius = 3f;
    [SerializeField] private float radiusPerBuilding = 0.5f;
    [SerializeField] private float maxRadius = 20f;

    private static readonly int RadiusProperty = Shader.PropertyToID("ColliderRadius");

    private int _buildingCount = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Unregister(FogRevealer revealer)
    {
        _buildingCount = Mathf.Max(0, _buildingCount - 1);
        Debug.Log($"[Fog] Unregistered. Building count: {_buildingCount}");
        PushToVFX();
    }

    public void Register(FogRevealer revealer)
    {
        _buildingCount++;
        Debug.Log($"[Fog] Registered. Building count: {_buildingCount}");
        PushToVFX();
    }

    private void PushToVFX()
    {
        if (fogVFX == null) return;
        float radius = Mathf.Min(baseRadius + _buildingCount * radiusPerBuilding, maxRadius);
        fogVFX.SetFloat(RadiusProperty, radius);
    }
}