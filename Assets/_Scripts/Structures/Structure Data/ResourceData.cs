using UnityEngine;

public enum ResourceType { Nutrients, Hydration }

[CreateAssetMenu(
    menuName = "Structure/Resource",
    fileName = "ResourceData_",
    order = 30)]
public class ResourceData : StructureData
{
    [Header("Resource Settings")]
    [SerializeField] public ResourceType type;
    
    [Tooltip("Amount of Nutrients produced per collection.")]
    [SerializeField] public float nutrientsPerWave = 60f;

    [Tooltip("How much this building increases Max Hydration while standing.")]
    [SerializeField] public int hydrationCapacityBoost = 10;
    
    [Header("Visual")]
    [Tooltip("Particle effect when collecting resources.")]
    public GameObject collectEffect;
    
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