using System;
using UnityEngine;

public abstract class BuildingData : ScriptableObject
{
    [Header("Base Settings")]
    [SerializeField] private string structureName = "New Structure";
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private BuildingTier tier = BuildingTier.Tier1;
    [SerializeField] private string description = "Structure Description";

    [Header("Economy")]
    [SerializeField] private float nutrientCost = 100f;
    [SerializeField] private float hydrationCost = 5f;

    [Header("Upgrade")]
    [SerializeField] private BuildingData nextLevelData;
    [SerializeField] private float upgradeCost = 150f;

    [Header("Visual")]
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject prefab;

    public string StructureName => structureName;
    public float MaxHP => maxHP;
    public BuildingTier Tier => tier;
    public float NutrientCost => nutrientCost;
    public float HydrationCost => hydrationCost;
    public float UpgradeCost => upgradeCost;
    public BuildingData NextLevelData => nextLevelData;
    public Sprite Icon => icon;
    public GameObject Prefab => prefab;
    public String Description => description;

    public abstract StructureType GetStructureType();

    public virtual void ConfigureBuilding(Building building) { }
}