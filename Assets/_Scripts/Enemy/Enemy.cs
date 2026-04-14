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
        if (data == null || heartTarget == null) return;

        attackTimer -= Time.deltaTime;
        foreach (var ability in instantiatedAbilities)
        {
            ability.OnUpdate(this);
        }
        EvaluateDetection();

        if (currentTarget == null) currentTarget = heartTarget;

        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);
        
        // check path status every frame to handle walls appearing
        bool hasPath = HasPath(currentTarget);

        if (data.IsRanged)
            HandleRangedBehavior(distanceToTarget, hasPath);
        else
            HandleMeleeBehavior(distanceToTarget, hasPath);
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
        // 1. Always check if our current target's COLLIDER is within our attack radius
        if (currentTarget != null && IsTargetInPhysicalRange())
        {
            agent.ResetPath();
            isAttacking = true;
            TryAttack();
            return; // Exit so we don't try to move
        }

        // 2. If not in range, move toward the target
        isAttacking = false;
        
        if (!hasPath)
        {
            HandleBlockedPathMelee();
        }
        else
        {
            agent.SetDestination(currentTarget.position);
        }
    }

    private bool IsTargetInPhysicalRange()
    {
        if (currentTarget == null) return false;

        // We check for any colliders within the attack radius
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, data.AttackRadius);
        
        foreach (var hit in hitColliders)
        {
            // If one of the colliders we are touching is our target, we are in range!
            if (hit.transform == currentTarget || hit.transform.IsChildOf(currentTarget) || currentTarget.IsChildOf(hit.transform))
            {
                return true;
            }
        }
        return false;
    }
    
    void EvaluateDetection()
    {
        if (currentTarget != null && currentTarget.CompareTag("Wall") && !HasPath(heartTarget)) 
        return;

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
        // If the path is blocked, ANY wall in front of us is a valid target 
        // to clear the path, regardless of our "Priority" settings.
        

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
                // If priority is none, they only care about the Heart, 
                // but the "Wall" check above still lets them break obstacles.
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
        if (attackTimer > 0f || currentTarget == null) return;

        // Use GetComponentInParent to ensure we grab the Building script 
        // regardless of whether the collider is on a child or the root
        Building targetBuilding = currentTarget.GetComponentInParent<Building>();

        if (targetBuilding != null)
        {
            float damageToApply = (scaledDamage > 0) ? scaledDamage : data.AttackDamage;
            
            targetBuilding.TakeDamage(damageToApply);
            
            if (data != null)
                data.TriggerAttackAbilities(this, targetBuilding);
                
            attackTimer = data.AttackCooldown;
            Debug.Log($"<color=green>[SUCCESS]</color> Attacking {currentTarget.name}. Wall/Tower is within physical radius.");
        }
        else
        {
            // Fallback: If target is destroyed or missing script, reset
            isAttacking = false;
            currentTarget = heartTarget;
        }
    }
    
    public void TakeDamage(float damage)
    {
        currentHP -= damage;
        
        if (currentHP <= 0)
        {
            // 1. Check the LIVE instances, not the ScriptableObject Asset
            bool deathPrevented = false;
            
            foreach (var ability in instantiatedAbilities)
            {
                // This calls OnDeath on the actual instance (e.g., DeathExplosionAbility)
                if (ability != null && ability.OnDeath(this))
                {
                    deathPrevented = true;
                }
            }
            
            if (!deathPrevented)
            {
                Die();
            }
            else
            {
                // Optional: Reset health if an ability (like a Revive) prevented death
                currentHP = (scaledMaxHP > 0) ? scaledMaxHP : data.MaxHP;
            }
        }
    }
    
    void Die()
    {
        // 2. Cleanup: Important to destroy the instances to stop sounds/particles
        foreach (var ability in instantiatedAbilities)
        {
            if (ability != null) Destroy(ability);
        }
        instantiatedAbilities.Clear();
        
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