using UnityEngine;

public enum StructureType
{
    Heart,
    Resource,
    Defense,
    Wall
}

/// Defines how a defense tower selects its target.
public enum TargetMode
{
    ClosestToHeart,
    FarthestFromHeart,
    ClosestToTower,
    //Targets the first enemy that entered range.
    First
}

/// Defines the type of attack a defense tower performs.
public enum AttackType
{
    /// Damages a single target.
    SingleTarget,
    /// Damages multiple targets (up to a limit).
    MultiTarget,
    /// Damages the target and nearby enemies.
    SplashDamage,
    /// Damages all enemies in range.
    AllInRange
}


/// Base class for all buildings/structures in the game.
/// Implements common functionality shared by all structure types.
public class Building : MonoBehaviour
{
    [Header("Building Info")]
    [SerializeField] protected StructureType structureType;
    
    [Header("Health")]
    [SerializeField] protected float maxHP = 100f;
    [SerializeField] protected float currentHP;

    [Header("Cost")]
    [SerializeField] protected float nutrientCost = 0f;
    [SerializeField] protected float hydrationCost = 0f;

    private Outline outline;

    // Properties for external access
    protected StructureData structureData;
    protected int level = 1;
    protected bool isPowered = false;

    public StructureData Data => structureData;
    public int Level => level;
    public bool IsPowered => isPowered;
    
    public StructureType StructureType => structureType;
    public float MaxHP => maxHP;
    public float CurrentHP => currentHP;
    public float HPPercent => maxHP > 0 ? currentHP / maxHP : 0f;
    public bool IsAlive => currentHP > 0;

    //Initialize
    public virtual void Initialize(StructureData data)
    {
        structureData = data;
        structureType = data.GetStructureType();
        SetHealth(data.MaxHP);

        TryActivate();
    }
    
    /// Sets the health values for this building.
    /// <param name="newMaxHP">The maximum health value.</param>
    /// <param name="newCurrentHP">The current health value (defaults to maxHP).</param>
    public void SetHealth(float newMaxHP, float? newCurrentHP = null)
    {
        maxHP = newMaxHP;
        currentHP = newCurrentHP ?? newMaxHP;
    }

    protected virtual void Awake()
    {
        outline = GetComponent<Outline>();
        if (outline != null)
            outline.enabled = false; 

        currentHP = maxHP;
    }
    
    /// Applies damage to the building.
    /// <param name="damage">Amount of damage to deal.</param>
    public virtual void TakeDamage(float damage)
    {
        currentHP -= damage;
        
        if (currentHP <= 0)
        {
            currentHP = 0;
            OnDestroyed();
        }
    }
    
    /// Heals the building by the specified amount.
    /// <param name="amount">Amount to heal.</param>
    public virtual void Heal(float amount)
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);
    }
    
    /// Called when the building is destroyed.
    /// Override in subclasses for custom destruction logic.
    protected virtual void OnDestroyed()
    {
        GameManager.Instance.ReleaseHydration(structureData.HydrationCost);

        if (TowerPlacer.Instance != null)
            TowerPlacer.Instance.FreeTile(transform.position);
        Destroy(gameObject);
    }
    
    
    /// Called when the building is placed on the map.
    /// Override to perform initialization.
    public virtual void OnPlaced()
    {
        
    }

    protected void TryActivate()
    {
        if (GameManager.Instance.TryUseHydration(structureData.HydrationCost))
        {
            isPowered = true;
            OnPowered();
        }
        else
        {
            isPowered = false;
            OnUnpowered();
        }
    }

    protected virtual void OnPowered() { }

    protected virtual void OnUnpowered() { }
    public virtual void OnSelected()
    {
        if (outline != null)
            outline.enabled = true;
    }

    public virtual void OnDeselected()
    {
        if (outline != null)
            outline.enabled = false;
    }

    public virtual void Upgrade()
    {
        if (structureData.NextLevelData == null)
            return;

        if (!GameManager.Instance.SpendNutrients(structureData.UpgradeCost))
            return;

        StructureData nextData = structureData.NextLevelData;

        Vector3 spawnPosition = transform.position;
        Quaternion spawnRotation = transform.rotation;

        // Release old hydration
        GameManager.Instance.ReleaseHydration(structureData.HydrationCost);

        // Destroy current building
        Destroy(gameObject);

        // Spawn upgraded prefab
        GameObject newBuildingObj = Instantiate(nextData.Prefab, spawnPosition, spawnRotation);

        Building newBuilding = newBuildingObj.GetComponent<Building>();

        nextData.ConfigureBuilding(newBuilding);
        newBuilding.Initialize(nextData);

        //auto-select new building
        BuildingSelector.SelectedBuilding = newBuilding;
        newBuilding.OnSelected();
}

    public virtual void Sell()
    {
        float refund = structureData.NutrientCost * 0.5f;

        GameManager.Instance.AddNutrients(refund);
        GameManager.Instance.ReleaseHydration(structureData.HydrationCost);

        if (TowerPlacer.Instance != null)
            TowerPlacer.Instance.FreeTile(transform.position);

        Destroy(gameObject);
    }
    
}