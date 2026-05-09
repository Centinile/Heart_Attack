// using UnityEngine;
// using UnityEngine.AI;
// using System.Collections.Generic;
// using System.Linq;
// using System.Collections;

// [RequireComponent(typeof(NavMeshAgent))]
// public class EnemyBrain : MonoBehaviour, IEnemy
// {
//     [Header("References")]
//     [SerializeField] private EnemyData data;
//     [SerializeField] private EnemyAnimations enemyAnimations;
//     public EnemyData Data => data;

//     [Header("Live State")]
//     [SerializeField] private float currentHP;
//     public float CurrentHP => currentHP;
//     private float scaledMaxHP;
//     private float scaledDamage;
//     private List<AbilityBase> instantiatedAbilities = new List<AbilityBase>();

//     [Header("Navigation & AI")]
//     private NavMeshAgent agent;
//     public Building targetBuilding;
//     public GameObject currentMoveTarget;

//     private Building lockedWall;
//     private bool isBreakingWall = false;
//     private float attackTimer;
//     private float detectionTimer;
//     private const float DETECTION_INTERVAL = 0.6f;

//     private int pathClearFrames = 0;
//     private const int PATH_CLEAR_THRESHOLD = 5;

//     [Header("Healthbar")]
//     [SerializeField] private HealthBarAnchor healthBar;

//     void Awake()
//     {
//         agent = GetComponent<NavMeshAgent>();
//         enemyAnimations = GetComponent<EnemyAnimations>();
//         if (agent != null)
//         {
//             agent.updateRotation = false;
//             agent.updateUpAxis = false;
//         }
//     }

//     void Start()
//     {
//         InitializeStats();
//         InitializeAbilities();
//         DetermineTarget();
//     }

//     public void SetHP(float amount)
//     {
//         currentHP = Mathf.Clamp(amount, 0, scaledMaxHP);
//         healthBar?.UpdateBar(currentHP, scaledMaxHP);
//     }

//     void Update()
//     {
//         // 1. Handle Animations & Abilities
//         foreach (var a in instantiatedAbilities) a?.OnUpdate(this);

//         Vector2 velocity = new Vector2(agent.velocity.x, agent.velocity.y);
//         enemyAnimations?.PlayAnimation(velocity);
//         if (velocity != Vector2.zero)
//             enemyAnimations?.RotateToPointer(velocity);

//         // 2. Refresh Timers
//         if (attackTimer > 0) attackTimer -= Time.deltaTime;
//         detectionTimer -= Time.deltaTime;

//         // 3. Target Validation
//         if (targetBuilding == null || !targetBuilding.IsAlive)
//         {
//             UnlockWall();
//             DetermineTarget();
//             return;
//         }

//         // 4. Pathing Logic (Every interval)
//         if (detectionTimer <= 0)
//         {
//             detectionTimer = DETECTION_INTERVAL;
//             CheckPathStatus();
//         }

//         // 5. Combat Logic (ALWAYS RUN THIS)
//         HandleCombatState();
//     }

//     private void CheckPathStatus()
//     {
//         NavMeshPath path = new NavMeshPath();
//         agent.CalculatePath(targetBuilding.transform.position, path);

//         if (path.status == NavMeshPathStatus.PathComplete)
//         {
//             // If we were breaking a wall, we need to "confirm" the path is really open
//             pathClearFrames++;
//             if (pathClearFrames >= PATH_CLEAR_THRESHOLD)
//             {
//                 UnlockWall();
//                 currentMoveTarget = targetBuilding.gameObject;
//                 agent.SetDestination(currentMoveTarget.transform.position);
//             }
//         }
//         else
//         {
//             // Path is blocked
//             pathClearFrames = 0;
//             if (lockedWall == null || !lockedWall.IsAlive)
//             {
//                 SearchForNearestWall();
//             }
//         }
//     }

//     private void InitializeStats()
//     {
//         scaledMaxHP = data.MaxHP;
//         scaledDamage = data.AttackDamage;
//         currentHP = scaledMaxHP;
//         agent.speed = data.MoveSpeed;
//         agent.stoppingDistance = data.StoppingDistance;
//         healthBar?.Initialize(scaledMaxHP);
//     }

//     // --- Core AI Logic ---

//     private void DetermineTarget()
//     {
//         Building[] allBuildings = FindObjectsOfType<Building>();
//         if (allBuildings.Length == 0) return;

//         List<Building> candidates;

//         if (data.TargetingPriority == TargetPriority.None)
//         {
//             // No priority — always target the heart
//             candidates = allBuildings
//                 .Where(b => b.StructureType == StructureType.Heart).ToList();
//         }
//         else
//         {
//             // Has priority — look for preferred targets first
//             candidates = allBuildings.Where(b => IsCorrectPriority(b)).ToList();

//             // No preferred targets exist anywhere — fall back to heart
//             if (candidates.Count == 0)
//                 candidates = allBuildings
//                     .Where(b => b.StructureType == StructureType.Heart).ToList();
//         }

//         if (candidates.Count == 0) return;

//         // Rule of 3: take 3 closest by straight-line, pick shortest NavMesh path
//         targetBuilding = candidates
//             .OrderBy(b => Vector3.Distance(transform.position, b.transform.position))
//             .Take(3)
//             .Select(b => new { Building = b, PathDist = GetEffectiveDistance(b) })
//             .OrderBy(x => x.PathDist)
//             .First().Building;

//         UpdatePathing();
//     }

//     // Scan detection radius for a live building matching targeting priority
//     private Building FindPreferredTargetInRange()
//     {
//         Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, data.DetectionRadius);
//         Building best = null;
//         float bestDist = Mathf.Infinity;

//         foreach (var col in hits)
//         {
//             Building b = col.GetComponent<Building>();
//             if (b == null || !b.IsAlive) continue;
//             if (!IsCorrectPriority(b)) continue;

//             float d = Vector3.Distance(transform.position, b.transform.position);
//             if (d < bestDist) { bestDist = d; best = b; }
//         }
//         return best;
//     }

//     private float GetEffectiveDistance(Building b)
//     {
//         NavMeshPath path = new NavMeshPath();
//         agent.CalculatePath(b.transform.position, path);
//         float d = GetPathLength(path, b); // pass b directly
//         if (path.status == NavMeshPathStatus.PathPartial) d += 40f;
//         return d;
//     }

//     private void UpdatePathing()
//     {
//         if (targetBuilding == null) return;

//         NavMeshPath path = new NavMeshPath();
//         agent.CalculatePath(targetBuilding.transform.position, path);

//         if (path.status == NavMeshPathStatus.PathComplete)
//         {
//             if (isBreakingWall && lockedWall != null && lockedWall.IsAlive)
//             {
//                 pathClearFrames++;
//                 if (pathClearFrames < PATH_CLEAR_THRESHOLD)
//                     return; // FIX 2: Don't call SetDestination mid-confirmation
//             }

//             pathClearFrames = 0;
//             UnlockWall();
//             currentMoveTarget = targetBuilding.gameObject;
//             agent.isStopped = false; // FIX 3: Ensure agent isn't stopped from combat state
//             agent.SetDestination(currentMoveTarget.transform.position);
//         }
//         else
//         {
//             pathClearFrames = 0;

//             if (lockedWall == null || !lockedWall.IsAlive)
//                 SearchForNearestWall();
//             // FIX 4: If wall is already locked and alive, do nothing — don't re-search
//         }
//     }

//     private void SearchForNearestWall()
//     {
//         GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
//         if (walls.Length == 0) return;

//         // Find the wall that is physically closest to the path blockage
//         GameObject closest = walls
//             .Select(w => new { Wall = w, Dist = Vector3.Distance(transform.position, w.transform.position) })
//             .OrderBy(x => x.Dist)
//             .FirstOrDefault()?.Wall;

//         if (closest != null)
//         {
//             lockedWall = closest.GetComponent<Building>();
//             currentMoveTarget = closest;
//             isBreakingWall = true;
//             pathClearFrames = 0;
//             agent.SetDestination(closest.transform.position);
//         }
//     }

//     private float GetNavMeshDistanceTo(Vector3 destination)
//     {
//         NavMeshPath path = new NavMeshPath();
//         agent.CalculatePath(destination, path);

//         if (path.corners.Length < 2)
//             return Vector3.Distance(transform.position, destination);

//         float dist = 0f;
//         for (int i = 0; i < path.corners.Length - 1; i++)
//             dist += Vector3.Distance(path.corners[i], path.corners[i + 1]);
//         return dist;
//     }

//     private void UnlockWall()
//     {
//         lockedWall = null;
//         isBreakingWall = false;
//         pathClearFrames = 0;
//     }

// private void HandleCombatState()
//     {
//         // Determine what we are currently trying to kill
//         GameObject activeTarget = isBreakingWall ? (lockedWall != null ? lockedWall.gameObject : null) : currentMoveTarget;
        
//         if (activeTarget == null) return;

//         float dist = Vector3.Distance(transform.position, activeTarget.transform.position);
        
//         // Use a slightly larger buffer for walls to prevent "dancing" at the edge of range
//         float range = isBreakingWall ? agent.stoppingDistance + 0.5f : data.AttackRadius;

//         if (dist <= range)
//         {
//             agent.isStopped = true;
//             if (attackTimer <= 0)
//             {
//                 Attack(activeTarget);
//                 attackTimer = data.AttackCooldown;
//             }
//         }
//         else
//         {
//             agent.isStopped = false;
//             // Only update destination if it's different to save performance
//             if (agent.destination != activeTarget.transform.position)
//             {
//                 agent.SetDestination(activeTarget.transform.position);
//             }
//         }
//     }

//     private void Attack(GameObject target)
//     {
//         enemyAnimations?.PlayAttackAnimation();

//         if (data.IsRanged && data.ProjectileData != null)
//         {
//             FireProjectile(target);
//             return;
//         }

//         // Melee — direct damage
//         if (target.TryGetComponent<Building>(out Building b))
//         {
//             float damage = scaledDamage > 0 ? scaledDamage : data.AttackDamage;

//             foreach (var a in instantiatedAbilities)
//             {
//                 if (a is FirstAttackMultiplierAbility firstHit)
//                 {
//                     damage *= firstHit.GetAndConsumeMultiplier();
//                     break;
//                 }
//             }

//             b.TakeDamage(damage);
//             data.TriggerAttackAbilities(this, b);

//             if (!b.IsAlive)
//             {
//                 UnlockWall();
//                 CheckPathStatus();
//             }
//         }
//     }

//     private void FireProjectile(GameObject target)
//     {
//         if (data.ProjectileData.Prefab == null) return;

//         GameObject projObj = Instantiate(
//             data.ProjectileData.Prefab,
//             transform.position,
//             Quaternion.identity);

//         if (projObj.TryGetComponent(out Projectile proj))
//         {
//             float damage = scaledDamage > 0 ? scaledDamage : data.AttackDamage;

//             foreach (var a in instantiatedAbilities)
//             {
//                 if (a is FirstAttackMultiplierAbility firstHit)
//                 {
//                     damage *= firstHit.GetAndConsumeMultiplier();
//                     break;
//                 }
//             }

//             proj.InitializeFromEnemy(target.transform, damage);
//         }
//     }

//     private bool IsCorrectPriority(Building b)
//     {
//         return data.TargetingPriority switch
//         {
//             TargetPriority.Defense => b.StructureType == StructureType.Defense,
//             TargetPriority.Resource => b.StructureType == StructureType.Resource,
//             _ => b.StructureType != StructureType.Wall && b.StructureType != StructureType.Heart
//         };
//     }

//     private float GetPathLength(NavMeshPath path, Building b)
//     {
//         if (path.corners.Length < 2)
//             return Vector3.Distance(transform.position, b.transform.position); // use b, not targetBuilding

//         float dist = 0f;
//         for (int i = 0; i < path.corners.Length - 1; i++)
//             dist += Vector3.Distance(path.corners[i], path.corners[i + 1]);
//         return dist;
//     }

//     // --- Stats & Lifecycle ---

//     public void TakeDamage(float damage)
//     {
//         currentHP -= damage;
//         if (currentHP <= 0) DieLogic();
//         healthBar?.UpdateBar(currentHP, scaledMaxHP);
//     }

//     private void DieLogic()
//     {
//         bool deathPrevented = false;
//         foreach (var a in instantiatedAbilities)
//             if (a != null && a.OnDeath(this)) deathPrevented = true;

//         if (!deathPrevented)
//         {
//             healthBar?.ReturnBar();
//             agent.isStopped = true; // stop moving during death animation
//             StopAllCoroutines();
//             foreach (var a in instantiatedAbilities) if (a != null) Destroy(a);

//             enemyAnimations.PlayDeathAnimation();
//             Destroy(gameObject);

//         }
//         else
//         {
//             currentHP = scaledMaxHP;
//         }
//     }

//     private void InitializeAbilities()
//     {
//         foreach (var a in data.Abilities)
//         {
//             if (a == null) continue;
//             var instance = Instantiate(a);
//             instance.OnAssigned(this);
//             instantiatedAbilities.Add(instance);
//         }
//     }

//     public void ModifySpeed(float mult, float dur)
//     {
//         StopCoroutine("SpeedModifierRoutine");
//         StartCoroutine(SpeedModifierRoutine(mult, dur));
//     }

//     private System.Collections.IEnumerator SpeedModifierRoutine(float multiplier, float duration)
//     {
//         agent.speed = data.MoveSpeed * multiplier;
//         yield return new WaitForSeconds(duration);
//         agent.speed = data.MoveSpeed;
//     }

//     public void ApplyScaling(float multiplier)
//     {
//         scaledMaxHP = data.MaxHP * multiplier;
//         scaledDamage = data.AttackDamage * multiplier;
//         currentHP = scaledMaxHP;
//     }
    

//     // --- Gizmos ---

//     private void OnDrawGizmosSelected()
//     {
//         if (data == null) return;

//         Gizmos.color = new Color(0, 1, 0, 0.5f);
//         Gizmos.DrawWireSphere(transform.position, data.AttackRadius);

//         Gizmos.color = new Color(1, 1, 0, 0.2f);
//         Gizmos.DrawWireSphere(transform.position, data.DetectionRadius);

//         if (currentMoveTarget != null)
//         {
//             Gizmos.color = isBreakingWall ? Color.magenta : Color.green;
//             Gizmos.DrawLine(transform.position, currentMoveTarget.transform.position);
//         }
//     }
// }