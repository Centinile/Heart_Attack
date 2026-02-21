using UnityEngine;

/// <summary>
/// Represents a resource-generating structure.
/// </summary>
public class ResourceStructure : Building
{
    [Header("References")]
    [SerializeField] private ResourceData data;
    
    [Header("State")]
    [SerializeField] private float currentResources;
    [SerializeField] private float collectionTimer;
    
    public ResourceData Data => data;
    public float CurrentResources => currentResources;
    public float StoragePercent => data.MaxStorage > 0 ? currentResources / data.MaxStorage : 0f;
    public bool IsFull => currentResources >= data.MaxStorage;
    
    protected override void Awake()
    {
        base.Awake();
        structureType = StructureType.Resource;
        currentResources = 0f;
        collectionTimer = 0f;
    }
    
    /// <summary>
    /// Configures the resource structure with data from a ScriptableObject.
    /// </summary>
    public void Configure(ResourceData resourceData)
    {
        data = resourceData;
        maxHP = resourceData.MaxHP;
        currentHP = maxHP;
        currentResources = 0f;
    }
    
    void Update()
    {
        if (data == null) return;
        
        // Generate resources over time
        if (currentResources < data.MaxStorage)
        {
            collectionTimer += Time.deltaTime;
            
            if (collectionTimer >= data.CollectionInterval)
            {
                collectionTimer = 0f;
                GenerateResources();
            }
        }
    }
    
    private void GenerateResources()
    {
        currentResources = Mathf.Min(currentResources + data.ResourceAmount, data.MaxStorage);
        
        // Spawn collection effect
        if (data.CollectEffect != null)
        {
            Instantiate(data.CollectEffect, transform.position, Quaternion.identity);
        }
        
        Debug.Log($"Generated {data.ResourceAmount} {data.ResourceType}. Current: {currentResources}/{data.MaxStorage}");
    }
    
    /// <summary>
    /// Collects all available resources from this structure.
    /// </summary>
    /// <returns>The amount of resources collected.</returns>
    public float CollectResources()
    {
        float collected = currentResources;
        currentResources = 0f;
        
        Debug.Log($"Collected {collected} {data.ResourceType}");
        
        return collected;
    }
    
    /// <summary>
    /// Collects a specific amount of resources.
    /// </summary>
    /// <param name="amount">Amount to collect.</param>
    /// <returns>The actual amount collected.</returns>
    public float CollectResources(float amount)
    {
        float collected = Mathf.Min(currentResources, amount);
        currentResources -= collected;
        
        return collected;
    }
    
    protected override void OnDestroyed()
    {
        // Return resources to player before destruction
        if (currentResources > 0)
        {
            Debug.Log($"Lost {currentResources} {data.ResourceType} when structure was destroyed!");
        }
        
        Destroy(gameObject);
    }
    
    public override void OnPlaced()
    {
        collectionTimer = 0f;
        Debug.Log("Resource structure placed!");
    }
}