using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingEnemy : MonoBehaviour, IEnemy
{
    [Header("References")]
    [SerializeField] private EnemyData data;
    [SerializeField] private EnemyAnimations enemyAnimations;
    public EnemyData Data => data;

    [Header("Live State")]
    [SerializeField] private float currentHP;
    public float CurrentHP => currentHP;
    private float scaledMaxHP;
    private float scaledDamage;
    private List<AbilityBase> instantiatedAbilities = new List<AbilityBase>();

    [Header("Healthbar")]
    [SerializeField] private HealthBarAnchor healthBar;

    private Building targetBuilding;
    private float attackTimer;
    private bool isDead = false;

    private void Start()
    {
        InitializeStats();
        InitializeAbilities();
        DetermineTarget();
    }

    private void Update()
    {
        if (isDead) return;

        foreach (var a in instantiatedAbilities) a?.OnUpdate(this);

        // Retarget if current target is gone
        if (targetBuilding == null || !targetBuilding.IsAlive)
            DetermineTarget();

        if (targetBuilding == null) return;

        float dist = Vector2.Distance(transform.position, targetBuilding.transform.position);

        if (dist <= data.AttackRadius)
        {
            // In range — attack
            if (attackTimer <= 0f)
            {
                Attack();
                attackTimer = data.AttackCooldown;
            }
        }
        else
        {
            // Move directly toward target — no NavMesh, no wall avoidance
            Vector3 direction = (targetBuilding.transform.position - transform.position).normalized;
            transform.position += direction * data.MoveSpeed * Time.deltaTime;

            // Update animations and facing
            Vector2 velocity = direction;
            enemyAnimations?.PlayAnimation(velocity);
            enemyAnimations?.RotateToPointer(velocity);
        }

        if (attackTimer > 0f) attackTimer -= Time.deltaTime;
    }

    private void DetermineTarget()
    {
        // Flying enemies ignore walls entirely — target heart directly,
        // or fall back to priority targets if set
        Building[] allBuildings = FindObjectsOfType<Building>();
        if (allBuildings.Length == 0) return;

        Building heart = null;
        Building priorityTarget = null;
        float closestDist = Mathf.Infinity;

        foreach (var b in allBuildings)
        {
            if (!b.IsAlive) continue;

            if (b.StructureType == StructureType.Heart)
            {
                heart = b;
                continue;
            }

            // Skip walls — flying enemies fly over them
            if (b.StructureType == StructureType.Wall) continue;

            if (data.TargetingPriority != TargetPriority.None && IsCorrectPriority(b))
            {
                float d = Vector2.Distance(transform.position, b.transform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    priorityTarget = b;
                }
            }
        }

        targetBuilding = priorityTarget != null ? priorityTarget : heart;
    }

    private void Attack()
    {
        if (targetBuilding == null) return;

        enemyAnimations?.PlayAttackAnimation();
        targetBuilding.TakeDamage(scaledDamage > 0 ? scaledDamage : data.AttackDamage);
        data.TriggerAttackAbilities(this, targetBuilding);

        if (!targetBuilding.IsAlive)
            DetermineTarget();
    }

    private bool IsCorrectPriority(Building b)
    {
        return data.TargetingPriority switch
        {
            TargetPriority.Defense => b.StructureType == StructureType.Defense,
            TargetPriority.Resource => b.StructureType == StructureType.Resource,
            _ => b.StructureType != StructureType.Wall && b.StructureType != StructureType.Heart
        };
    }

    private void InitializeStats()
    {
        scaledMaxHP = data.MaxHP;
        scaledDamage = data.AttackDamage;
        currentHP = scaledMaxHP;
        healthBar?.Initialize(scaledMaxHP);
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        currentHP -= damage;
        enemyAnimations?.PlayHitAnimation();
        healthBar?.UpdateBar(currentHP, scaledMaxHP);
        if (currentHP <= 0) DieLogic();
    }

    private void DieLogic()
    {
        bool deathPrevented = false;
        foreach (var a in instantiatedAbilities)
            if (a != null && a.OnDeath(this)) deathPrevented = true;

        if (!deathPrevented)
        {
            healthBar?.ReturnBar();
            StopAllCoroutines();
            foreach (var a in instantiatedAbilities) if (a != null) Destroy(a);

            enemyAnimations.PlayDeathAnimation();

        }
        else
        {
            currentHP = scaledMaxHP;
        }
    }


    private void InitializeAbilities()
    {
        foreach (var a in data.Abilities)
        {
            if (a == null) continue;
            var instance = Instantiate(a);
            instance.OnAssigned(this);
            instantiatedAbilities.Add(instance);
        }
    }

    public void ApplyScaling(float multiplier)
    {
        scaledMaxHP = data.MaxHP * multiplier;
        scaledDamage = data.AttackDamage * multiplier;
        currentHP = scaledMaxHP;
    }

    public void ModifySpeed(float mult, float dur)
    {
        StartCoroutine(SpeedModifierRoutine(mult, dur));
    }

    private IEnumerator SpeedModifierRoutine(float multiplier, float duration)
    {
        float original = data.MoveSpeed;
        yield return new WaitForSeconds(duration);
        // Speed is read directly from data.MoveSpeed so modifying
        // scaledSpeed would require a field — add if needed
    }

    private void OnDrawGizmosSelected()
    {
        if (data == null) return;
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, data.AttackRadius);

        if (targetBuilding != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetBuilding.transform.position);
        }
    }
}