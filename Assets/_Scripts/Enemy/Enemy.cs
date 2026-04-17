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
    
    // --- PERSISTENCE FIX ---
    private Building lockedWall; 
    private bool isBreakingWall = false;
    private float attackTimer;
    private float detectionTimer;
    private const float DETECTION_INTERVAL = 0.6f; 

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null) 
        { 
            // Standard for 2D/Isometric NavMesh
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
    }

    // --- Core AI Logic ---

    private void DetermineTarget()
    {
        Building[] allBuildings = FindObjectsOfType<Building>();
        if (allBuildings.Length == 0) return;

        List<Building> candidates;
        if (data.TargetingPriority == TargetPriority.None)
            candidates = allBuildings.Where(b => b.StructureType == StructureType.Heart).ToList();
        else
        {
            candidates = allBuildings.Where(b => IsCorrectPriority(b)).ToList();
            if (candidates.Count == 0)
                candidates = allBuildings.Where(b => b.StructureType != StructureType.Wall).ToList();
        }

        if (candidates.Count == 0) return;

        // Rule of 3
        targetBuilding = candidates
            .OrderBy(b => Vector3.Distance(transform.position, b.transform.position))
            .Take(3)
            .Select(b => new { Building = b, PathDist = GetEffectiveDistance(b) })
            .OrderBy(x => x.PathDist)
            .First().Building;

        UpdatePathing();
    }

    private float GetEffectiveDistance(Building b)
    {
        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(b.transform.position, path);
        float d = GetPathLength(path);
        // Wall penalty
        if (path.status == NavMeshPathStatus.PathPartial && !data.IsFlying) d += 40f; 
        return d;
    }

    private void UpdatePathing()
    {
        if (targetBuilding == null) return;

        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(targetBuilding.transform.position, path);

        // 1. If we can reach the main target, clear any wall locks and go.
        if (path.status == NavMeshPathStatus.PathComplete || data.IsFlying)
        {
            UnlockWall();
            currentMoveTarget = targetBuilding.gameObject;
            agent.SetDestination(currentMoveTarget.transform.position);
        }
        else
        {
            // 2. If path is blocked, ONLY search for a wall if we don't have one locked.
            // This is the "First wall seen" logic.
            if (lockedWall == null || !lockedWall.IsAlive)
            {
                SearchForNearestWall();
            }
        }
    }

    private void SearchForNearestWall()
    {
        GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
        if (walls.Length == 0) return;

        GameObject closest = walls
            .OrderBy(w => Vector3.Distance(transform.position, w.transform.position))
            .FirstOrDefault();

        if (closest != null)
        {
            lockedWall = closest.GetComponent<Building>();
            currentMoveTarget = closest;
            isBreakingWall = true;
            agent.SetDestination(currentMoveTarget.transform.position);
        }
    }

    private void UnlockWall()
    {
        lockedWall = null;
        isBreakingWall = false;
    }

    private void HandleCombatState()
    {
        // Prioritize the locked wall over the building target
        GameObject activeTarget = (lockedWall != null && lockedWall.IsAlive) ? lockedWall.gameObject : currentMoveTarget;
        
        if (activeTarget == null) return;

        float dist = Vector3.Distance(transform.position, activeTarget.transform.position);
        
        // Walls in isometric often need a slightly higher range to account for the pivot
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
        if (path.corners.Length < 2) return Vector3.Distance(transform.position, targetBuilding.transform.position);
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
    }

    private void DieLogic()
    {
        bool deathPrevented = false;
        foreach (var a in instantiatedAbilities) if (a != null && a.OnDeath(this)) deathPrevented = true;
        if (!deathPrevented)
        {
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

    // --- Gizmos & Debugging ---
    private void OnDrawGizmosSelected()
    {
        if (data == null) return;

        // 1. Attack Range (Red)
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, data.AttackRadius);
        
        // 2. Detection/Targeting Range (Yellow)
        // Since the whole map is searched, we visualize the path refresh area
        Gizmos.color = new Color(1, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, 5f); // Visualization of local awareness

        // 3. Current Target Line
        if (currentMoveTarget != null)
        {
            Gizmos.color = isBreakingWall ? Color.magenta : Color.green;
            Gizmos.DrawLine(transform.position, currentMoveTarget.transform.position);
        }
    }
}