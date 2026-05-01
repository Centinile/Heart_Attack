using Unity.VisualScripting;
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

    [Header("Continuous Attack")]
    public float continuousBaseDamage = 5f;
    public float continuousMaxDamage = 50f;
    public float continuousRampTime = 4f; // seconds to reach max damage
    public float continuousTickRate = 0.1f; // seconds between damage ticks

    [Header("DOT Settings")]
    public bool applyDOT = false;
    public float dotDamagePerTick = 5f;
    public float dotTickInterval = 0.5f;
    public float dotDuration = 3f;
    public float dotRadius = 1.5f;
    public GameObject dotZonePrefab;

    public override StructureType GetStructureType() => StructureType.Defense;
}