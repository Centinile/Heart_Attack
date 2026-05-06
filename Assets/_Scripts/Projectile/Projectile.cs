using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private ProjectileData data;

    [Header("Visual")]
    [SerializeField] private ProjectileVisual projectileVisual;

    // ── Shared state ───────────────────────────────────────────────────
    private Transform target;
    private Vector3 lastTargetPosition;
    private float damage;
    private bool hasHit;
    private AttackType attackType;
    private float splashRadius;
    private DefenseTower sourceTower;

    // ── Arc trajectory state ───────────────────────────────────────────
    private Vector3 trajectoryStartPoint;
    private Vector3 projectileMoveDir;
    private Vector3 trajectoryRange;
    private float moveSpeed;
    private float maxMoveSpeed;
    private float trajectoryMaxRelativeHeight;

    private float nextYTrajectoryPosition;
    private float nextXTrajectoryPosition;
    private float nextPositionYCorrectionAbsolute;
    private float nextPositionXCorrectionAbsolute;

    private const float HIT_DISTANCE = 0.2f;
    private const float ARC_HIT_DISTANCE = 1f;

    // ──────────────────────────────────────────────────────────────────

    public void Initialize(Transform target, float damage, DefenseTower source,
        AttackType attackType = AttackType.SingleTarget, float splashRadius = 0f)
    {
        this.target       = target;
        this.damage       = damage;
        this.sourceTower  = source;
        this.attackType   = attackType;
        this.splashRadius = splashRadius;

        if (target != null)
            lastTargetPosition = target.position;

        // Arc setup — reads everything from ProjectileData
        if (data.UseArcTrajectory)
        {
            trajectoryStartPoint = transform.position;
            maxMoveSpeed = data.Speed;
            moveSpeed = maxMoveSpeed;

            float xDist = target != null
                ? target.position.x - transform.position.x
                : 1f;
            trajectoryMaxRelativeHeight = Mathf.Abs(xDist) * data.TrajectoryMaxHeight;

            // If locking target position, freeze lastTargetPosition now and never update it
        }

        Destroy(gameObject, data.Lifetime);

        if (data.TrailEffect != null)
            Instantiate(data.TrailEffect, transform.position, Quaternion.identity, transform);

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && data.Sprite != null)
            sr.sprite = data.Sprite;

        projectileVisual?.SetTarget(target);
    }

    private void Update()
    {
        if (hasHit) return;

        if (target != null && (!data.UseArcTrajectory || !data.LockTargetOnFire))
            lastTargetPosition = target.position;

        if (data.UseArcTrajectory)
            UpdateArcPosition();
        else
            UpdateStraightPosition();

        projectileVisual?.UpdateFacing(projectileMoveDir);

        float hitDist = data.UseArcTrajectory ? ARC_HIT_DISTANCE : HIT_DISTANCE;
        if (Vector3.Distance(transform.position, lastTargetPosition) < hitDist)
        {
            Hit();
            return; // stop all further processing this frame
        }
    }

    // ── Straight movement ──────────────────────────────────────────────

    private void UpdateStraightPosition()
    {
        Vector3 direction = (lastTargetPosition - transform.position).normalized;
        if (direction == Vector3.zero) return;

        // Removed: transform.up = direction — ProjectileVisual handles rotation
        transform.position += direction * data.Speed * Time.deltaTime;
        projectileMoveDir = direction;
    }

    // ── Arc movement ───────────────────────────────────────────────────

    private void UpdateArcPosition()
    {
        // Recalculate range each frame — uses lastTargetPosition which may be frozen
        trajectoryRange = lastTargetPosition - trajectoryStartPoint;

        if (Mathf.Abs(trajectoryRange.normalized.x) < Mathf.Abs(trajectoryRange.normalized.y))
        {
            if (trajectoryRange.y < 0) moveSpeed = -Mathf.Abs(moveSpeed);
            UpdatePositionWithXCurve();
        }
        else
        {
            if (trajectoryRange.x < 0) moveSpeed = -Mathf.Abs(moveSpeed);
            UpdatePositionWithYCurve();
        }
    }

    private void UpdatePositionWithXCurve()
    {
        float nextPositionY = transform.position.y + moveSpeed * Time.deltaTime;
        float nextPositionYNormalized = trajectoryRange.y != 0
            ? (nextPositionY - trajectoryStartPoint.y) / trajectoryRange.y
            : 0f;

        float nextPositionXNormalized = data.TrajectoryAnimationCurve.Evaluate(nextPositionYNormalized);
        nextXTrajectoryPosition = nextPositionXNormalized * trajectoryMaxRelativeHeight;

        float nextPositionXCorrectionNormalized = data.AxisCorrectionAnimationCurve.Evaluate(nextPositionYNormalized);
        nextPositionXCorrectionAbsolute = nextPositionXCorrectionNormalized * trajectoryRange.x;

        if (trajectoryRange.x > 0 && trajectoryRange.y > 0) nextXTrajectoryPosition = -nextXTrajectoryPosition;
        if (trajectoryRange.x < 0 && trajectoryRange.y < 0) nextXTrajectoryPosition = -nextXTrajectoryPosition;

        float nextPositionX = trajectoryStartPoint.x + nextXTrajectoryPosition + nextPositionXCorrectionAbsolute;
        Vector3 newPosition = new Vector3(nextPositionX, nextPositionY, 0);

        CalculateNextProjectileSpeed(nextPositionYNormalized);
        projectileMoveDir = newPosition - transform.position;
        transform.position = newPosition;
    }

    private void UpdatePositionWithYCurve()
    {
        float nextPositionX = transform.position.x + moveSpeed * Time.deltaTime;
        float nextPositionXNormalized = trajectoryRange.x != 0
            ? (nextPositionX - trajectoryStartPoint.x) / trajectoryRange.x
            : 0f;

        float nextPositionYNormalized = data.TrajectoryAnimationCurve.Evaluate(nextPositionXNormalized);
        nextYTrajectoryPosition = nextPositionYNormalized * trajectoryMaxRelativeHeight;

        float nextPositionYCorrectionNormalized = data.AxisCorrectionAnimationCurve.Evaluate(nextPositionXNormalized);
        nextPositionYCorrectionAbsolute = nextPositionYCorrectionNormalized * trajectoryRange.y;

        float nextPositionY = trajectoryStartPoint.y + nextYTrajectoryPosition + nextPositionYCorrectionAbsolute;
        Vector3 newPosition = new Vector3(nextPositionX, nextPositionY, 0);

        CalculateNextProjectileSpeed(nextPositionXNormalized);
        projectileMoveDir = newPosition - transform.position;
        transform.position = newPosition;
    }

    private void CalculateNextProjectileSpeed(float normalizedPosition)
    {
        float nextMoveSpeedNormalized = data.ProjectileSpeedAnimationCurve.Evaluate(normalizedPosition);
        moveSpeed = nextMoveSpeedNormalized * maxMoveSpeed;
    }

    // ── Hit handling ───────────────────────────────────────────────────

    private void Hit()
    {
        if (hasHit) return;
        hasHit = true;

        if (attackType == AttackType.SplashDamage)
            ApplySplashDamage();
        else if (target != null)
            ApplyDamage(target);

        if (data.ImpactEffect != null)
            Instantiate(data.ImpactEffect, transform.position, Quaternion.identity);

        // Play impact sound at hit position
        if (data.impactSound != null)
            AudioManager.Instance?.PlayOneShot(data.impactSound, transform.position, data.impactSoundVolume);

        CancelInvoke();
        Destroy(gameObject);
    }

    private void ApplyDamage(Transform targetTransform)
    {
        if (targetTransform == null) return;

        // Try building first (enemy projectiles hit buildings)
        Building building = targetTransform.GetComponent<Building>();
        if (building != null)
        {
            building.TakeDamage(damage);
            TryApplyDOT(transform.position);
            return;
        }

        // Fall back to enemy (tower projectiles hit enemies)
        Enemy enemy = targetTransform.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            TryApplyDOT(transform.position);
        }
    }

    private void ApplySplashDamage()
    {
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, splashRadius);
        float splashDamage = damage * 0.5f;

        foreach (Collider2D col in nearby)
        {
            Enemy enemy = col.GetComponent<Enemy>();
            if (enemy == null) continue;
            float finalDamage = col.transform == target ? damage : splashDamage;
            enemy.TakeDamage(finalDamage);
        }

        TryApplyDOT(transform.position);
    }

    private void TryApplyDOT(Vector3 position)
    {
        if (!data.ApplyDOT) return;

        GameObject zoneObj = data.DOTZonePrefab != null
            ? Instantiate(data.DOTZonePrefab, position, Quaternion.identity)
            : new GameObject("DOTZone");

        if (data.DOTZonePrefab == null)
            zoneObj.transform.position = position;

        DOTZone zone = zoneObj.GetComponent<DOTZone>() ?? zoneObj.AddComponent<DOTZone>();
        zone.Initialize(data.DOTDamagePerTick, data.DOTTickInterval, data.DOTDuration, data.DOTRadius);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        // Tower projectile hitting an enemy
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null && (other.transform == target || !data.Homing))
        {
            Hit();
            return;
        }

        // Enemy projectile hitting a building
        Building building = other.GetComponent<Building>();
        if (building != null && other.transform == target)
            Hit();
    }

    // Called when fired by an enemy — parallel to Initialize() for towers
    public void InitializeFromEnemy(Transform target, float damage)
    {
        this.target      = target;
        this.damage      = damage;
        this.sourceTower = null;
        this.attackType  = AttackType.SingleTarget;
        this.splashRadius = 0f;

        if (target != null)
            lastTargetPosition = target.position;

        if (data.UseArcTrajectory)
        {
            trajectoryStartPoint = transform.position;
            maxMoveSpeed = data.Speed;
            moveSpeed    = maxMoveSpeed;

            float xDist = target != null
                ? target.position.x - transform.position.x
                : 1f;
            trajectoryMaxRelativeHeight = Mathf.Abs(xDist) * data.TrajectoryMaxHeight;
        }

        Destroy(gameObject, data.Lifetime);

        if (data.TrailEffect != null)
            Instantiate(data.TrailEffect, transform.position, Quaternion.identity, transform);

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && data.Sprite != null)
            sr.sprite = data.Sprite;

        projectileVisual?.SetTarget(target);
    }
}