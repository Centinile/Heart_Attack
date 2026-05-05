using UnityEngine;

[CreateAssetMenu(
    menuName = "Abilities/On Attack/First Attack Multiplier",
    fileName = "FirstAttackMultiplier_",
    order = 500)]
public class FirstAttackMultiplierAbility : AbilityBase
{
    [Header("Multiplier Settings")]
    [SerializeField] private float damageMultiplier = 2f;

    private bool _hasAttacked = false;

    public FirstAttackMultiplierAbility()
    {
        abilityName = "First Attack Multiplier";
        cooldown = 0f;
        triggerOnAttack = true;
    }

    protected override void OnAbilityAssigned(IEnemy user)
    {
        base.OnAbilityAssigned(user);
        _hasAttacked = false;
    }

    protected override void ExecuteAbility(IEnemy user)
    {
        // Execution is intentionally empty — damage is applied via GetAndConsumeMultiplier
    }

    // Called by Enemy.Attack() before dealing damage
    public float GetAndConsumeMultiplier()
    {
        if (_hasAttacked) return 1f;
        _hasAttacked = true;
        Debug.Log($"[FirstAttackMultiplier] First attack — applying {damageMultiplier}x multiplier");
        return damageMultiplier;
    }
}