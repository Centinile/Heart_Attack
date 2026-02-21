using UnityEngine;

/// <summary>
/// Represents the player's main base/heart that must be defended.
/// </summary>
public class Heart : Building
{
    [Header("References")]
    [SerializeField] private HeartData data;
    
    [Header("State")]
    [SerializeField] private bool isDestroyed;
    
    public HeartData Data => data;
    public bool IsDestroyed => isDestroyed;
    
    protected override void Awake()
    {
        base.Awake();
        structureType = StructureType.Heart;
    }
    
    /// <summary>
    /// Configures the heart with data from a ScriptableObject.
    /// </summary>
    public void Configure(HeartData heartData)
    {
        data = heartData;
        maxHP = heartData.MaxHP;
        currentHP = maxHP;
    }
    
    public override void TakeDamage(float damage)
    {
        if (data.Invulnerable) return;
        
        currentHP -= damage;
        
        // Spawn damage effect
        if (data.DamageEffect != null && currentHP < maxHP)
        {
            Instantiate(data.DamageEffect, transform.position, Quaternion.identity);
        }
        
        if (currentHP <= 0 && !isDestroyed)
        {
            isDestroyed = true;
            OnDestroyed();
        }
    }
    
    protected override void OnDestroyed()
    {
        // Game over logic
        Debug.Log("Heart destroyed! Game Over!");
        
        // You can integrate with your game manager here
        // GameManager.Instance.GameOver();
        
        Destroy(gameObject);
    }
    
    public override void OnPlaced()
    {
        // Heart-specific placement logic
        Debug.Log("Heart placed!");
    }
}