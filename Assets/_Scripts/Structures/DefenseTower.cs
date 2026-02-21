using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

/// <summary>
/// Represents a defensive tower structure.
/// Towers can also block enemy movement and attack enemies.
/// </summary>
public class DefenseTower : Building
{
    [Header("NavMesh Settings")]
    [SerializeField] private bool isNavMeshObstacle = false;
    [SerializeField] private bool carveNavMesh = false;
    
    [Header("References")]
    [SerializeField] private DefenseData data;
    
    [Header("State")]
    [SerializeField] private Transform currentTarget;
    [SerializeField] private float attackTimer;
    [SerializeField] private bool isAttacking;
    
    // Cached references
    private Transform heartTransform;
    private NavMeshObstacle navMeshObstacle;
    
    public DefenseData Data => data;
    public bool IsAttacking => isAttacking;
    public Transform CurrentTarget => currentTarget;
    
    protected override void Awake()
    {
        base.Awake();
        structureType = StructureType.Defense;
        
        // Add NavMeshObstacle component
        navMeshObstacle = gameObject.AddComponent<NavMeshObstacle>();
        navMeshObstacle.enabled = isNavMeshObstacle;
        navMeshObstacle.carving = carveNavMesh;
        navMeshObstacle.shape = NavMeshObstacleShape.Capsule;
        navMeshObstacle.center = Vector3.zero;
        
        // Set size based on collider
        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider != null)
        {
            navMeshObstacle.size = new Vector3(circleCollider.radius * 2, 1f, 0.1f);
        }
    }
    
    void Start()
    {
        // Find the heart for targeting calculations
        GameObject heart = GameObject.FindGameObjectWithTag("Heart");
        if (heart != null)
        {
            heartTransform = heart.transform;
        }
        
        attackTimer = 0f;
    }
    
    void Update()
    {
        if (data == null) return;
        
        // Update attack timer
        attackTimer -= Time.deltaTime;
        
        // Find and attack target
        if (currentTarget == null || !IsTargetValid(currentTarget))
        {
            currentTarget = FindTarget();
        }
        
        if (currentTarget != null)
        {
            float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);
            
            if (distanceToTarget <= data.Range)
            {
                // REMOVED: FaceTarget() - Towers no longer rotate, add this again if we want rotation
                
                // Attack if ready
                if (attackTimer <= 0f)
                {
                    PerformAttack();
                    attackTimer = 1f / data.AttackSpeed;
                }
                
                isAttacking = true;
            }
            else
            {
                // Target out of range, find new target
                currentTarget = FindTarget();
                isAttacking = false;
            }
        }
        else
        {
            isAttacking = false;
        }
    }
    
    /// Finds the best target based on the tower's target mode.
    private Transform FindTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, data.Range);
        
        if (hits.Length == 0) return null;
        
        List<Transform> validTargets = new List<Transform>();
        
        foreach (Collider2D hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null && enemy.CurrentHP > 0)
            {
                validTargets.Add(hit.transform);
            }
        }
        
        if (validTargets.Count == 0) return null;
        
        // Sort based on target mode
        switch (data.TargetMode)
        {
            case TargetMode.ClosestToHeart:
                return GetClosestToHeart(validTargets);
                
            case TargetMode.FarthestFromHeart:
                return GetFarthestFromHeart(validTargets);
                
            case TargetMode.ClosestToTower:
                return GetClosestToTower(validTargets);
                
            case TargetMode.First:
                return validTargets[0];
                
            default:
                return validTargets[0];
        }
    }
    
    private Transform GetClosestToHeart(List<Transform> targets)
    {
        Transform bestTarget = null;
        float closestDistToHeart = Mathf.Infinity;
        
        foreach (Transform target in targets)
        {
            if (heartTransform == null) continue;
            
            float distToHeart = Vector2.Distance(target.position, heartTransform.position);
            
            if (distToHeart < closestDistToHeart)
            {
                closestDistToHeart = distToHeart;
                bestTarget = target;
            }
        }
        
        return bestTarget;
    }
    
    private Transform GetFarthestFromHeart(List<Transform> targets)
    {
        Transform bestTarget = null;
        float farthestDistToHeart = -Mathf.Infinity;
        
        foreach (Transform target in targets)
        {
            if (heartTransform == null) continue;
            
            float distToHeart = Vector2.Distance(target.position, heartTransform.position);
            
            if (distToHeart > farthestDistToHeart)
            {
                farthestDistToHeart = distToHeart;
                bestTarget = target;
            }
        }
        
        return bestTarget;
    }
    
    private Transform GetClosestToTower(List<Transform> targets)
    {
        Transform bestTarget = null;
        float closestDistToTower = Mathf.Infinity;
        
        foreach (Transform target in targets)
        {
            float distToTower = Vector2.Distance(transform.position, target.position);
            
            if (distToTower < closestDistToTower)
            {
                closestDistToTower = distToTower;
                bestTarget = target;
            }
        }
        
        return bestTarget;
    }
    
    private bool IsTargetValid(Transform target)
    {
        if (target == null) return false;
        
        Enemy enemy = target.GetComponent<Enemy>();
        if (enemy == null || enemy.CurrentHP <= 0) return false;
        
        float distance = Vector2.Distance(transform.position, target.position);
        return distance <= data.Range;
    }
    
    // REMOVED: FaceTarget() method - towers no longer rotate
    
    private void PerformAttack()
    {
        if (currentTarget == null) return;
        
        switch (data.AttackType)
        {
            case AttackType.SingleTarget:
                FireProjectile(currentTarget);
                break;
                
            case AttackType.MultiTarget:
                AttackMultipleTargets();
                break;
                
            case AttackType.SplashDamage:
                FireSplashProjectile(currentTarget);
                break;
                
            case AttackType.AllInRange:
                AttackAllInRange();
                break;
        }
    }
    
    private void FireProjectile(Transform target)
    {
        if (data.ProjectileData == null)
        {
            // Direct hit if no projectile
            Enemy enemy = target.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(data.Damage);
            }
            return;
        }
        
        GameObject projectileObj = InstantiateProjectile();
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        
        if (projectile != null)
        {
            projectile.Initialize(target, data.Damage, this, AttackType.SingleTarget);
        }
    }
    
    private void AttackMultipleTargets()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, data.Range);
        List<Transform> enemies = new List<Transform>();
        
        foreach (Collider2D hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null && enemy.CurrentHP > 0)
            {
                enemies.Add(hit.transform);
            }
        }
        
        // Sort by distance to tower
        enemies.Sort((a, b) => 
            Vector2.Distance(transform.position, a.position).CompareTo(
                Vector2.Distance(transform.position, b.position)));
        
        // Attack up to maxTargets
        int targetsToAttack = Mathf.Min(enemies.Count, data.MaxTargets);
        
        for (int i = 0; i < targetsToAttack; i++)
        {
            FireProjectile(enemies[i]);
        }
    }
    
    private void FireSplashProjectile(Transform target)
    {
        if (data.ProjectileData == null)
        {
            // Direct hit with splash
            ApplySplashDamage(target.position, data.Damage);
            return;
        }
        
        GameObject projectileObj = InstantiateProjectile();
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        
        if (projectile != null)
        {
            projectile.Initialize(target, data.Damage, this, AttackType.SplashDamage, data.SplashRadius);
        }
    }
    
    private void AttackAllInRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, data.Range);
        
        foreach (Collider2D hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null && enemy.CurrentHP > 0)
            {
                FireProjectile(hit.transform);
            }
        }
    }
    
    private GameObject InstantiateProjectile()
    {
        // Create projectile at tower position (no rotation)
        Vector3 spawnPosition = transform.position;
        
        // Use the prefab from ProjectileData
        GameObject projectileObj = Instantiate(data.ProjectileData.Prefab, spawnPosition, Quaternion.identity);
        
        return projectileObj;
    }
    
    private void ApplySplashDamage(Vector3 center, float damage)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, data.SplashRadius);
        
        foreach (Collider2D hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                float distToCenter = Vector2.Distance(center, hit.transform.position);
                float damageMultiplier = 1f - (distToCenter / data.SplashRadius);
                enemy.TakeDamage(damage * damageMultiplier);
            }
        }
    }
    
    /// Returns the position from which projectiles should spawn.
    /// Override for custom attack origins.
    public virtual Vector3 GetAttackPosition()
    {
        return transform.position;
    }
    
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Draw range circle
        Gizmos.color = new Color(0, 1, 0, 0.1f);
        Gizmos.DrawWireSphere(transform.position, data != null ? data.Range : 5f);
        
        // Draw line to current target
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
#endif
}