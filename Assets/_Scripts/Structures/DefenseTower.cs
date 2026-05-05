using UnityEngine;
using System.Collections.Generic;

public class DefenseTower : Building
{
    [Header("References")]
    [SerializeField] private DefenseData defenseData;
    [SerializeField] private RangeIndicator rangeIndicator;
    [SerializeField] private SpriteRenderer towerRenderer;
    [SerializeField] private BeamRenderer _beamRenderer;

    [Header("Live State")]
    [SerializeField] private TargetMode currentTargetMode = TargetMode.ClosestToHeart;
    private Transform currentTarget;
    private float attackTimer;
    private Transform heartTransform;

    // Continuous attack state
    private float _continuousRampTimer = 0f;
    private float _continuousTickTimer = 0f;
    private Transform _lastContinuousTarget;

    protected override void Awake()
    {
        base.Awake();
        if (towerRenderer == null) towerRenderer = GetComponentInChildren<SpriteRenderer>();
        _beamRenderer = GetComponent<BeamRenderer>();
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
        if (defenseData == null || !IsPowered) return;

        if (defenseData.attackType == AttackType.Continuous)
        {
            HandleContinuousAttack();
            return;
        }

        if (attackTimer > 0) attackTimer -= Time.deltaTime;

        // Healing doesn't need a target
        if (defenseData.attackType == AttackType.Healing)
        {
            if (attackTimer <= 0f)
            {
                PerformAttack();
                attackTimer = defenseData.attackCooldown;
            }
            return;
        }

        if (currentTarget == null || !IsTargetValid(currentTarget))
            currentTarget = FindTarget();

        if (currentTarget != null)
        {
            if (defenseData.useFourDirectionalFacing)
                UpdateFaceDirection(currentTarget.position);

            if (attackTimer <= 0f)
            {
                PerformAttack();
                attackTimer = defenseData.attackCooldown;
            }
        }
    }

    private void HandleContinuousAttack()
    {
        if (currentTarget == null || !IsTargetValid(currentTarget))
            currentTarget = FindTarget();

        if (currentTarget == null)
        {
            ResetContinuousRamp();
            _beamRenderer?.HideBeam();
            return;
        }

        if (_lastContinuousTarget != currentTarget)
        {
            ResetContinuousRamp();
            _lastContinuousTarget = currentTarget;
        }

        if (defenseData.useFourDirectionalFacing)
            UpdateFaceDirection(currentTarget.position);

        // Persistent beams still show every frame — non-persistent show only on tick
        if (_beamRenderer != null && _beamRenderer.IsPersistent)
            _beamRenderer.ShowBeam(transform.position, currentTarget.position);

        _continuousRampTimer += Time.deltaTime;
        _continuousRampTimer = Mathf.Min(_continuousRampTimer, defenseData.continuousRampTime);

        _continuousTickTimer -= Time.deltaTime;
        if (_continuousTickTimer <= 0f)
        {
            _continuousTickTimer = defenseData.continuousTickRate;
            ApplyContinuousDamage();
        }
    }

    private void ApplyContinuousDamage()
    {
        if (currentTarget == null) return;

        // Non-persistent beam flashes only on damage tick
        if (_beamRenderer != null && !_beamRenderer.IsPersistent)
            _beamRenderer.ShowBeam(transform.position, currentTarget.position);

        float t = defenseData.continuousRampTime > 0
            ? _continuousRampTimer / defenseData.continuousRampTime
            : 1f;

        float damage = Mathf.Lerp(
            defenseData.continuousBaseDamage,
            defenseData.continuousMaxDamage,
            t) * defenseData.continuousTickRate;

        if (currentTarget.TryGetComponent(out Enemy enemy))
        {
            structureAnimations?.PlayAttackAnimation();
            enemy.TakeDamage(damage);

            if (enemy.CurrentHP <= 0)
            {
                ResetContinuousRamp();
                currentTarget = null;
            }
        }
    }

    private void ResetContinuousRamp()
    {
        _continuousRampTimer = 0f;
        _continuousTickTimer = 0f;
        _lastContinuousTarget = null;
        _beamRenderer?.HideBeam();
    }

    private void UpdateFaceDirection(Vector3 targetPos)
    {
        if (towerRenderer == null) return;
        Vector2 direction = (targetPos - transform.position).normalized;

    }

    private Transform FindTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, defenseData.range);
        List<Transform> validTargets = new List<Transform>();

        foreach (Collider2D hit in hits)
        {
            bool isGround = hit.TryGetComponent(out Enemy _);
            bool isFlying = hit.TryGetComponent(out FlyingEnemy _);

            // Skip if doesn't match filter
            bool valid = defenseData.targetFilter switch
            {
                TargetFilter.GroundOnly => isGround,
                TargetFilter.FlyingOnly => isFlying,
                _ => isGround || isFlying // Both
            };

            if (!valid) continue;

            // Check HP via IEnemy
            IEnemy enemy = hit.GetComponent<IEnemy>();
            if (enemy != null && enemy.CurrentHP > 0)
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
        Transform best = null;
        float min = Mathf.Infinity;
        foreach (var t in targets)
        {
            float d = Vector2.Distance(t.position, heartTransform.position);
            if (d < min) { min = d; best = t; }
        }
        return best;
    }

    private Transform GetFarthestFromHeart(List<Transform> targets)
    {
        if (heartTransform == null) return targets[0];
        Transform best = null;
        float max = -Mathf.Infinity;
        foreach (var t in targets)
        {
            float d = Vector2.Distance(t.position, heartTransform.position);
            if (d > max) { max = d; best = t; }
        }
        return best;
    }

    private Transform GetClosestToTower(List<Transform> targets)
    {
        Transform best = null;
        float min = Mathf.Infinity;
        foreach (var t in targets)
        {
            float d = Vector2.Distance(transform.position, t.position);
            if (d < min) { min = d; best = t; }
        }
        return best;
    }

    private bool IsTargetValid(Transform t)
    {
        if (t == null) return false;

        bool isGround = t.TryGetComponent(out Enemy groundEnemy) && groundEnemy.CurrentHP > 0;
        bool isFlying = t.TryGetComponent(out FlyingEnemy flyingEnemy) && flyingEnemy.CurrentHP > 0;

        bool matchesFilter = defenseData.targetFilter switch
        {
            TargetFilter.GroundOnly => isGround,
            TargetFilter.FlyingOnly => isFlying,
            _ => isGround || isFlying
        };

        if (!matchesFilter) return false;

        return Vector2.Distance(transform.position, t.position) <= defenseData.range;
    }

    private void PerformAttack()
    {
        structureAnimations?.PlayAttackAnimation();
        switch (defenseData.attackType)
        {
            case AttackType.SingleTarget: FireAtTarget(currentTarget); break;
            case AttackType.MultiTarget:  AttackMultipleTargets();     break;
            case AttackType.SplashDamage: FireAtTarget(currentTarget, AttackType.SplashDamage); break;
            case AttackType.AllInRange:   AttackAllInRange();          break;
            case AttackType.Healing:      HealNearbyBuildings();       break;
        }
    }

    private void FireProjectile(Transform t, AttackType type)
    {
        if (defenseData.projectileData == null) return;
        GameObject projObj = Instantiate(defenseData.projectileData.Prefab, transform.position, Quaternion.identity);
        if (projObj.TryGetComponent(out Projectile proj))
        {
            float radius = type == AttackType.SplashDamage ? defenseData.splashRadius : 0f;
            proj.Initialize(t, defenseData.damage, this, type, radius);
        }
    }

    private void FireAtTarget(Transform t, AttackType type = AttackType.SingleTarget)
    {   
        if (t == null) return;

        if (defenseData.useLineRendererAttack && _beamRenderer != null)
        {
            _beamRenderer.ShowBeam(transform.position, t.position);
            ApplyDirectDamage(t, type);
        }
        else
        {
            FireProjectile(t, type);
        }
    }

    private void ApplyDirectDamage(Transform t, AttackType type)
    {
        if (type == AttackType.SplashDamage)
        {
            Collider2D[] nearby = Physics2D.OverlapCircleAll(t.position, defenseData.splashRadius);
            float splashDamage = defenseData.damage * 0.5f;
            foreach (Collider2D col in nearby)
            {
                IEnemy enemy = col.GetComponent<IEnemy>();
                if (enemy != null && enemy.CurrentHP > 0)
                {
                    float finalDamage = col.transform == t ? defenseData.damage : splashDamage;
                    col.GetComponent<Enemy>()?.TakeDamage(finalDamage);
                    col.GetComponent<FlyingEnemy>()?.TakeDamage(finalDamage);
                }
            }
        }
        else
        {
            t.GetComponent<Enemy>()?.TakeDamage(defenseData.damage);
            t.GetComponent<FlyingEnemy>()?.TakeDamage(defenseData.damage);
        }
    }

    private void AttackMultipleTargets()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, defenseData.range);
        List<Transform> enemies = new List<Transform>();

        foreach (var hit in hits)
        {
            if (!MatchesTargetFilter(hit)) continue;
            IEnemy e = hit.GetComponent<IEnemy>();
            if (e != null && e.CurrentHP > 0) enemies.Add(hit.transform);
        }

        enemies.Sort((a, b) => Vector2.Distance(transform.position, a.position)
            .CompareTo(Vector2.Distance(transform.position, b.position)));

        int limit = Mathf.Min(enemies.Count, defenseData.maxTargets);
        for (int i = 0; i < limit; i++) FireAtTarget(enemies[i]);
    }

    private void AttackAllInRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, defenseData.range);
        foreach (var hit in hits)
        {
            if (!MatchesTargetFilter(hit)) continue;
            IEnemy e = hit.GetComponent<IEnemy>();
            if (e != null && e.CurrentHP > 0) FireAtTarget(hit.transform);
        }
    }

    // Shared helper to avoid repeating the filter logic
    private bool MatchesTargetFilter(Collider2D hit)
    {
        bool isGround = hit.TryGetComponent(out Enemy _);
        bool isFlying = hit.TryGetComponent(out FlyingEnemy _);

        return defenseData.targetFilter switch
        {
            TargetFilter.GroundOnly => isGround,
            TargetFilter.FlyingOnly => isFlying,
            _ => isGround || isFlying
        };
    }

    private void HealNearbyBuildings()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, defenseData.range);
        foreach (var hit in hits)
        {
            Building b = hit.GetComponent<Building>();
            if (b == null || b == this) continue;
            if (!b.IsAlive || b.HPPercent >= 1f) continue;
            b.Heal(defenseData.damage);
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