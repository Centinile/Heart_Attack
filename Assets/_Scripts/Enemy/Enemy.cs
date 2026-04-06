using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class Enemy : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyData data;
    
    public EnemyData Data => data;
    
    [Header("State")]
    [SerializeField] private float currentHP;
    public float CurrentHP => currentHP;
    
    private float scaledMaxHP;
    private float scaledDamage;

    [SerializeField] private bool isAttacking;
    public bool IsAttacking => isAttacking;
    
    [Header("Components")]
    private NavMeshAgent agent;
    private Transform currentTarget;
    private Transform heartTarget;
    private float attackTimer;
    private NavMeshPath path;
    
    // Track instantiated abilities for cleanup
    private List<AbilityBase> instantiatedAbilities = new List<AbilityBase>();
    
    // Properties for external access
    public Transform CurrentTarget => currentTarget;
    public Transform HeartTarget => heartTarget;
    public float AttackTimer => attackTimer;
    public bool HasPathToTarget => HasPath(currentTarget);
    
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }
        
        path = new NavMeshPath();
    }
    
    void Start()
    {
        if (data == null) return;
                
                // If scaling wasn't applied, use defaults
        if (scaledMaxHP <= 0) 
        {
            scaledMaxHP = data.MaxHP;
            scaledDamage = data.AttackDamage;
            currentHP = scaledMaxHP;
        }
        
        // Initialize health
        currentHP = data.MaxHP;
        
        // Configure NavMesh agent
        if (agent != null)
        {
            agent.speed = data.MoveSpeed;
            agent.stoppingDistance = data.StoppingDistance;
        }
        
        // Find the heart/base target
        heartTarget = GameObject.FindGameObjectWithTag("Heart")?.transform;
        currentTarget = heartTarget;
        
        // Initialize abilities
        InitializeAbilities();
        
        Debug.Log($"Enemy spawned: {gameObject.name} with {instantiatedAbilities.Count} abilities");
    }
    
    void InitializeAbilities()
    {
        foreach (var ability in data.Abilities)
        {
            if (ability != null)
            {
                // Create a unique instance for this enemy
                var abilityInstance = Instantiate(ability);
                abilityInstance.OnAssigned(this);
                instantiatedAbilities.Add(abilityInstance);
            }
        }
    }
    
    void Update()
    {
        if (data == null) return;
        
        if (heartTarget == null) return;
        
        // Update attack timer
        attackTimer -= Time.deltaTime;
        
        // Update passive abilities
        data.UpdatePassiveAbilities(this);
        
        // Evaluate detection and targeting
        EvaluateDetection();
        
        // Ensure we have a valid target
        if (currentTarget == null)
            currentTarget = heartTarget;
        
        // Execute enemy behavior
        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);
        bool hasPath = HasPath(currentTarget);
        
        if (data.IsRanged)
        {
            HandleRangedBehavior(distanceToTarget, hasPath);
        }
        else
        {
            HandleMeleeBehavior(distanceToTarget, hasPath);
        }
    }

    public void ApplyScaling(float multiplier)
    {
        // Ensure we have the data reference
        if (data == null) return;

        scaledMaxHP = data.MaxHP * multiplier;
        scaledDamage = data.AttackDamage * multiplier;

        // Update current health to the new max
        currentHP = scaledMaxHP;
        
        Debug.Log($"{gameObject.name} scaled: HP {scaledMaxHP}, DMG {scaledDamage}");
    }
    
    void HandleRangedBehavior(float distanceToTarget, bool hasPath)
    {
        if (distanceToTarget <= data.AttackRadius)
        {
            agent.ResetPath();
            isAttacking = true;
            TryAttack();
        }
        else
        {
            isAttacking = false;
            
            if (hasPath)
            {
                agent.SetDestination(currentTarget.position);
            }
            else
            {
                HandleBlockedPathRanged(distanceToTarget);
            }
        }
    }
    
    void HandleMeleeBehavior(float distanceToTarget, bool hasPath)
    {
        if (!hasPath)
        {
            HandleBlockedPathMelee();
            return;
        }
        
        if (distanceToTarget <= data.AttackRadius)
        {
            agent.ResetPath();
            TryAttack();
        }
        else
        {
            agent.SetDestination(currentTarget.position);
        }
    }
    
    void EvaluateDetection()
    {
        if (isAttacking) return;
        
        // Find valid targets based on targeting priority
        Transform bestTarget = FindBestTarget();
        
        if (bestTarget != null)
            currentTarget = bestTarget;
        else
            currentTarget = heartTarget;
    }
    
    /// <summary>
    /// Finds the best target based on the enemy's targeting priority.
    /// </summary>
    private Transform FindBestTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, data.DetectionRadius);
        
        Transform bestTarget = null;
        float bestScore = Mathf.Infinity;
        
        foreach (Collider2D col in hits)
        {
            if (!IsValidTarget(col)) continue;
            
            float distToEnemy = Vector2.Distance(transform.position, col.transform.position);
            
            // Score based on targeting priority
            float score = GetTargetScore(col.transform, distToEnemy);
            
            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = col.transform;
            }
        }
        
        return bestTarget;
    }
    
    /// Calculates a score for a target based on targeting priority.
    /// Lower score = higher priority.
    private float GetTargetScore(Transform target, float distanceToEnemy)
    {
        switch (data.TargetingPriority)
        {
            case TargetPriority.WallOnly:
                // Only target walls, prioritize by distance to heart
                if (heartTarget != null)
                {
                    return Vector2.Distance(target.position, heartTarget.position);
                }
                return distanceToEnemy;
                
            case TargetPriority.Defense:
                // Only target defenses, prioritize by distance to heart
                if (heartTarget != null)
                {
                    return Vector2.Distance(target.position, heartTarget.position);
                }
                return distanceToEnemy;
                
            case TargetPriority.Resource:
                // Only target resources, prioritize by distance to heart
                if (heartTarget != null)
                {
                    return Vector2.Distance(target.position, heartTarget.position);
                }
                return distanceToEnemy;
                
            case TargetPriority.AnyNonWall:
                // Target defenses or resources, prioritize by distance to heart
                if (heartTarget != null)
                {
                    return Vector2.Distance(target.position, heartTarget.position);
                }
                return distanceToEnemy;
                
            case TargetPriority.None:
                return Mathf.Infinity;
                
            default:
                return distanceToEnemy;
        }
    }
    
    bool IsValidTarget(Collider2D col)
    {
        switch (data.TargetingPriority)
        {
            case TargetPriority.WallOnly:
                return col.CompareTag("Wall");
                
            case TargetPriority.Defense:
                return col.CompareTag("Defense");
                
            case TargetPriority.Resource:
                return col.CompareTag("Resource");
                
            case TargetPriority.AnyNonWall:
                return col.CompareTag("Defense") || col.CompareTag("Resource");
                
            case TargetPriority.None:
                return false;
        }
        
        return false;
    }
    
    bool HasPath(Transform target)
    {
        if (agent == null || target == null) return false;
        
        agent.CalculatePath(target.position, path);
        return path.status == NavMeshPathStatus.PathComplete;
    }
    
    void HandleBlockedPathRanged(float distanceToTarget)
    {
        if (distanceToTarget <= data.AttackRadius)
        {
            agent.ResetPath();
            TryAttack();
            return;
        }
        
        AttackClosestWallToTarget();
    }
    
    void HandleBlockedPathMelee()
    {
        AttackClosestWallToTarget();
    }
    
    void AttackClosestWallToTarget()
    {
        // Find all walls
        GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
        if (walls.Length == 0) return;
        
        Transform bestWall = null;
        float bestScore = Mathf.Infinity;
        
        foreach (GameObject wall in walls)
        {
            // First preference: closest to target (heart)
            float distToTarget = Vector2.Distance(wall.transform.position, heartTarget.position);
            
            if (distToTarget < bestScore)
            {
                // Check if path exists to this wall
                if (HasPath(wall.transform))
                {
                    bestScore = distToTarget;
                    bestWall = wall.transform;
                }
            }
        }
        
        // If no wall has path to target, fallback to closest to enemy
        if (bestWall == null)
        {
            float closestToEnemy = Mathf.Infinity;
            
            foreach (GameObject wall in walls)
            {
                float distToEnemy = Vector2.Distance(transform.position, wall.transform.position);
                
                if (distToEnemy < closestToEnemy)
                {
                    closestToEnemy = distToEnemy;
                    bestWall = wall.transform;
                }
            }
        }
        
        if (bestWall != null)
        {
            currentTarget = bestWall;
            agent.SetDestination(bestWall.position);
        }
    }
    
    void TryAttack()
    {
        if (attackTimer > 0f) return;
        Building building = currentTarget.GetComponent<Building>();
        
        if (building != null)
        {
            // USE THE SCALED DAMAGE HERE
            building.TakeDamage(scaledDamage);
            data.TriggerAttackAbilities(this, building);
            attackTimer = data.AttackCooldown;
        }
    }
    
    public void TakeDamage(float damage)
    {
        currentHP -= damage;
        
        if (currentHP <= 0)
        {
            // Trigger death abilities first
            bool deathPrevented = data.TriggerDeathAbilities(this);
            
            if (!deathPrevented)
            {
                Die();
            }
            else
            {
                // Reset HP if revived (optional: adjust as needed)
                currentHP = Data.MaxHP;
            }
        }
    }
    
    void Die()
    {
        // Cleanup abilities
        data.CleanupAbilities();
        
        Destroy(gameObject);
    }
    

    /// Heals the enemy by the specified amount.
    /// <param name="amount">Amount to heal.</param>
    public void Heal(float amount)
    {
        currentHP = Mathf.Min(currentHP + amount, data.MaxHP);
    }
    

    /// Modifies the enemy's movement speed temporarily.
    /// <param name="multiplier">Speed multiplier (1 = normal).</param>
    /// <param name="duration">Duration in seconds.</param>
    public void ModifySpeed(float multiplier, float duration)
    {
        StartCoroutine(SpeedModifierRoutine(multiplier, duration));
    }
    
    System.Collections.IEnumerator SpeedModifierRoutine(float multiplier, float duration)
    {
        if (agent == null) yield break;
        
        float originalSpeed = agent.speed;
        agent.speed = originalSpeed * multiplier;
        
        yield return new WaitForSeconds(duration);
        
        agent.speed = originalSpeed;
    }
    

    /// Returns the position of the enemy's attack point.
    /// Override in subclasses for custom attack origins.
    public virtual Vector3 GetAttackPosition()
    {
        return transform.position + transform.up * 0.5f;
    }
    
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Draw detection radius
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, data != null ? data.DetectionRadius : 5f);
        
        // Draw attack radius
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, data != null ? data.AttackRadius : 1.5f);
        
        // Draw line to current target
        if (currentTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
#endif
}