using UnityEngine;

public abstract class BuildingData : ScriptableObject
{
    [Header("Basic Info")]
    public string buildingName;
    public Sprite icon;
    public GameObject prefab;

    [Header("Economy")]
    public int cost;
    public int sellValue;

    [Header("Health")]
    public float maxHP;

    [Header("Upgrade")]
    public BuildingData nextUpgrade; // points to next level
}
