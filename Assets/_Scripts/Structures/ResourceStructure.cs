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
        InitializeResourceLogic();
    }

    public override void OnPlaced()
    {
        base.OnPlaced(); // Good practice
        InitializeResourceLogic();
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
        // Add nutrients to the GameManager bank
        GameManager.Instance.AddNutrients(data.nutrientsPerWave);
        
        if (data.collectEffect != null)
        {
            Instantiate(data.collectEffect, transform.position, Quaternion.identity);
        }
    }

    protected override void OnDestroyed()
    {
        // Remove the capacity if the tower is destroyed
        if (data != null && data.type == ResourceType.Hydration)
        {
            GameManager.Instance.ModifyMaxHydration(-data.hydrationCapacityBoost);
        }

        WaveManager.OnWaveCleared -= ProduceWaveNutrients;
        
        // IMPORTANT: Call base.OnDestroyed() to handle the rest of the cleanup 
        // (releasing hydration cost, freeing tiles, and the actual Destroy call)
        base.OnDestroyed();
    }

    private void OnDisable()
    {
        WaveManager.OnWaveCleared -= ProduceWaveNutrients;
    }
}