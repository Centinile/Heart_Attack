using UnityEngine;

public class Heart : Building
{
    [Header("References")]
    [SerializeField] private HeartData heartData;
    
    private bool isGameOverTriggered = false;

    public void Configure(HeartData data)
    {
        heartData = data;
    }

    public override void TakeDamage(float damage)
    {
        if (heartData != null && heartData.Invulnerable) return;

        // Apply damage through base class
        base.TakeDamage(damage);

        // Spawn effect if we are still alive but took a hit
        if (IsAlive && heartData.DamageEffect != null)
        {
            Instantiate(heartData.DamageEffect, transform.position, Quaternion.identity);
        }
    }

    protected override void OnDestroyed()
    {
        if (isGameOverTriggered) return;
        isGameOverTriggered = true;

        Debug.Log("<color=red><b>The Heart has been destroyed!</b></color>");
        
        // Trigger game over logic in GameManager
        GameManager.Instance.GameOver();
        
        base.OnDestroyed();
    }
}