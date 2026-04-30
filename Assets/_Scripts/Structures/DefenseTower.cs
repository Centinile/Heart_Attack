using UnityEngine;
using System.Collections.Generic;

public class DefenseTower : Building
{
    [Header("References")]
    [SerializeField] private DefenseData defenseData; 
    [SerializeField] private RangeIndicator rangeIndicator;
    [SerializeField] private SpriteRenderer towerRenderer;
    private StructureAnimations structureAnimations;

    [Header("Live State")]
    [SerializeField] private TargetMode currentTargetMode = TargetMode.ClosestToHeart;
    private Transform currentTarget;
    private float attackTimer;
    private Transform heartTransform;

    protected override void Awake()
    {
        base.Awake();
        // structureType is now set automatically via Initialize(data)
        if (towerRenderer == null) towerRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Start()
    {
        GameObject heart = GameObject.FindGameObjectWithTag("Heart");
        if (heart != null) heartTransform = heart.transform;
        attackTimer = 0f;
        structureAnimations = GetComponent<StructureAnimations>();
    }

    public void SwitchTargetMode()
    {
        int nextMode = ((int)currentTargetMode + 1) % System.Enum.GetValues(typeof(TargetMode)).Length;
        currentTargetMode = (TargetMode)nextMode;
        Debug.Log($"{gameObject.name} targeting: {currentTargetMode}");
    }

    void Update()
    {
        // 1. Only run logic if we have data AND the building is powered
        if (defenseData == null || !IsPowered) return;

        if (attackTimer > 0) attackTimer -= Time.deltaTime;

        if (currentTarget == null || !IsTargetValid(currentTarget))
        {
            currentTarget = FindTarget();
        }

        if (currentTarget != null)
        {
            if (defenseData.useFourDirectionalFacing)
            {
                UpdateFaceDirection(currentTarget.position);
            }

            if (attackTimer <= 0f)
            {
                PerformAttack();
                attackTimer = defenseData.attackCooldown;
            }
        }
    }

    private void UpdateFaceDirection(Vector3 targetPos)
    {
        if (towerRenderer == null) return;

        Vector2 direction = (targetPos - transform.position).normalized;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            towerRenderer.sprite = direction.x > 0 ? defenseData.spriteRight : defenseData.spriteLeft;
        }
        else
        {
            towerRenderer.sprite = direction.y > 0 ? defenseData.spriteUp : defenseData.spriteDown;
        }
    }

    private Transform FindTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, defenseData.range);
        List<Transform> validTargets = new List<Transform>();

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent(out Enemy enemy) && enemy.CurrentHP > 0) 
                validTargets.Add(hit.transform);
        }

        if (validTargets.Count == 0) return null;

        return currentTargetMode switch
        {
            TargetMode.ClosestToHeart => GetClosestToHeart(validTargets),
            TargetMode.FarthestFromHeart => GetFarthestFromHeart(validTargets),
            TargetMode.ClosestToTower => GetClosestToTower(validTargets),
            _ => validTargets[0],
        };
    }

    private Transform GetClosestToHeart(List<Transform> targets)
    {
        if (heartTransform == null) return targets[0];
        Transform bestTarget = null;
        float minContext = Mathf.Infinity;
        
        foreach (var t in targets)
        {
            float d = Vector2.Distance(t.position, heartTransform.position);
            if (d < minContext) { minContext = d; bestTarget = t; }
        }
        return bestTarget;
    }

    private Transform GetFarthestFromHeart(List<Transform> targets)
    {
        if (heartTransform == null) return targets[0];
        Transform bestTarget = null;
        float maxContext = -Mathf.Infinity;

        foreach (var t in targets)
        {
            float d = Vector2.Distance(t.position, heartTransform.position);
            if (d > maxContext) { maxContext = d; bestTarget = t; }
        }
        return bestTarget;
    }

    private Transform GetClosestToTower(List<Transform> targets)
    {
        Transform bestTarget = null;
        float minContext = Mathf.Infinity;

        foreach (var t in targets)
        {
            float d = Vector2.Distance(transform.position, t.position);
            if (d < minContext) { minContext = d; bestTarget = t; }
        }
        return bestTarget;
    }

    private bool IsTargetValid(Transform t)
    {
        if (t == null) return false;
        return t.TryGetComponent(out Enemy e) && e.CurrentHP > 0 && 
               Vector2.Distance(transform.position, t.position) <= defenseData.range;
    }

    private void PerformAttack()
    {
        structureAnimations?.PlayAttackAnimation();
        switch (defenseData.attackType)
        {
            case AttackType.SingleTarget: FireProjectile(currentTarget, AttackType.SingleTarget); break;
            case AttackType.MultiTarget: AttackMultipleTargets(); break;
            case AttackType.SplashDamage: FireProjectile(currentTarget, AttackType.SplashDamage); break;
            case AttackType.AllInRange: AttackAllInRange(); break;
        }
    }

    private void FireProjectile(Transform t, AttackType type)
    {
        if (defenseData.projectileData == null) return;

        GameObject projObj = Instantiate(defenseData.projectileData.Prefab, transform.position, Quaternion.identity);
        if (projObj.TryGetComponent(out Projectile proj))
        {
            float radius = (type == AttackType.SplashDamage) ? defenseData.splashRadius : 0f;
            proj.Initialize(t, defenseData.damage, this, type, radius);
        }
    }

    private void AttackMultipleTargets()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, defenseData.range);
        List<Transform> enemies = new List<Transform>();

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out Enemy e) && e.CurrentHP > 0) enemies.Add(hit.transform);
        }

        enemies.Sort((a, b) => Vector2.Distance(transform.position, a.position).CompareTo(Vector2.Distance(transform.position, b.position)));

        int limit = Mathf.Min(enemies.Count, defenseData.maxTargets);
        for (int i = 0; i < limit; i++) FireProjectile(enemies[i], AttackType.SingleTarget);
    }

    private void AttackAllInRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, defenseData.range);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out Enemy e) && e.CurrentHP > 0) FireProjectile(hit.transform, AttackType.SingleTarget);
        }
    }

    public override void OnSelected()
    {
        base.OnSelected();
        if (rangeIndicator != null) rangeIndicator.Show(defenseData.range);
    }

    public override void OnDeselected()
    {
        base.OnDeselected();
        if (rangeIndicator != null) rangeIndicator.Hide();
    }
}