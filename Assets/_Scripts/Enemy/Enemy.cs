using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using UnityEngine.InputSystem.Utilities;
using System.ComponentModel;
using Unity.Collections;

public class Enemy : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyData data;
    public EnemyData Data => data;

    [Header("Live State")]
    [SerializeField] private Transform currentTarget;
    [SerializeField] private float currentHP;
    public float CurrentHP => currentHP;
    public bool isFlying;
    private float scaledMaxHP;
    private float scaledDamage;
    private float attackTimer;
    private float detectionTimer;

    private NavMeshAgent agent;
    private NavMeshPath path;
    private Transform heartTarget;
    private List<AbilityBase> instantiatedAbilities = new List<AbilityBase>();

    private const float DETECTION_INTERVAL = 0.5f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        path = new NavMeshPath();
        if (agent != null) { agent.updateRotation = false; agent.updateUpAxis = false; }
    }

    void Start()
    {
        if (data == null) return;
        scaledMaxHP = data.MaxHP;
        scaledDamage = data.AttackDamage;
        isFlying = data.IsFlying;
        currentHP = (scaledMaxHP > 0) ? scaledMaxHP : data.MaxHP;

        agent.speed = data.MoveSpeed;
        agent.stoppingDistance = data.AttackRadius * 0.9f;
        agent.autoBraking = false;
                    
        if (isFlying && agent != null) agent.enabled = false;
        //agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        

        heartTarget = GameObject.FindGameObjectWithTag("Heart")?.transform;
        currentTarget = heartTarget;

        agent.SetDestination(currentTarget.position);

        InitializeAbilities();
    }

    void Update()
    {
        if (data == null || heartTarget == null) return;

        HandleTimers();

        detectionTimer -= Time.deltaTime;
        if (detectionTimer <= 0)
        {
            detectionTimer = DETECTION_INTERVAL;
            PerformTargetingLogic();
        }

        HandleAction();
    }

    #region Pathfinding
    private void HandleTimers()
    {
        attackTimer -= Time.deltaTime;
        foreach (var ability in instantiatedAbilities) ability.OnUpdate(this);
    }

    private void PerformTargetingLogic()
    {
        // 1. Validate Target
        if (currentTarget == null) currentTarget = heartTarget;

        if (isFlying)
        {
            // Check for preferred targets only, otherwise go straight to heart
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, data.DetectionRadius);
            foreach (var col in hits)
                if (IsPreferredTarget(col.transform)) { currentTarget = col.transform; return; }

            currentTarget = heartTarget;
            return;
        }

        bool heartIsReachable = HasPath(heartTarget);

        // 2. RULE: If attacking a wall but a path to the Heart is clear, go to Heart
        if (currentTarget.CompareTag("Wall") && heartIsReachable)
        {
            currentTarget = heartTarget;
            return;
        }

        // 3. RULE: Preference Override (Check for preferred targets within Detection Radius)
        // This only triggers if we aren't already attacking something of high interest
        if (currentTarget == heartTarget || currentTarget.CompareTag("Wall"))
        {
            Collider2D[] potentialTargets = Physics2D.OverlapCircleAll(transform.position, data.DetectionRadius);
            foreach (var col in potentialTargets)
            {
                if (IsPreferredTarget(col.transform))
                {
                    currentTarget = col.transform;
                    return; 
                }
            }
        }

        // 4. RULE: Blockage logic (Only if trying to get to Heart and can't)
        if (!heartIsReachable && currentTarget == heartTarget)
        {
            DetermineBlockedTarget();
        }
    }

    private bool IsPreferredTarget(Transform t)
    {
        if (t == null || t == heartTarget) return false;

        // Compare tag to the priority setting in EnemyData
        switch (data.TargetingPriority)
        {
            case TargetPriority.Defense: return t.CompareTag("Defense");
            case TargetPriority.Resource: return t.CompareTag("Resource");
            case TargetPriority.WallOnly: return t.CompareTag("Wall");
            case TargetPriority.AnyNonWall: return t.CompareTag("Defense") || t.CompareTag("Resource");
            default: return false;
        }
    }

    private void DetermineBlockedTarget()
    {
        agent.CalculatePath(heartTarget.position, path);
        if (path.corners.Length < 2) return;

        // 1. Get blockage point (closest to heart)
        Vector3 blockedPointNearHeart = path.corners[path.corners.Length - 1];
        
        // 2. Find wall CLOSEST TO THAT BLOCKAGE POINT (not enemy)
        Transform targetWall = FindClosestWallToPoint(blockedPointNearHeart);
        
        if (targetWall != null)
        {
            // 3. Check for wall between enemy and target wall (closest to ENEMY)
            Transform immediateWall = FindImmediateWallObstacle(targetWall.position);
            currentTarget = immediateWall ?? targetWall; // Prefers immediate wall
        }
    }


    private void HandleAction()
    {
        if (currentTarget == null) 
        {
            if (isFlying) MoveDirectlyToward(heartTarget);
            agent.SetDestination(heartTarget.position);
            return;
        }

        if (IsTargetInAttackRange())
        {
            if (!isFlying && agent.hasPath) agent.ResetPath();
            TryAttack();
            return; //  Early return
        }

        if (isFlying)
        {
            MoveDirectlyToward(currentTarget);
            return;
        }

        // Test path to current target
        NavMeshPath testPath = new NavMeshPath();
        agent.CalculatePath(currentTarget.position, testPath);

        if (testPath.status == NavMeshPathStatus.PathComplete)
        {
            // Path clear - move normally
            if (Vector3.Distance(agent.destination, currentTarget.position) > 0.2f)
            {
                agent.SetDestination(currentTarget.position);
            }
        }
        else
        {
            // Path BLOCKED - IMMEDIATELY retarget wall closest to HEART
            if (currentTarget == heartTarget)
            {
                DetermineBlockedTarget(); // This already finds wall closest to heart
            }
            
            // Now try path to NEW target (wall)
            agent.CalculatePath(currentTarget.position, testPath);
            if (testPath.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetDestination(currentTarget.position);
            }
            // If even wall path is blocked, FindImmediateWallObstacle handles closest wall to enemy
        }
    }

    private void MoveDirectlyToward(Transform target)
    {
        if (target == null) return;
        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * data.MoveSpeed * Time.deltaTime;
    }


    private bool IsTargetInAttackRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, data.AttackRadius);
        foreach (var c in hits)
        {
            if (c.transform == currentTarget || c.transform.IsChildOf(currentTarget)) return true;
        }
        return false;
    }

    private Transform FindClosestWallToPoint(Vector3 point)
    {
        Collider2D[] walls = Physics2D.OverlapCircleAll(point, 3.0f);
        Transform best = null;
        float min = Mathf.Infinity;
        foreach (var w in walls)
        {
            if (w.CompareTag("Wall"))
            {
                float d = Vector2.Distance(point, w.transform.position);
                if (d < min) { min = d; best = w.transform; }
            }
        }
        return best;
    }

    private Transform FindImmediateWallObstacle(Vector3 targetWallPos)
    {
        Vector2 dir = (targetWallPos - transform.position).normalized;
        float dist = Vector2.Distance(transform.position, targetWallPos);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, dist);
        
        if (hit.collider != null && hit.transform.CompareTag("Wall") && hit.transform != currentTarget)
        {
            return hit.transform;
        }
        return null;
    }

    private bool HasPath(Transform t)
    {
        if (isFlying) return true;
        if (t == null || !agent.isOnNavMesh) return false;
        agent.CalculatePath(t.position, path);
        return path.status == NavMeshPathStatus.PathComplete;
    }

    private void TryAttack()
    {
        if (attackTimer > 0) return;
        Building b = currentTarget.GetComponentInParent<Building>();
        if (b != null)
        {
            float dmg = (scaledDamage > 0) ? scaledDamage : data.AttackDamage;
            b.TakeDamage(dmg);
            attackTimer = data.AttackCooldown;
            data.TriggerAttackAbilities(this, b);
        }
    }

    #endregion

    // --- Stats & Lifecycle ---
    public void TakeDamage(float damage)
    {
        currentHP -= damage;
        if (currentHP <= 0) DieLogic();
    }

    private void DieLogic()
    {
        bool deathPrevented = false;
        foreach (var ability in instantiatedAbilities)
        {
            if (ability != null && ability.OnDeath(this)) deathPrevented = true;
        }

        if (!deathPrevented)
        {
            foreach (var a in instantiatedAbilities) if (a != null) Destroy(a);
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

    public void Heal(float amount) => currentHP = Mathf.Min(currentHP + amount, scaledMaxHP);

    public void ModifySpeed(float mult, float dur) => StartCoroutine(SpeedModifierRoutine(mult, dur));

    private System.Collections.IEnumerator SpeedModifierRoutine(float multiplier, float duration)
    {
        if (agent == null) yield break;
        float original = agent.speed;
        agent.speed *= multiplier;
        yield return new WaitForSeconds(duration);
        agent.speed = original;
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
}