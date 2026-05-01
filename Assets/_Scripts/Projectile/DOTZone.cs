using System.Collections;
using UnityEngine;

public class DOTZone : MonoBehaviour
{
    private float _damagePerTick;
    private float _tickInterval;
    private float _duration;
    private float _radius;

    public void Initialize(float damagePerTick, float tickInterval, float duration, float radius)
    {
        _damagePerTick = damagePerTick;
        _tickInterval = tickInterval;
        _duration = duration;
        _radius = radius;

        StartCoroutine(TickRoutine());
        Destroy(gameObject, duration);
    }

    private IEnumerator TickRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(_tickInterval);
            ApplyDamage();
        }
    }

    private void ApplyDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _radius);
        foreach (var hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null && enemy.CurrentHP > 0)
                enemy.TakeDamage(_damagePerTick);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}