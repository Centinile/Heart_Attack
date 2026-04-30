using UnityEngine;

[CreateAssetMenu(menuName = "Structure/Defense Tower", fileName = "DefenseData_")]
public class DefenseData : BuildingData
{
    [Header("Combat Stats")]
    public float damage = 10f;
    [Tooltip("Seconds between attacks (e.g., 0.5 = fires every half second)")]
    public float attackCooldown = 1f; 
    public float range = 5f;
    public AttackType attackType = AttackType.SingleTarget;
    
    [Header("Visuals (4-Directional)")]
    public bool useFourDirectionalFacing = false;
    public Sprite spriteUp;
    public Sprite spriteDown;
    public Sprite spriteLeft;
    public Sprite spriteRight;

    [Header("Projectile")]
    public ProjectileData projectileData;
    public float projectileSpeed = 10f;
    
    [Header("Multi-Target / Splash")]
    public int maxTargets = 3;
    public float splashRadius = 2f;

    public override StructureType GetStructureType() => StructureType.Defense;
}