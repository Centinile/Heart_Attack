using UnityEngine;

[CreateAssetMenu(menuName = "Structure/Research", fileName = "ResearchData_", order = 35)]
public class ResearchData : BuildingData
{
    [Header("Tier Unlocks")]
    [SerializeField] private bool unlocksTier2 = false;
    [SerializeField] private bool unlocksTier3 = false;
    [SerializeField] private bool unlocksTier4 = false;

    public bool UnlocksTier2 => unlocksTier2;
    public bool UnlocksTier3 => unlocksTier3;
    public bool UnlocksTier4 => unlocksTier4;

    public BuildingTier GetUnlockedTier()
    {
        if (unlocksTier4) return BuildingTier.Tier4;
        if (unlocksTier3) return BuildingTier.Tier3;
        if (unlocksTier2) return BuildingTier.Tier2;
        return BuildingTier.Tier1;
    }

    public override StructureType GetStructureType() => StructureType.Research;

    public override void ConfigureBuilding(Building building)
    {
        if (building is ResearchStructure research)
            research.Configure(this);
    }
}

public enum BuildingTier
{
    Tier1 = 1,
    Tier2 = 2,
    Tier3 = 3,
    Tier4 = 4
}