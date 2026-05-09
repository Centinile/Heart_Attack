using System.Collections;
using UnityEngine;

public class DOTZone : MonoBehaviour
{
    private float _damagePerTick;
    private float _tickInterval;
    private float _radius;

    private ParticleSystem _particleEffect;

    public void Initialize(float damagePerTick, float tickInterval, float duration, float radius)
    {
        _damagePerTick = damagePerTick;
        _tickInterval  = tickInterval;
        _radius        = radius;

        _particleEffect = GetComponentInChildren<ParticleSystem>();
        if (_particleEffect != null)
        {
            _particleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _particleEffect.main;
            main.loop = true;
            _particleEffect.Play();
        }

        StartCoroutine(TickRoutine(duration));
        Destroy(gameObject, duration);
    }

    private IEnumerator TickRoutine(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return new WaitForSeconds(_tickInterval);
            elapsed += _tickInterval;
            ApplyDamage();
        }
    }

    private void ApplyDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _radius);
        foreach (var hit in hits)
        {
            EnemyBrain enemy = hit.GetComponent<EnemyBrain>();
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