using UnityEngine;

/// Defines the contract that all abilities must implement.
/// This interface enables the enemy system to work with any ability without knowing the specific implementation details.
public interface IAbility
{
    /// The name displayed in UI and debugging.
    string AbilityName { get; }
    
    /// Cooldown time in seconds between ability uses.
    /// Return 0 for abilities with no cooldown.
    float Cooldown { get; }
    
    /// Returns true if the ability is ready to be executed.
    bool IsReady { get; }
    
    /// Executes the ability's effect.
    /// <param name="user">The Enemy component executing the ability.</param>
    void Execute(IEnemy user);
    
    /// Called when the ability is assigned to an enemy at spawn. Use for initialization that requires enemy context.
    /// <param name="user">The Enemy component this ability is assigned to.</param>
    void OnAssigned(IEnemy user);
    
    /// Called when the enemy dies. Return true if the ability handles death (revive, etc).
    /// <param name="user">The Enemy component that is dying.</param>
    /// <returns>True if death should be prevented, false otherwise.</returns>
    bool OnDeath(IEnemy user);
    
    /// Called every frame for passive abilities.
    /// <param name="user">The Enemy component this ability belongs to.</param>
    void OnUpdate(IEnemy user);
    

    /// Called when the enemy successfully attacks a target.
    /// <param name="user">The Enemy component executing the attack.</param>
    /// <param name="target">The Building component that was attacked.</param>
    void OnAttack(IEnemy user, Building target);
}
