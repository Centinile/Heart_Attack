using UnityEngine;

[CreateAssetMenu(
    menuName = "Structure/Wall",
    fileName = "WallData_",
    order = 40)]
public class WallData : BuildingData
{
    [Header("Wall Settings")]
    [Tooltip("If true, enemies will attack walls to reach the heart.")]
    [SerializeField] private bool prioritizedByEnemies = true;
    
    [Tooltip("Additional damage reduction from ranged attacks.")]
    
    // Public accessors
    public bool PrioritizedByEnemies => prioritizedByEnemies;
    public override StructureType GetStructureType() => StructureType.Wall;
}