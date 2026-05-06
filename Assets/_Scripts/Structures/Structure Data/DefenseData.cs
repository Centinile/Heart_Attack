using Unity.VisualScripting;
using UnityEngine;
public enum TargetFilter
{
    Both,
    GroundOnly,
    FlyingOnly
}

[CreateAssetMenu(menuName = "Structure/Defense Tower", fileName = "DefenseData_")]
public class DefenseData : BuildingData
{
    [Header("Combat Stats")]
    public float damage = 10f;
    [Tooltip("Seconds between attacks (e.g., 0.5 = fires every half second)")]
    public float attackCooldown = 1f; 
    public float range = 5f;
    public AttackType attackType = AttackType.SingleTarget;

    [Header("Target Filter")]
    public TargetFilter targetFilter = TargetFilter.Both;
    
    [Header("Visuals (4-Directional)")]
    public bool useFourDirectionalFacing = false;

    [Header("Audio")]
    public AudioClip attackSound;       // plays once when tower fires
    public AudioClip beamLoopSound;     // loops while continuous beam is active
    [Range(0f, 1f)] public float attackSoundVolume = 1f;
    [Range(0f, 1f)] public float beamLoopVolume = 0.8f;

    [Header("Projectile")]
    public ProjectileData projectileData;
    public float projectileSpeed = 10f;
    public bool useLineRendererAttack = false;
    public FlickerBeam flickerBeamPrefab; // assign a prefab with FlickerBeam + LineRenderer
    
    [Header("Multi-Target / Splash")]
    public int maxTargets = 3;
    public float splashRadius = 2f;

    [Header("Burst")]
    public bool useBurst = false;
    [Tooltip("Number of hits fired per attack")]
    public int burstCount = 3;
    [Tooltip("Seconds between each hit in the burst")]
    public float burstInterval = 0.1f;

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