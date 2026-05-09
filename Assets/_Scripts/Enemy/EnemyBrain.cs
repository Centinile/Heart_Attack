using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;

public class EnemyBrain : MonoBehaviour, IEnemy
{
    [Header("References")]
    [SerializeField] private EnemyData data;
    [SerializeField] private EnemyAnimations enemyAnimations;
    [SerializeField] private HealthBarAnchor healthBar;
    public EnemyData Data => data;
    private EnemyMotor motor;

    [Header("Live State")]
    [SerializeField] private float currentHP;
    public float CurrentHP => currentHP;
    private float scaledMaxHP;
    private float scaledDamage;
    private List<AbilityBase> instantiatedAbilities = new List<AbilityBase>();

    private Building targetBuilding;
    private float attackTimer;
    private float detectionTimer;
    private const float DETECTION_INTERVAL = 0.6f;

    void Start()
    {
        motor = GetComponent<EnemyMotor>();
        enemyAnimations = GetComponent<EnemyAnimations>();
        InitializeStats();
        InitializeAbilities();
        DetermineTarget();
    }

    void Update()
    {
        foreach (var a in instantiatedAbilities) a?.OnUpdate(this);

        if (attackTimer > 0) attackTimer -= Time.deltaTime;

        // Always retarget if target is gone
        if (targetBuilding == null || !targetBuilding.IsAlive)
        {
            DetermineTarget();
            return;
        }

        // Detection + pathing tick — runs regardless of attack state
        detectionTimer -= Time.deltaTime;
        if (detectionTimer <= 0)
        {
            detectionTimer = DETECTION_INTERVAL;
            CheckDetectionRange();
            motor.UpdatePath(targetBuilding.transform.position);
        }

        HandleCombatState();

        // Sync animations
        Vector2 vel = motor.Agent.velocity;
        enemyAnimations?.PlayAnimation(vel);
        if (vel != Vector2.zero)
            enemyAnimations?.RotateToPointer(vel);
    }

    public void SetHP(float amount)
    {
        currentHP = Mathf.Clamp(amount, 0, scaledMaxHP);
        healthBar?.UpdateBar(currentHP, scaledMaxHP);
    }

    private void InitializeStats()
    {
        scaledMaxHP = data.MaxHP;
        scaledDamage = data.AttackDamage;
        currentHP = scaledMaxHP;
        motor.Agent.speed = data.MoveSpeed;
        motor.Agent.stoppingDistance = Mathf.Min(data.StoppingDistance, data.AttackRadius * 0.8f);
        healthBar?.Initialize(scaledMaxHP);
    }

    // --- Targeting ---

    private void CheckDetectionRange()
    {
        if (data.TargetingPriority == TargetPriority.None) return;

        // Already on a preferred target — don't switch
        if (targetBuilding != null && IsPreferredTarget(targetBuilding)) return;

        Building inRange = FindPreferredTargetInRange();
        if (inRange != null && inRange != targetBuilding)
            targetBuilding = inRange;
    }

    private Building FindPreferredTargetInRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position, data.DetectionRadius);

        Building best = null;
        float bestDist = Mathf.Infinity;

        foreach (var col in hits)
        {
            Building b = col.GetComponent<Building>();
            if (b == null || !b.IsAlive) continue;
            if (!IsPreferredTarget(b)) continue;

            float d = Vector3.Distance(transform.position, b.transform.position);
            if (d < bestDist) { bestDist = d; best = b; }
        }
        return best;
    }

    private void DetermineTarget()
    {
        Building[] allBuildings = FindObjectsOfType<Building>();
        if (allBuildings.Length == 0) return;

        List<Building> candidates;

        if (data.TargetingPriority == TargetPriority.None)
        {
            candidates = allBuildings
                .Where(b => b.IsAlive && b.StructureType == StructureType.Heart)
                .ToList();
        }
        else
        {
            candidates = allBuildings
                .Where(b => b.IsAlive && IsPreferredTarget(b))
                .ToList();

            if (candidates.Count == 0)
                candidates = allBuildings
                    .Where(b => b.IsAlive && b.StructureType == StructureType.Heart)
                    .ToList();
        }

        if (candidates.Count == 0) return;

        // Rule of 3
        targetBuilding = candidates
            .OrderBy(b => Vector3.Distance(transform.position, b.transform.position))
            .Take(3)
            .OrderBy(b => GetNavMeshDistance(b.transform.position))
            .First();
    }

    private bool IsPreferredTarget(Building b)
    {
        if (b == null || !b.IsAlive) return false;
        return data.TargetingPriority switch
        {
            TargetPriority.Defense    => b.StructureType == StructureType.Defense,
            TargetPriority.Resource   => b.StructureType == StructureType.Resource,
            TargetPriority.WallOnly   => b.StructureType == StructureType.Wall,
            TargetPriority.AnyNonWall => b.StructureType != StructureType.Wall
                                      && b.StructureType != StructureType.Heart,
            _ => false
        };
    }

    private float GetNavMeshDistance(Vector3 destination)
    {
        NavMeshPath path = new NavMeshPath();
        motor.Agent.CalculatePath(destination, path);

        if (path.corners.Length < 2)
            return Vector3.Distance(transform.position, destination);

        float dist = 0f;
        for (int i = 0; i < path.corners.Length - 1; i++)
            dist += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        return dist;
    }

    // --- Combat ---

    private void HandleCombatState()
    {
        // Wall blocking path takes combat priority
        bool wallBlocking = motor.IsPathBlocked
                         && motor.BlockedBy != null
                         && motor.BlockedBy.IsAlive;

        GameObject activeTarget = wallBlocking
            ? motor.BlockedBy.gameObject
            : targetBuilding.gameObject;

        if (activeTarget == null) return;

        // Use ClosestPoint for accurate distance on large colliders
        Collider2D col = activeTarget.GetComponent<Collider2D>();
        float dist = col != null
            ? Vector3.Distance(transform.position, col.ClosestPoint(transform.position))
            : Vector3.Distance(transform.position, activeTarget.transform.position);

        if (dist <= data.AttackRadius)
        {
            motor.Stop();
            PerformAttack(activeTarget);
        }
        else
        {
            motor.Resume();
            // Only drive movement here if not already managed by UpdatePath
            if (!motor.IsPathBlocked)
                motor.MoveToward(targetBuilding.transform.position);
        }
    }

    private void PerformAttack(GameObject target)
    {
        if (attackTimer > 0) return;

        enemyAnimations?.PlayAttackAnimation();

        if (target.TryGetComponent<Building>(out Building b))
        {
            float dmg = scaledDamage > 0 ? scaledDamage : data.AttackDamage;
            b.TakeDamage(dmg);
            data.TriggerAttackAbilities(this, b);

            if (!b.IsAlive)
                DetermineTarget();
        }

        attackTimer = data.AttackCooldown;
    }

    // --- Stats & Lifecycle ---

    public void TakeDamage(float damage)
    {
        currentHP -= damage;
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
            enemyAnimations?.PlayDeathAnimation();
            Destroy(gameObject);
        }
        else { currentHP = scaledMaxHP; }
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

    public void ModifySpeed(float mult, float dur)
    {
        StopCoroutine("SpeedModifierRoutine");
        StartCoroutine(SpeedModifierRoutine(mult, dur));
    }

    private System.Collections.IEnumerator SpeedModifierRoutine(float multiplier, float duration)
    {
        motor.ModifySpeed(multiplier);
        yield return new WaitForSeconds(duration);
        motor.ModifySpeed(1f / multiplier);
    }

    public void ApplyScaling(float multiplier)
    {
        scaledMaxHP = data.MaxHP * multiplier;
        scaledDamage = data.AttackDamage * multiplier;
        currentHP = scaledMaxHP;
    }

    private void OnDrawGizmosSelected()
    {
        if (data == null) return;

        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Gizmos.DrawWireSphere(transform.position, data.AttackRadius);

        Gizmos.color = new Color(1, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, data.DetectionRadius);

        if (motor != null && motor.IsPathBlocked && motor.BlockedBy != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Gizmos.DrawLine(transform.position, motor.BlockedBy.transform.position);
        }
    }
}