using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class DefenseTower : Building
{
    [Header("References")]
    [SerializeField] private DefenseData data;
    [SerializeField] private RangeIndicator rangeIndicator;
    [SerializeField] private SpriteRenderer towerRenderer;

    [Header("Live State")]
    [SerializeField] private TargetMode currentTargetMode;
    private Transform currentTarget;
    private float attackTimer;
    private Transform heartTransform;

    protected override void Awake()
    {
        base.Awake();
        structureType = StructureType.Defense;
        if (towerRenderer == null) towerRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Start()
    {
        GameObject heart = GameObject.FindGameObjectWithTag("Heart");
        if (heart != null) heartTransform = heart.transform;
        
        // Initialize targeting from the data default
        // If you want a default, you can set it here or in the Inspector
        currentTargetMode = TargetMode.ClosestToHeart; 
        attackTimer = 0f;
    }

    /// <summary>
    /// Call this from a UI button to cycle through targeting modes.
    /// </summary>
    public void SwitchTargetMode()
    {
        // Cycles: ClosestToHeart -> Farthest -> ClosestToTower -> First -> (Back to start)
        int nextMode = ((int)currentTargetMode + 1) % System.Enum.GetValues(typeof(TargetMode)).Length;
        currentTargetMode = (TargetMode)nextMode;
        
        Debug.Log($"Tower {gameObject.name} targeting changed to: {currentTargetMode}");
    }

    void Update()
    {
        if (data == null) return;

        if (attackTimer > 0) attackTimer -= Time.deltaTime;

        if (currentTarget == null || !IsTargetValid(currentTarget))
        {
            currentTarget = FindTarget();
        }

        if (currentTarget != null)
        {
            if (data.useFourDirectionalFacing)
            {
                UpdateFaceDirection(currentTarget.position);
            }

            if (attackTimer <= 0f)
            {
                PerformAttack();
                attackTimer = data.attackCooldown; // Simple seconds-based cooldown
            }
        }
    }

    private void UpdateFaceDirection(Vector3 targetPos)
    {
        if (towerRenderer == null) return;

        Vector2 direction = (targetPos - transform.position).normalized;

        // Determine 4-way direction
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            // Horizontal
            towerRenderer.sprite = direction.x > 0 ? data.spriteRight : data.spriteLeft;
        }
        else
        {
            // Vertical
            towerRenderer.sprite = direction.y > 0 ? data.spriteUp : data.spriteDown;
        }
    }

    private Transform FindTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, data.range);
        List<Transform> validTargets = new List<Transform>();

        foreach (Collider2D hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null && enemy.CurrentHP > 0) validTargets.Add(hit.transform);
        }

        if (validTargets.Count == 0) return null;

        // Use the instance variable 'currentTargetMode' instead of data
        switch (currentTargetMode)
        {
            case TargetMode.ClosestToHeart: return GetClosestToHeart(validTargets);
            case TargetMode.FarthestFromHeart: return GetFarthestFromHeart(validTargets);
            case TargetMode.ClosestToTower: return GetClosestToTower(validTargets);
            default: return validTargets[0];
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
        return Vector2.Distance(transform.position, target.position) <= data.range;
    }

    private void PerformAttack()
    {
        switch (data.attackType)
        {
            case AttackType.SingleTarget: FireProjectile(currentTarget); break;
            case AttackType.MultiTarget: AttackMultipleTargets(); break;
            case AttackType.SplashDamage: FireSplashProjectile(currentTarget); break;
            case AttackType.AllInRange: AttackAllInRange(); break;
        }
    }

    private void FireProjectile(Transform target)
    {
        if (data.projectileData == null) return;

        GameObject projObj = Instantiate(data.projectileData.Prefab, transform.position, Quaternion.identity);
        Projectile proj = projObj.GetComponent<Projectile>();
        if (proj != null) proj.Initialize(target, data.damage, this, AttackType.SingleTarget);
    }

    private void AttackMultipleTargets()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, data.range);
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
        int targetsToAttack = Mathf.Min(enemies.Count, data.maxTargets);
        
        for (int i = 0; i < targetsToAttack; i++)
        {
            FireProjectile(enemies[i]);
        }
    }
    
    private void FireSplashProjectile(Transform target)
    {        
        if (data.projectileData == null)
        {
            // Direct hit with splash
            ApplySplashDamage(target.position, data.damage);
            return;
        }
        
        GameObject projectileObj = InstantiateProjectile();
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        
        if (projectile != null)
        {
            projectile.Initialize(target, data.damage, this, AttackType.SplashDamage, data.splashRadius);
        }
    }
    
    private void AttackAllInRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, data.range);
        
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
        // Create projectile at tower position
        Vector3 spawnPosition = transform.position;
        
        // Use the prefab from ProjectileData
        GameObject projectileObj = Instantiate(data.projectileData.Prefab, spawnPosition, Quaternion.identity);
        
        return projectileObj;
    }
    
    private void ApplySplashDamage(Vector3 center, float damage)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, data.splashRadius);
        
        foreach (Collider2D hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                float distToCenter = Vector2.Distance(center, hit.transform.position);
                // Damage falls off based on distance from the center of the blast
                float damageMultiplier = 1f - (distToCenter / data.splashRadius);
                damageMultiplier = Mathf.Clamp01(damageMultiplier);
                
                enemy.TakeDamage(damage * damageMultiplier);
            }
        }
    }

    // Helper methods for Range Indicator
    public override void OnSelected()
    {
        base.OnSelected();
        if (rangeIndicator != null) rangeIndicator.Show(data.range);
    }

    public override void OnDeselected()
    {
        base.OnDeselected();
        if (rangeIndicator != null) rangeIndicator.Hide();
    }
}