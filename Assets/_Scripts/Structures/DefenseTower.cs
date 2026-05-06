using UnityEngine;
using System.Collections;
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

    // Single-target continuous state
    private float _continuousRampTimer = 0f;
    private float _continuousTickTimer = 0f;
    private Coroutine _burstCoroutine;
    private Transform _lastContinuousTarget;

    // Multi-target continuous state
    private class ContinuousTargetState
    {
        public float rampTimer  = 0f;
        public float tickTimer  = 0f;
        public float flashTimer = 0f; // counts down after a tick fires
        public BeamRenderer beam;
    }
    private Dictionary<Transform, ContinuousTargetState> _continuousTargets
        = new Dictionary<Transform, ContinuousTargetState>();

    // Pool of beam renderers for multi-target
    private List<BeamRenderer> _beamPool = new List<BeamRenderer>();
    private List<FlickerBeam> _flickerPool = new List<FlickerBeam>();

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
            // Only use multi-path if explicitly configured for multiple continuous targets
            if (defenseData.maxTargets > 1 && defenseData.attackType == AttackType.Continuous)
                HandleMultiContinuousAttack();
            else
                HandleContinuousAttack();
            return;
        }

        if (attackTimer > 0) attackTimer -= Time.deltaTime;

        if (defenseData.attackType == AttackType.Healing)
        {
            if (attackTimer <= 0f)
            {
                TriggerAttack();
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
                TriggerAttack();
                attackTimer = defenseData.attackCooldown;
            }
        }
    }

    // ── Single-target continuous ───────────────────────────────────────
    private float _continuousFlashTimer = 0f;
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
            AudioManager.Instance?.PlayLooping(
                defenseData.beamLoopSound,
                transform.position,
                GetInstanceID(),
                defenseData.beamLoopVolume);
        }

        AudioManager.Instance?.UpdateLoopingPosition(GetInstanceID(), transform.position);

        if (_beamRenderer != null && _beamRenderer.IsPersistent)
            _beamRenderer.ShowBeam(transform.position, currentTarget.position);

        _continuousRampTimer += Time.deltaTime;
        _continuousRampTimer = Mathf.Min(_continuousRampTimer, defenseData.continuousRampTime);

        _continuousTickTimer -= Time.deltaTime;

        if (_beamRenderer != null)
        {
            if (_beamRenderer.IsPersistent)
            {
                _beamRenderer.ShowBeam(transform.position, currentTarget.position);
            }
            else
            {
                if (_continuousFlashTimer > 0f)
                {
                    _continuousFlashTimer -= Time.deltaTime;
                    _beamRenderer.ShowBeam(transform.position, currentTarget.position);
                    if (_continuousFlashTimer <= 0f)
                        _beamRenderer.HideBeam();
                }
            }
        }

        if (_continuousTickTimer <= 0f)
        {
            _continuousTickTimer  = defenseData.continuousTickRate;
            _continuousFlashTimer = _beamRenderer != null ? _beamRenderer.flashDuration : 0.15f;
            ApplyContinuousDamage(currentTarget, ref _continuousRampTimer);
        }
    }

    private void ResetContinuousRamp()
    {
        _continuousRampTimer  = 0f;
        _continuousTickTimer  = 0f;
        _continuousFlashTimer = 0f;
        _lastContinuousTarget = null;
        _beamRenderer?.HideBeam();
        AudioManager.Instance?.StopLooping(GetInstanceID());
    }

    // ── Multi-target continuous ────────────────────────────────────────

    private void HandleMultiContinuousAttack()
    {
        // Find all valid targets up to maxTargets
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, defenseData.range);
        HashSet<Transform> currentFrameTargets = new HashSet<Transform>();
        List<Transform> validTargets = new List<Transform>();

        foreach (var hit in hits)
        {
            if (!MatchesTargetFilter(hit)) continue;
            IEnemy e = hit.GetComponent<IEnemy>();
            if (e != null && e.CurrentHP > 0)
                validTargets.Add(hit.transform);
        }

        // Sort by target mode
        validTargets = SortByTargetMode(validTargets);
        int limit = Mathf.Min(validTargets.Count, defenseData.maxTargets);

        for (int i = 0; i < limit; i++)
            currentFrameTargets.Add(validTargets[i]);

        // Remove targets no longer in range
        List<Transform> toRemove = new List<Transform>();
        foreach (var kvp in _continuousTargets)
        {
            if (!currentFrameTargets.Contains(kvp.Key) || !IsTargetValid(kvp.Key))
            {
                kvp.Value.beam?.HideBeam();
                ReturnBeamToPool(kvp.Value.beam);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var t in toRemove)
            _continuousTargets.Remove(t);

        // Stop loop sound if no targets
        if (_continuousTargets.Count == 0 && currentFrameTargets.Count == 0)
        {
            AudioManager.Instance?.StopLooping(GetInstanceID());
            return;
        }

        // Add new targets
        foreach (Transform t in currentFrameTargets)
        {
            if (!_continuousTargets.ContainsKey(t))
            {
                BeamRenderer beam = GetBeamFromPool();
                _continuousTargets[t] = new ContinuousTargetState { beam = beam };

                // Start loop sound on first target acquired
                if (_continuousTargets.Count == 1)
                    AudioManager.Instance?.PlayLooping(
                        defenseData.beamLoopSound,
                        transform.position,
                        GetInstanceID(),
                        defenseData.beamLoopVolume);
            }
        }

        AudioManager.Instance?.UpdateLoopingPosition(GetInstanceID(), transform.position);

        var activeTargets = new List<Transform>(_continuousTargets.Keys);
        foreach (Transform t in activeTargets)
        {
            if (!_continuousTargets.TryGetValue(t, out ContinuousTargetState state)) continue;

            state.rampTimer += Time.deltaTime;
            state.rampTimer  = Mathf.Min(state.rampTimer, defenseData.continuousRampTime);
            state.tickTimer -= Time.deltaTime;

            if (state.beam != null)
            {
                if (state.beam.IsPersistent)
                {
                    // Persistent — show every frame
                    state.beam.ShowBeam(transform.position, t.position);
                }
                else
                {
                    // Non-persistent — show during flash window, hide outside it
                    if (state.flashTimer > 0f)
                    {
                        state.flashTimer -= Time.deltaTime;
                        state.beam.ShowBeam(transform.position, t.position);
                        if (state.flashTimer <= 0f)
                            state.beam.HideBeam();
                    }
                }
            }

            if (state.tickTimer <= 0f)
            {
                state.tickTimer  = defenseData.continuousTickRate;
                state.flashTimer = state.beam != null ? state.beam.flashDuration : 0.15f;
                ApplyContinuousDamage(t, ref state.rampTimer);
            }
        }
    }

    // ── Shared continuous damage ───────────────────────────────────────

    private void ApplyContinuousDamage(Transform t, ref float rampTimer)
    {
        if (t == null) return;

        float progress = defenseData.continuousRampTime > 0
            ? rampTimer / defenseData.continuousRampTime
            : 1f;

        float damage = Mathf.Lerp(
            defenseData.continuousBaseDamage,
            defenseData.continuousMaxDamage,
            progress) * defenseData.continuousTickRate;

        structureAnimations?.PlayAttackAnimation();

        if (t.TryGetComponent(out Enemy enemy))
            enemy.TakeDamage(damage);
        else if (t.TryGetComponent(out FlyingEnemy flyingEnemy))
            flyingEnemy.TakeDamage(damage);

        // Single-target cleanup only — multi-target cleanup handled by next frame refresh
        if (!_continuousTargets.ContainsKey(t))
        {
            IEnemy iEnemy = t.GetComponent<IEnemy>();
            if (iEnemy != null && iEnemy.CurrentHP <= 0)
            {
                ResetContinuousRamp();
                currentTarget = null;
            }
        }
    }

    // ── Beam pool for multi-target ─────────────────────────────────────

    private BeamRenderer GetBeamFromPool()
    {
        foreach (var b in _beamPool)
            if (b != null && !b.gameObject.activeSelf)
            {
                b.gameObject.SetActive(true);
                return b;
            }

        GameObject newBeamObj = new GameObject("PooledBeam");
        newBeamObj.transform.SetParent(transform);
        newBeamObj.transform.localPosition = Vector3.zero;

        LineRenderer originalLR = _beamRenderer.GetComponent<LineRenderer>();
        LineRenderer newLR = newBeamObj.AddComponent<LineRenderer>();
        if (originalLR != null)
        {
            newLR.startWidth    = originalLR.startWidth;
            newLR.endWidth      = originalLR.endWidth;
            newLR.material      = originalLR.material;
            newLR.startColor    = originalLR.startColor;
            newLR.endColor      = originalLR.endColor;
            newLR.positionCount = 2;
            newLR.enabled       = false;
        }

        BeamRenderer newBeam = newBeamObj.AddComponent<BeamRenderer>();
        newBeam.CopySettingsFrom(_beamRenderer); // copy persistentBeam and other settings
        _beamPool.Add(newBeam);
        return newBeam;
    }

    private FlickerBeam GetFlickerBeam()
    {
        foreach (var b in _flickerPool)
            if (b != null && !b.gameObject.activeSelf)
            {
                b.gameObject.SetActive(true);
                return b;
            }

        if (defenseData.flickerBeamPrefab == null) return null;

        FlickerBeam newBeam = Instantiate(defenseData.flickerBeamPrefab, transform);
        newBeam.CopySettingsFrom(defenseData.flickerBeamPrefab);
        _flickerPool.Add(newBeam);
        return newBeam;
    }

    private void ReturnBeamToPool(BeamRenderer beam)
    {
        if (beam == null || beam == _beamRenderer) return;
        beam.HideBeam();
        beam.gameObject.SetActive(false);
    }

    // ── Sorting helper ─────────────────────────────────────────────────

    private List<Transform> SortByTargetMode(List<Transform> targets)
    {
        switch (currentTargetMode)
        {
            case TargetMode.ClosestToHeart:
                targets.Sort((a, b) => Vector2.Distance(a.position, heartTransform != null ? heartTransform.position : transform.position)
                    .CompareTo(Vector2.Distance(b.position, heartTransform != null ? heartTransform.position : transform.position)));
                break;
            case TargetMode.FarthestFromHeart:
                targets.Sort((a, b) => Vector2.Distance(b.position, heartTransform != null ? heartTransform.position : transform.position)
                    .CompareTo(Vector2.Distance(a.position, heartTransform != null ? heartTransform.position : transform.position)));
                break;
            case TargetMode.ClosestToTower:
                targets.Sort((a, b) => Vector2.Distance(a.position, transform.position)
                    .CompareTo(Vector2.Distance(b.position, transform.position)));
                break;
        }
        return targets;
    }

    // ── Remaining methods unchanged ────────────────────────────────────

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

            bool valid = defenseData.targetFilter switch
            {
                TargetFilter.GroundOnly => isGround,
                TargetFilter.FlyingOnly => isFlying,
                _ => isGround || isFlying
            };

            if (!valid) continue;

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

        if (defenseData.attackSound != null)
            AudioManager.Instance?.PlayOneShot(defenseData.attackSound, transform.position, defenseData.attackSoundVolume);

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

        if (defenseData.useLineRendererAttack)
        {
            // Use flicker beam if prefab assigned, otherwise fall back to BeamRenderer
            if (defenseData.flickerBeamPrefab != null)
            {
                FlickerBeam flicker = GetFlickerBeam();
                flicker?.Fire(transform.position, t.position);
            }
            else if (_beamRenderer != null)
            {
                _beamRenderer.ShowBeam(transform.position, t.position);
            }

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

    private void TriggerAttack()
    {
        if (defenseData.useBurst && defenseData.burstCount > 1)
        {
            if (_burstCoroutine != null) StopCoroutine(_burstCoroutine);
            _burstCoroutine = StartCoroutine(BurstCoroutine());
        }
        else
        {
            PerformAttack();
        }
    }

    private IEnumerator BurstCoroutine()
    {
        for (int i = 0; i < defenseData.burstCount; i++)
        {
            if (defenseData.attackType != AttackType.Healing)
            {
                if (currentTarget == null || !IsTargetValid(currentTarget))
                    currentTarget = FindTarget();

                if (currentTarget == null) yield break;
            }

            PerformAttack();

            if (i < defenseData.burstCount - 1)
                yield return new WaitForSeconds(defenseData.burstInterval);
        }
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

    protected override void OnDestroyed()
    {
        foreach (var kvp in _continuousTargets)
        {
            kvp.Value.beam?.HideBeam();
            ReturnBeamToPool(kvp.Value.beam);
        }
        _continuousTargets.Clear();

        foreach (var f in _flickerPool)
            if (f != null) Destroy(f.gameObject);
        _flickerPool.Clear();

        AudioManager.Instance?.StopLooping(GetInstanceID());

        if (_burstCoroutine != null)
        {
            StopCoroutine(_burstCoroutine);
            _burstCoroutine = null;
        }
        base.OnDestroyed();
    }
}