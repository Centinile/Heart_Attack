using UnityEngine;
using System.Collections.Generic;

public enum TargetPriority
{
    WallOnly,
    AnyNonWall,
    Defense,
    Resource,
    None,
}

[CreateAssetMenu(
    menuName = "Enemy/Enemy Data",
    fileName = "EnemyData_",
    order = 0)]
public class EnemyData : ScriptableObject
{
    [Header("Base Stats")]
    [Tooltip("Maximum health points.")]
    [SerializeField] private float maxHP = 100f;
    
    [Tooltip("Damage dealt per attack.")]
    [SerializeField] private float attackDamage = 10f;
    
    [Tooltip("Distance at which the enemy stops to attack.")]
    [SerializeField] private float attackRadius = 1.5f;
    
    [Tooltip("Movement speed on the NavMesh.")]
    [SerializeField] private float moveSpeed = 3.5f;
    
    [Header("Combat Configuration")]
    [Tooltip("If true, enemy is ranged and can attack from distance.")]
    [SerializeField] private bool isRanged = false;
    
    [Tooltip("Time between attacks in seconds.")]
    [SerializeField] private float attackCooldown = 1f;

    [Tooltip("If true, enemy is airborne and ignores walls.")]
    [SerializeField] private bool isFlying = false;
    
    [Header("AI Settings")]
    [Tooltip("Priority for selecting attack targets.")]
    [SerializeField] private TargetPriority targetingPriority = TargetPriority.None;
    
    [Tooltip("Radius for detecting valid targets.")]
    [SerializeField] private float detectionRadius = 5f;
    
    [Header("NavMesh Settings")]
    [Tooltip("Distance at which the enemy stops before reaching destination.")]
    [SerializeField] private float stoppingDistance = 1.2f;
    
    [Header("Abilities")]
    [Tooltip("Abilities assigned to this enemy. Drag ScriptableObject abilities here.")]
    [SerializeField] private List<AbilityBase> abilities = new List<AbilityBase>();
    
    [Header("Visual")]
    [Tooltip("Optional: Sprite or model preview for this enemy type.")]
    [SerializeField] private Sprite enemyIcon;
    
    // Public accessors
    public float MaxHP => maxHP;
    public float AttackDamage => attackDamage;
    public float AttackRadius => attackRadius;
    public float MoveSpeed => moveSpeed;
    public bool IsRanged => isRanged;
    public bool IsFlying => isFlying;
    public float AttackCooldown => attackCooldown;
    public TargetPriority TargetingPriority => targetingPriority;
    public float DetectionRadius => detectionRadius;
    public float StoppingDistance => stoppingDistance;
    public IReadOnlyList<AbilityBase> Abilities => abilities;
    public Sprite EnemyIcon => enemyIcon;
    
    /// Creates a new ability instance and assigns it to the specified enemy.
    /// Called by the Enemy component during initialization.
    /// <param name="enemy">The Enemy component to assign abilities to.</param>
    public void InitializeAbilities(Enemy enemy)
    {
        foreach (var ability in abilities)
        {
            if (ability != null)
            {
                // Create a unique instance of each ability for this enemy
                var abilityInstance = Instantiate(ability);
                abilityInstance.OnAssigned(enemy);
            }
        }
    }
    
    /// Called when the enemy attacks. Triggers all abilities with attack triggers.
    /// <param name="enemy">The Enemy component that attacked.</param>
    /// <param name="target">The Building component that was attacked.</param>
    public void TriggerAttackAbilities(Enemy enemy, Building target)
    {
        foreach (var ability in abilities)
        {
            ability?.OnAttack(enemy, target);
        }
    }
    

    /// Called when the enemy dies. Triggers all abilities with death triggers. Returns true if any ability prevented the death.
    /// <param name="enemy">The Enemy component that is dying.</param>
    /// <returns>True if death was prevented by an ability.</returns>
    public bool TriggerDeathAbilities(Enemy enemy)
    {
        bool deathPrevented = false;
        
        foreach (var ability in abilities)
        {
            if (ability != null && ability.OnDeath(enemy))
            {
                deathPrevented = true;
            }
        }
        
        return deathPrevented;
    }
    

    /// Called every frame to update passive abilities.
    /// <param name="enemy">The Enemy component to update.</param>
    public void UpdatePassiveAbilities(Enemy enemy)
    {
        foreach (var ability in abilities)
        {
            ability?.OnUpdate(enemy);
        }
    }
    
    /// Cleans up all abilities when the enemy is destroyed.
    public void CleanupAbilities()
    {
        foreach (var ability in abilities)
        {
            // Add
        }
    }
}