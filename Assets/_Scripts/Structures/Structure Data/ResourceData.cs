using UnityEngine;
public enum ResourceType { Nutrients, Hydration }

[CreateAssetMenu(menuName = "Structure/Resource", fileName = "ResourceData_", order = 30)]
public class ResourceData : StructureData
{
    [Header("Resource Settings")]
    public ResourceType type;
    public float nutrientsPerWave = 60f;
    public int hydrationCapacityBoost = 10;
    
    [Header("Visual")]
    public GameObject collectEffect;
    
    public override StructureType GetStructureType() => StructureType.Resource;
    
    public override void ConfigureBuilding(Building building)
    {
        // We don't need to manually set health here anymore because 
        // Building.Initialize(this) will do it automatically.
        
        if (building is ResourceStructure resource)
        {
            resource.Configure(this);
        }
    }
}