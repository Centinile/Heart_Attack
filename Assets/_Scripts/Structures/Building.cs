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
    public StructureData Data { get; private set; }
    public int Level { get; protected set; } = 1;
    public bool IsPowered { get; protected set; }
    
    public StructureType StructureType { get; private set; }
    public float MaxHP { get; private set; }
    public float CurrentHP { get; private set; }
    public float HPPercent => MaxHP > 0 ? CurrentHP / MaxHP : 0f;
    public bool IsAlive => CurrentHP > 0;

    private Outline outline;

    protected virtual void Awake()
    {
        outline = GetComponent<Outline>();
        if (outline != null) outline.enabled = false; 
    }

    public virtual void Initialize(StructureData data)
    {
        Data = data;
        StructureType = data.GetStructureType();
        MaxHP = data.MaxHP;
        CurrentHP = MaxHP;

        TryActivate();
    }

    public virtual void TakeDamage(float damage)
    {
        CurrentHP = Mathf.Max(0, CurrentHP - damage);
        if (CurrentHP <= 0) OnDestroyed();
    }

    public virtual void Heal(float amount)
    {
        CurrentHP = Mathf.Min(CurrentHP + amount, MaxHP);
    }

    protected virtual void OnDestroyed()
    {
        // One-stop shop for cleanup
        GameManager.Instance.ReleaseHydration(Data.HydrationCost);

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
        // This is good logic, ensures hydration is managed on spawn
        IsPowered = GameManager.Instance.TryUseHydration(Data.HydrationCost);
        if (IsPowered) OnPowered(); else OnUnpowered();
    }

    protected virtual void OnPowered() { }
    protected virtual void OnUnpowered() { }

    public virtual void OnSelected() { if (outline != null) outline.enabled = true; }
    public virtual void OnDeselected() { if (outline != null) outline.enabled = false; }

    public virtual void Upgrade()
    {
        if (Data.NextLevelData == null || !GameManager.Instance.SpendNutrients(Data.UpgradeCost))
            return;

        StructureData nextData = Data.NextLevelData;

        // Capture transform before destruction
        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;

        // We call OnDestroyed to handle the cleanup of old hydration/tiles
        OnDestroyed();

        GameObject newObj = Instantiate(nextData.Prefab, pos, rot);
        Building newBuilding = newObj.GetComponent<Building>();
        
        // Let initialize handle everything
        newBuilding.Initialize(nextData);

        BuildingSelector.SelectedBuilding = newBuilding;
        newBuilding.OnSelected();
    }

    public virtual void Sell()
    {
        GameManager.Instance.AddNutrients(Data.NutrientCost * 0.5f);
        OnDestroyed(); // Use the common cleanup method
    }
}