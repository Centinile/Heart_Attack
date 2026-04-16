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
    private StructureAnimations structureAnimations;

    protected virtual void Awake()
    {
        outline = GetComponent<Outline>();
        if (outline != null) outline.enabled = false; 
        structureAnimations = GetComponent<StructureAnimations>();
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
        structureAnimations?.PlayDeathAnimation();
        if (IsPowered)
        {
            GameManager.Instance.UnregisterPoweredBuilding(this);
            GameManager.Instance.ReleaseHydration(Data.HydrationCost);
        }
        else
        {
            GameManager.Instance.UnregisterUnpoweredBuilding(this);
        }

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
        IsPowered = GameManager.Instance.TryUseHydration(Data.HydrationCost);
        if (IsPowered)
        {
            GameManager.Instance.RegisterPoweredBuilding(this);
            OnPowered();
        }
        else
        {
            GameManager.Instance.RegisterUnpoweredBuilding(this);
            OnUnpowered();
        }
    }

    public void SetPowered(bool powered)
    {
        if (IsPowered == powered) return;
        IsPowered = powered;

        if (powered)
        {
            GameManager.Instance.RegisterPoweredBuilding(this);
            OnPowered();
        }
        else
        {
            GameManager.Instance.UnregisterPoweredBuilding(this);
            GameManager.Instance.RegisterUnpoweredBuilding(this);
            OnUnpowered();
        }
    }

    protected virtual void OnPowered()
    {
        structureAnimations?.ShowPowered();
    }
    protected virtual void OnUnpowered()
    {
        structureAnimations?.ShowUnpowered();
    }

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

        bool wasPowered = IsPowered;
        // We call OnDestroyed to handle the cleanup of old hydration/tiles
        OnDestroyed();

        GameObject newObj = Instantiate(nextData.Prefab, pos, rot);
        Building newBuilding = newObj.GetComponent<Building>();
        
        // Let initialize handle everything
        newBuilding.InitializeWithoutActivation(nextData);
        if (wasPowered && GameManager.Instance.TryUseHydration(nextData.HydrationCost))
        {
            newBuilding.SetPowered(true);
        }
        else
        {
            newBuilding.SetPowered(false);
            GameManager.Instance.RegisterUnpoweredBuilding(newBuilding);
        }


        BuildingSelector.SelectedBuilding = newBuilding;
        newBuilding.OnSelected();
    }

    public virtual void Sell()
    {
        GameManager.Instance.AddNutrients(Data.NutrientCost * 0.5f);
        OnDestroyed(); // Use the common cleanup method
    }

    public void InitializeWithoutActivation(StructureData data)
    {
        Data = data;
        StructureType = data.GetStructureType();
        MaxHP = data.MaxHP;
        CurrentHP = MaxHP;
        // Deliberately does NOT call TryActivate
    }
}