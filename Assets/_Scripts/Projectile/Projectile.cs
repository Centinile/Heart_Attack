using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private ProjectileData data;

    private Transform target;
    private Vector3 lastTargetPosition;
    private float damage;
    private bool hasHit;
    private AttackType attackType;
    private float splashRadius;
    private DefenseTower sourceTower;

    public void Initialize(Transform target, float damage, DefenseTower source, AttackType attackType = AttackType.SingleTarget, float splashRadius = 0f)
    {
        this.target = target;
        this.damage = damage;
        this.sourceTower = source;
        this.attackType = attackType;
        this.splashRadius = splashRadius;

        if (target != null) lastTargetPosition = target.position;

        Destroy(gameObject, data.Lifetime);

        if (data.TrailEffect != null)
            Instantiate(data.TrailEffect, transform.position, Quaternion.identity, transform);

        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && data.Sprite != null)
            spriteRenderer.sprite = data.Sprite;
    }

    void Update()
    {
        if (hasHit) return;

        if (target != null)
            lastTargetPosition = target.position;

        Vector3 direction = (lastTargetPosition - transform.position).normalized;

        if (direction != Vector3.zero)
        {
            if (data.Homing) transform.up = direction;
            transform.position += direction * data.Speed * Time.deltaTime;
        }

        if (Vector2.Distance(transform.position, lastTargetPosition) < 0.2f)
            Hit();
    }

    void Hit()
    {
        if (hasHit) return;
        hasHit = true;

        if (attackType == AttackType.SplashDamage)
            ApplySplashDamage();
        else if (target != null)
            ApplyDamage(target);

        if (data.ImpactEffect != null)
            Instantiate(data.ImpactEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

    void ApplyDamage(Transform targetTransform)
    {
        if (targetTransform == null) return;

        Enemy enemy = targetTransform.GetComponent<Enemy>();
        if (enemy == null) return;

        enemy.TakeDamage(damage);
        TryApplyDOT(transform.position); // spawn zone at impact point
    }


    void ApplySplashDamage()
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

        // One zone at impact point regardless of how many enemies were hit
        TryApplyDOT(transform.position);
    }

    void TryApplyDOT(Vector3 position)
    {
        if (!data.ApplyDOT) return;

        // Spawn visual prefab if assigned, otherwise spawn a bare GameObject
        GameObject zoneObj = data.DOTZonePrefab != null
            ? Instantiate(data.DOTZonePrefab, position, Quaternion.identity)
            : new GameObject("DOTZone");

        if (data.DOTZonePrefab == null)
            zoneObj.transform.position = position;

        DOTZone zone = zoneObj.GetComponent<DOTZone>() ?? zoneObj.AddComponent<DOTZone>();
        zone.Initialize(data.DOTDamagePerTick, data.DOTTickInterval, data.DOTDuration, data.DOTRadius);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null && (other.transform == target || !data.Homing))
            Hit();
    }
}