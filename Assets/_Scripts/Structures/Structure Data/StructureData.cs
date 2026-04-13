using UnityEngine;

/// <summary>
/// Base class for all structure data ScriptableObjects.
/// </summary>
public abstract class StructureData : ScriptableObject
{
    [Header("Base Settings")]
    [SerializeField] private string structureName = "New Structure";
    [SerializeField] private float maxHP = 100f;

    [Header("Economy")]
    [SerializeField] private float nutrientCost = 100f;
    [SerializeField] private float hydrationCost = 5f;

    [Header("Upgrade")]
    [SerializeField] private StructureData nextLevelData;
    [SerializeField] private float upgradeCost = 150f;
    
    [Header("Visual")]
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject prefab;
    
    public string StructureName => structureName;
    public float MaxHP => maxHP;
    public float NutrientCost => nutrientCost;
    public float HydrationCost => hydrationCost;
    public float UpgradeCost => upgradeCost;
    public StructureData NextLevelData => nextLevelData;
    public Sprite Icon => icon;
    public GameObject Prefab => prefab;
    
    public abstract StructureType GetStructureType();

    public virtual void ConfigureBuilding(Building building)
    {
        // Base logic: Initialize handles the core stats now
    }
}