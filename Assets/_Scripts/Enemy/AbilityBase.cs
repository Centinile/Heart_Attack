using UnityEngine;

/// Abstract base class for all ScriptableObject-based abilities.
/// Provides common functionality, cooldown management, and lifecycle hooks.
/// Inherit from this class to create concrete abilities that can be created as assets in the Unity editor.
public abstract class AbilityBase : ScriptableObject, IAbility
{
    [Header("Ability Information")]
    [Tooltip("The display name shown in UI and debugging.")]
    [SerializeField] protected string abilityName = "New Ability";
    
    [Tooltip("Cooldown time in seconds between uses. Set to 0 for no cooldown.")]
    [SerializeField] protected float cooldown = 2f;
    
    [Header("Trigger Settings")]
    [Tooltip("If true, this ability triggers when the enemy spawns.")]
    [SerializeField] protected bool triggerOnSpawn;
    
    [Tooltip("If true, this ability triggers when the enemy attacks.")]
    [SerializeField] protected bool triggerOnAttack;
    
    [Tooltip("If true, this ability triggers when the enemy dies.")]
    [SerializeField] protected bool triggerOnDeath;
    
    [Tooltip("If true, this ability runs every frame (passive ability).")]
    [SerializeField] protected bool isPassive;
    
    [Tooltip("If true, ability can be used while the enemy is stunned or disabled.")]
    [SerializeField] protected bool usableWhileStunned = true;
    
    // Internal state tracking
    private Enemy assignedEnemy;
    private float lastUsedTime;
    private bool isInitialized;
    

    /// Gets the display name of the ability.
    public string AbilityName => abilityName;

    /// Gets the cooldown time in seconds.
    public float Cooldown => cooldown;
    
    /// Gets whether the ability is ready to be used based on cooldown.
    public bool IsReady => Time.time >= lastUsedTime + cooldown;
    

    /// Gets whether the ability is currently assigned to an enemy.
    public bool IsAssigned => assignedEnemy != null;
    

    /// Gets whether this ability triggers on spawn.

    public bool TriggerOnSpawn => triggerOnSpawn;
    
    /// Gets whether this ability triggers on attack.
    public bool TriggerOnAttack => triggerOnAttack;
    
    /// Gets whether this ability triggers on death.
    public bool TriggerOnDeath => triggerOnDeath;
    
    /// Gets whether this is a passive ability.
    public bool IsPassive => isPassive;
    

    /// Gets the Enemy component this ability is assigned to.
    protected Enemy AssignedEnemy => assignedEnemy;
    

    /// Abstract method that subclasses must override to define the ability's behavior.
    /// <param name="user">The Enemy executing the ability.</param>
    protected abstract void ExecuteAbility(Enemy user);
    

    /// Virtual method for subclass initialization when assigned to an enemy.
    /// <param name="user">The Enemy this ability is assigned to.</param>
    protected virtual void OnAbilityAssigned(Enemy user)
    {
        assignedEnemy = user;
        isInitialized = true;
        lastUsedTime = -cooldown; // Allow immediate first use
    }
    
    /// Virtual method for subclass cleanup when removed from an enemy.
    protected virtual void OnAbilityRemoved()
    {
        assignedEnemy = null;
        isInitialized = false;
    }
    
    #region IAbility Implementation
    
    /// <inheritdoc/>
    public void Execute(Enemy user)
    {
        if (!isInitialized)
        {
            Debug.LogWarning($"Ability {abilityName} has not been assigned to an enemy yet!");
            return;
        }
        
        if (!IsReady)
        {
            return;
        }
        
        ExecuteAbility(user);
        lastUsedTime = Time.time;
    }
    
    /// <inheritdoc/>
    public void OnAssigned(Enemy user)
    {
        OnAbilityAssigned(user);
        
        // Trigger immediately if this is a spawn-triggered ability
        if (triggerOnSpawn && IsReady)
        {
            ExecuteAbility(user);
        }
    }
    
    /// <inheritdoc/>
    public bool OnDeath(Enemy user)
    {
        if (!triggerOnDeath || !isInitialized)
            return false;
        
        if (IsReady)
        {
            ExecuteAbility(user);
            lastUsedTime = Time.time;
            return true; // Ability handled death (could revive, etc.)
        }
        
        return false;
    }
    
    /// <inheritdoc/>
    public void OnUpdate(Enemy user)
    {
        if (!isPassive || !isInitialized) return;
            
            // Passives should still respect their tick rate (cooldown)
            if (IsReady) 
            {
                ExecuteAbility(user);
                lastUsedTime = Time.time;
            }
    }
    
    /// <inheritdoc/>
    public void OnAttack(Enemy user, Building target)
    {
        if (!triggerOnAttack || !isInitialized)
            return;
        
        if (IsReady)
        {
            ExecuteAbility(user);
            lastUsedTime = Time.time;
        }
    }
    
    #endregion
    
    #region Editor Menu Support
    
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Ability Base", priority = 0)]
    private static void CreateAbilityBase()
    {
        string path = UnityEditor.EditorUtility.SaveFilePanelInProject(
            "Save Ability",
            "NewAbility",
            "asset",
            "Choose where to save the ability");
        
        if (!string.IsNullOrEmpty(path))
        {
            var ability = CreateInstance<AbilityBase>();
            UnityEditor.AssetDatabase.CreateAsset(ability, path);
            UnityEditor.AssetDatabase.Refresh();
        }
    }
#endif
    
    #endregion
}