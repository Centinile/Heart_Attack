using UnityEngine;

public class ResourceStructure : Building
{
    [SerializeField] private ResourceData data;
    private bool initializedLogic = false;

    // We no longer need Awake to set structureType; Initialize handles it.

    public void Configure(ResourceData resourceData)
    {
        data = resourceData;
        // The base class Initialize will handle HP and StructureType, 
        // we just focus on the unique resource logic.
    }

    public override void OnPlaced()
    {
        base.OnPlaced(); // Good practice
    }

    public override void Initialize(BuildingData structureData)
    {
        base.Initialize(structureData); // TryActivate runs here
        InitializeResourceLogic();      // Hydration boost runs immediately after
    }

    private void InitializeResourceLogic()
    {
        if (data == null || initializedLogic) return;

        // 1. HYDRATION
        if (data.type == ResourceType.Hydration)
        {
            GameManager.Instance.ModifyMaxHydration(data.hydrationCapacityBoost);
            Debug.Log($"[Hydration] Boosted by {data.hydrationCapacityBoost}");
        }
        
        // 2. NUTRIENTS
        if (data.type == ResourceType.Nutrients)
        {
            WaveManager.OnWaveCleared -= ProduceWaveNutrients;
            WaveManager.OnWaveCleared += ProduceWaveNutrients;
            Debug.Log("[Nutrients] Subscribed to WaveManager");
        }

        initializedLogic = true;
    }

    private void ProduceWaveNutrients()
    {
        if (!IsPowered) return;

        GameManager.Instance.AddNutrients(data.nutrientsPerWave);
        
        if (data.collectEffect != null)
            Instantiate(data.collectEffect, transform.position, Quaternion.identity);
    }

    protected override void OnDestroyed()
    {
        if (data != null && data.type == ResourceType.Hydration)
        {
            // ModifyMaxHydration now handles depowering internally
            GameManager.Instance.ModifyMaxHydration(-data.hydrationCapacityBoost);
        }

        WaveManager.OnWaveCleared -= ProduceWaveNutrients;

        base.OnDestroyed(); // Handles tile freeing + Destroy()
    }

    private void OnDisable()
    {
        WaveManager.OnWaveCleared -= ProduceWaveNutrients;
    }
}