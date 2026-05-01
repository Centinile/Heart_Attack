using UnityEngine;

[CreateAssetMenu(
    menuName = "Structure/Heart",
    fileName = "HeartData_",
    order = 20)]
public class HeartData : BuildingData
{
    [Header("Heart Settings")]
    [Tooltip("Whether the heart can be attacked directly.")]
    [SerializeField] private bool invulnerable = false;
    
    [Tooltip("Visual effect when heart takes damage.")]
    [SerializeField] private GameObject damageEffect;
    
    [Header("Game Flow")]
    [Tooltip("If true, game ends when heart is destroyed.")]
    [SerializeField] private bool isMainHeart = true;
    
    // Public accessors
    public bool Invulnerable => invulnerable;
    public GameObject DamageEffect => damageEffect;
    public bool IsMainHeart => isMainHeart;
    
    public override StructureType GetStructureType() => StructureType.Heart;
    
    public override void ConfigureBuilding(Building building)
    {
        base.ConfigureBuilding(building);
        
        if (building is Heart heart)
        {
            heart.Configure(this);
        }
    }
}