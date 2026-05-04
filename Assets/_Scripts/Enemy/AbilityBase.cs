using UnityEngine;

public abstract class AbilityBase : ScriptableObject, IAbility
{
    [Header("Ability Information")]
    [SerializeField] protected string abilityName = "New Ability";
    [SerializeField] protected float cooldown = 2f;

    [Header("Trigger Settings")]
    [SerializeField] protected bool triggerOnSpawn;
    [SerializeField] protected bool triggerOnAttack;
    [SerializeField] protected bool triggerOnDeath;
    [SerializeField] protected bool preventDeath = false;
    [SerializeField] protected bool isPassive;
    [SerializeField] protected bool usableWhileStunned = true;

    private IEnemy assignedEnemy;
    private float lastUsedTime;
    private bool isInitialized;

    public string AbilityName => abilityName;
    public float Cooldown => cooldown;
    public bool IsReady => Time.time >= lastUsedTime + cooldown;
    public bool IsAssigned => assignedEnemy != null;
    public bool TriggerOnSpawn => triggerOnSpawn;
    public bool TriggerOnAttack => triggerOnAttack;
    public bool TriggerOnDeath => triggerOnDeath;
    public bool IsPassive => isPassive;

    // IEnemy instead of Enemy — works for both ground and flying
    protected IEnemy AssignedEnemy => assignedEnemy;

    // Subclasses implement this with IEnemy
    protected abstract void ExecuteAbility(IEnemy user);

    protected virtual void OnAbilityAssigned(IEnemy user)
    {
        assignedEnemy = user;
        isInitialized = true;
        lastUsedTime = -cooldown;
    }

    protected virtual void OnAbilityRemoved()
    {
        assignedEnemy = null;
        isInitialized = false;
    }

    public void Execute(IEnemy user)
    {
        if (!isInitialized)
        {
            Debug.LogWarning($"Ability {abilityName} has not been assigned yet.");
            return;
        }
        if (!IsReady) return;
        ExecuteAbility(user);
        lastUsedTime = Time.time;
    }

    public void OnAssigned(IEnemy user)
    {
        OnAbilityAssigned(user);
        if (triggerOnSpawn && IsReady)
            ExecuteAbility(user);
    }

    public bool OnDeath(IEnemy user)
    {
        if (!triggerOnDeath || !isInitialized) return false;
        if (IsReady)
        {
            ExecuteAbility(user);
            lastUsedTime = Time.time;
            return preventDeath;
        }
        return false;
    }

    public void OnUpdate(IEnemy user)
    {
        if (!isPassive || !isInitialized) return;
        if (IsReady)
        {
            ExecuteAbility(user);
            lastUsedTime = Time.time;
        }
    }

    public void OnAttack(IEnemy user, Building target)
    {
        if (!triggerOnAttack || !isInitialized) return;
        if (IsReady)
        {
            ExecuteAbility(user);
            lastUsedTime = Time.time;
        }
    }
}