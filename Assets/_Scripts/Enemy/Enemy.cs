using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyData data;
    public EnemyData Data => data;

    [Header("Live State")]
    [SerializeField] private float currentHP;
    public float CurrentHP => currentHP;
    private float scaledMaxHP;
    private float scaledDamage;
    private List<AbilityBase> instantiatedAbilities = new List<AbilityBase>();

    [Header("Navigation & AI")]
    private NavMeshAgent agent;
    public Building targetBuilding;
    public GameObject currentMoveTarget;

    private Building lockedWall;
    private bool isBreakingWall = false;
    private float attackTimer;
    private float detectionTimer;
    private const float DETECTION_INTERVAL = 0.6f;

    private int pathClearFrames = 0;
    private const int PATH_CLEAR_FRAMES_REQUIRED = 3;

    [Header("Healthbar")]
    [SerializeField] private HealthBarAnchor healthBar;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }
    }

    void Start()
    {
        InitializeStats();
        InitializeAbilities();
        DetermineTarget();
    }

    void Update()
    {
        foreach (var a in instantiatedAbilities) a?.OnUpdate(this);

        if (targetBuilding == null || !targetBuilding.IsAlive)
        {
            UnlockWall();
            DetermineTarget();
            return;
        }

        HandleCombatState();

        if (attackTimer > 0) attackTimer -= Time.deltaTime;

        detectionTimer -= Time.deltaTime;
        if (detectionTimer <= 0)
        {
            detectionTimer = DETECTION_INTERVAL;

            // Check detection range for preferred targets, but not while breaking a wall
            if (data.TargetingPriority != TargetPriority.None && !isBreakingWall)
            {
                Building inRange = FindPreferredTargetInRange();
                if (inRange != null && inRange != targetBuilding)
                {
                    targetBuilding = inRange;
                    UpdatePathing();
                    return;
                }
            }

            UpdatePathing();
        }
    }

    private void InitializeStats()
    {
        scaledMaxHP = data.MaxHP;
        scaledDamage = data.AttackDamage;
        currentHP = scaledMaxHP;
        agent.speed = data.MoveSpeed;
        agent.stoppingDistance = data.StoppingDistance;
        healthBar?.Initialize(scaledMaxHP);
    }

    // --- Core AI Logic ---

    private void DetermineTarget()
    {
        Building[] allBuildings = FindObjectsOfType<Building>();
        if (allBuildings.Length == 0) return;

        List<Building> candidates;

        if (data.TargetingPriority == TargetPriority.None)
        {
            // No priority — always target the heart
            candidates = allBuildings
                .Where(b => b.StructureType == StructureType.Heart).ToList();
        }
        else
        {
            // Has priority — look for preferred targets first
            candidates = allBuildings.Where(b => IsCorrectPriority(b)).ToList();

            // No preferred targets exist anywhere — fall back to heart
            if (candidates.Count == 0)
                candidates = allBuildings
                    .Where(b => b.StructureType == StructureType.Heart).ToList();
        }

        if (candidates.Count == 0) return;

        // Rule of 3: take 3 closest by straight-line, pick shortest NavMesh path
        targetBuilding = candidates
            .OrderBy(b => Vector3.Distance(transform.position, b.transform.position))
            .Take(3)
            .Select(b => new { Building = b, PathDist = GetEffectiveDistance(b) })
            .OrderBy(x => x.PathDist)
            .First().Building;

        UpdatePathing();
    }

    // Scan detection radius for a live building matching targeting priority
    private Building FindPreferredTargetInRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, data.DetectionRadius);
        Building best = null;
        float bestDist = Mathf.Infinity;

        foreach (var col in hits)
        {
            Building b = col.GetComponent<Building>();
            if (b == null || !b.IsAlive) continue;
            if (!IsCorrectPriority(b)) continue;

            float d = Vector3.Distance(transform.position, b.transform.position);
            if (d < bestDist) { bestDist = d; best = b; }
        }
        return best;
    }

    private float GetEffectiveDistance(Building b)
    {
        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(b.transform.position, path);
        float d = GetPathLength(path);
        // Penalize partial paths heavily so fully reachable targets are preferred
        if (path.status == NavMeshPathStatus.PathPartial) d += 40f;
        return d;
    }

    private void UpdatePathing()
    {
        if (targetBuilding == null) return;

        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(targetBuilding.transform.position, path);

        if (path.status == NavMeshPathStatus.PathComplete)
        {
            // Require several consecutive clear frames before trusting the path
            // is genuinely open — prevents NavMesh flicker near wall seams
            if (isBreakingWall && lockedWall != null && lockedWall.IsAlive)
            {
                pathClearFrames++;
                if (pathClearFrames < PATH_CLEAR_FRAMES_REQUIRED)
                    return;
            }

            pathClearFrames = 0;
            UnlockWall();
            currentMoveTarget = targetBuilding.gameObject;
            agent.SetDestination(currentMoveTarget.transform.position);
        }
        else
        {
            pathClearFrames = 0;

            // Only pick a new wall if we don't already have one locked —
            // once committed, stay on it until it dies
            if (lockedWall == null || !lockedWall.IsAlive)
                SearchForNearestWall();
        }
    }

    private void SearchForNearestWall()
    {
        GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
        if (walls.Length == 0) return;

        // Use NavMesh path distance for stable wall selection
        GameObject closest = walls
            .Select(w => new { Wall = w, Dist = GetNavMeshDistanceTo(w.transform.position) })
            .OrderBy(x => x.Dist)
            .Select(x => x.Wall)
            .FirstOrDefault();

        if (closest != null)
        {
            lockedWall = closest.GetComponent<Building>();
            currentMoveTarget = closest;
            isBreakingWall = true;
            agent.SetDestination(currentMoveTarget.transform.position);
        }
    }

    private float GetNavMeshDistanceTo(Vector3 destination)
    {
        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(destination, path);

        if (path.corners.Length < 2)
            return Vector3.Distance(transform.position, destination);

        float dist = 0f;
        for (int i = 0; i < path.corners.Length - 1; i++)
            dist += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        return dist;
    }

    private void UnlockWall()
    {
        lockedWall = null;
        isBreakingWall = false;
        pathClearFrames = 0;
    }

    private void HandleCombatState()
    {
        GameObject activeTarget = (lockedWall != null && lockedWall.IsAlive)
            ? lockedWall.gameObject
            : currentMoveTarget;

        if (activeTarget == null) return;

        float dist = Vector3.Distance(transform.position, activeTarget.transform.position);
        float range = isBreakingWall ? agent.stoppingDistance + 0.4f : data.AttackRadius;

        if (dist <= range)
        {
            agent.isStopped = true;
            if (attackTimer <= 0)
            {
                Attack(activeTarget);
                attackTimer = data.AttackCooldown;
            }
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(activeTarget.transform.position);
        }
    }

    private void Attack(GameObject target)
    {
        if (target.TryGetComponent<Building>(out Building b))
        {
            b.TakeDamage(scaledDamage > 0 ? scaledDamage : data.AttackDamage);
            data.TriggerAttackAbilities(this, b);

            if (!b.IsAlive)
            {
                UnlockWall();
                DetermineTarget();
            }
        }
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

    private float GetPathLength(NavMeshPath path)
    {
        if (path.corners.Length < 2)
            return Vector3.Distance(transform.position, targetBuilding.transform.position);

        float dist = 0f;
        for (int i = 0; i < path.corners.Length - 1; i++)
            dist += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        return dist;
    }

    // --- Stats & Lifecycle ---

    public void TakeDamage(float damage)
    {
        currentHP -= damage;
        if (currentHP <= 0) DieLogic();
        healthBar?.UpdateBar(currentHP, scaledMaxHP);
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
        agent.speed = data.MoveSpeed * multiplier;
        yield return new WaitForSeconds(duration);
        agent.speed = data.MoveSpeed;
    }

    public void ApplyScaling(float multiplier)
    {
        scaledMaxHP = data.MaxHP * multiplier;
        scaledDamage = data.AttackDamage * multiplier;
        currentHP = scaledMaxHP;
    }

    // --- Gizmos ---

    private void OnDrawGizmosSelected()
    {
        if (data == null) return;

        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, data.AttackRadius);

        Gizmos.color = new Color(1, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, data.DetectionRadius);

        if (currentMoveTarget != null)
        {
            Gizmos.color = isBreakingWall ? Color.magenta : Color.green;
            Gizmos.DrawLine(transform.position, currentMoveTarget.transform.position);
        }
    }
}