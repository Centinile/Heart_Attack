using UnityEngine;

public class ResourceStructure : Building
{
    [SerializeField] private ResourceData data;

    protected override void Awake()
    {
        base.Awake();
        structureType = StructureType.Resource;
    }

    public void Configure(ResourceData resourceData)
    {
        data = resourceData;
        maxHP = resourceData.MaxHP;
        currentHP = maxHP;

        InitializeResourceLogic();
    }

    public override void OnPlaced()
    {
        InitializeResourceLogic();
    }

    private bool initialized = false;
    private void InitializeResourceLogic()
    {
        if (data == null || initialized) return;

        // 1. HYDRATION
        if (data.type == ResourceType.Hydration)
        {
            GameManager.Instance.ModifyMaxHydration(data.hydrationCapacityBoost);
            Debug.Log($"[Hydration] Boosted by {data.hydrationCapacityBoost}");
        }
        
        // 2. NUTRIENTS
        if (data.type == ResourceType.Nutrients)
        {
            // Safety: Unsubscribe first to avoid double-subscribing
            WaveManager.OnWaveCleared -= ProduceWaveNutrients;
            WaveManager.OnWaveCleared += ProduceWaveNutrients;
            Debug.Log("[Nutrients] Subscribed to WaveManager");
        }

        initialized = true;
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
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        // Essential to prevent errors if the level changes or tower is disabled
        WaveManager.OnWaveCleared -= ProduceWaveNutrients;
    }
}