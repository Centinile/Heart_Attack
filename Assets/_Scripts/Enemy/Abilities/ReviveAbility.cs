using UnityEngine;

[CreateAssetMenu(
    menuName = "Abilities/On Death/Revive",
    fileName = "ReviveOnce_",
    order = 202)]
public class ReviveAbility : AbilityBase
{
    [Header("Revive Settings")]
    [SerializeField] private float reviveHPPercent = 0.5f; // fraction of max HP to revive with
    [SerializeField] private GameObject reviveEffect;

    private bool _hasRevived = false;

    public ReviveAbility()
    {
        abilityName = "Revive Once";
        cooldown = 0f;
        triggerOnDeath = true;
        preventDeath = true;
    }

    protected override void OnAbilityAssigned(IEnemy user)
    {
        base.OnAbilityAssigned(user);
        _hasRevived = false;
    }

    // public override bool OnDeath(IEnemy user) // needs override, not base
    // {
    //     // Only revive once — after that let death proceed normally
    //     if (_hasRevived) return false;

    //     _hasRevived = true;

    //     // Restore HP via TakeDamage in reverse — set HP directly via the interface
    //     float reviveHP = user.Data.MaxHP * Mathf.Clamp01(reviveHPPercent);
    //     user.SetHP(reviveHP);

    //     if (reviveEffect != null)
    //         Object.Instantiate(reviveEffect, user.transform.position, Quaternion.identity);

    //     Debug.Log($"[ReviveAbility] {user.transform.name} revived at {reviveHP:0} HP");
    //     return true; // prevents death
    // }

    protected override void ExecuteAbility(IEnemy user) { }
}