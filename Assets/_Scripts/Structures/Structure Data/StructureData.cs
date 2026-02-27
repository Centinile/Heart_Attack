using UnityEngine;

/// <summary>
/// Base class for all structure data ScriptableObjects.
/// </summary>
public abstract class StructureData : ScriptableObject
{
    [Header("Base Settings")]
    [Tooltip("Display name of this structure.")]
    [SerializeField] protected string structureName = "New Structure";
    
    [Tooltip("Maximum health points.")]
    [SerializeField] protected float maxHP = 100f;

    [Header("Economy")]
    [SerializeField] protected float nutrientCost = 100f;
    [SerializeField] protected float hydrationCost = 5f;

    [Header("Upgrade")]
    [SerializeField] protected StructureData nextLevelData;
    [SerializeField] protected float upgradeCost = 150f;
    
    [Header("Visual")]
    [Tooltip("Icon shown in UI.")]
    [SerializeField] protected Sprite icon;
    
    [Tooltip("Prefab to spawn.")]
    [SerializeField] protected GameObject prefab;
    
    // Public accessors
    public string StructureName => structureName;
    public float MaxHP => maxHP;
    public float NutrientCost => nutrientCost;
    public float HydrationCost => hydrationCost;
    public float UpgradeCost => upgradeCost;
    public StructureData NextLevelData => nextLevelData;
    public Sprite Icon => icon;
    public GameObject Prefab => prefab;
    
    /// Returns the type of structure this data represents.
    public abstract StructureType GetStructureType();
    
    /// Configures the building component with this data.
    /// <param name="building">The Building component to configure.</param>
    public virtual void ConfigureBuilding(Building building)
    {
        building.SetHealth(maxHP);
    }
}