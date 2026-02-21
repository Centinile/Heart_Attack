using UnityEngine;

[CreateAssetMenu(
    menuName = "Structure/Defense Tower",
    fileName = "DefenseData_",
    order = 10)]
public class DefenseData : StructureData
{
    [Header("Combat Stats")]
    [Tooltip("Damage dealt per attack.")]
    [SerializeField] private float damage = 10f;
    
    [Tooltip("Time between attacks in seconds.")]
    [SerializeField] private float attackSpeed = 1f;
    
    [Tooltip("Attack range in world units.")]
    [SerializeField] private float range = 5f;
    
    [Tooltip("How the tower selects its target.")]
    [SerializeField] private TargetMode targetMode = TargetMode.ClosestToHeart;
    
    [Tooltip("Type of attack performed.")]
    [SerializeField] private AttackType attackType = AttackType.SingleTarget;
    
    [Header("Projectile")]
    [Tooltip("Projectile data for the projectile this tower fires.")]
    [SerializeField] private ProjectileData projectileData;
    
    [Tooltip("Speed of the projectile.")]
    [SerializeField] private float projectileSpeed = 10f;
    
    [Header("Multi-Target Settings")]
    [Tooltip("Maximum number of targets for MultiTarget attack type.")]
    [SerializeField] private int maxTargets = 3;
    
    [Header("Splash Settings")]
    [Tooltip("Radius of splash damage.")]
    [SerializeField] private float splashRadius = 2f;
    
    [Header("Upgrades")]
    [Tooltip("Next level upgrade data (optional).")]
    [SerializeField] private DefenseData nextLevel;
    
    // Public accessors
    public float Damage => damage;
    public float AttackSpeed => attackSpeed;
    public float Range => range;
    public TargetMode TargetMode => targetMode;
    public AttackType AttackType => attackType;
    public ProjectileData ProjectileData => projectileData;
    public float ProjectileSpeed => projectileSpeed;
    public int MaxTargets => maxTargets;
    public float SplashRadius => splashRadius;
    public DefenseData NextLevel => nextLevel;
    public bool HasUpgrade => nextLevel != null;
    
    public override StructureType GetStructureType() => StructureType.Defense;
}