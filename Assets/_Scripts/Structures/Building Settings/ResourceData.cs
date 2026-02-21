using UnityEngine;

[CreateAssetMenu(
    menuName = "Structure/Resource",
    fileName = "ResourceData_",
    order = 30)]
public class ResourceData : StructureData
{
    [Header("Resource Settings")]
    [Tooltip("Type of resource this structure produces.")]
    [SerializeField] private string resourceType = "Gold";
    
    [Tooltip("Amount of resource produced per collection.")]
    [SerializeField] private float resourceAmount = 10f;
    
    [Tooltip("Time in seconds between resource collections.")]
    [SerializeField] private float collectionInterval = 5f;
    
    [Tooltip("Maximum resources that can be stored.")]
    [SerializeField] private float maxStorage = 100f;
    
    [Header("Visual")]
    [Tooltip("Particle effect when collecting resources.")]
    [SerializeField] private GameObject collectEffect;
    
    // Public accessors
    public string ResourceType => resourceType;
    public float ResourceAmount => resourceAmount;
    public float CollectionInterval => collectionInterval;
    public float MaxStorage => maxStorage;
    public GameObject CollectEffect => collectEffect;
    
    public override StructureType GetStructureType() => StructureType.Resource;
    
    public override void ConfigureBuilding(Building building)
    {
        base.ConfigureBuilding(building);
        
        if (building is ResourceStructure resource)
        {
            resource.Configure(this);
        }
    }
}