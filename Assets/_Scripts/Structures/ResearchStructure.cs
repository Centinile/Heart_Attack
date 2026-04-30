using UnityEngine;

public class ResearchStructure : Building
{
    private ResearchData _researchData;
    private BuildingTier _ownedTier = BuildingTier.Tier1;
    private bool _tierRegistered = false;

    public void Configure(ResearchData data)
    {
        _researchData = data;
    }

    public override void Initialize(BuildingData data)
    {
        base.Initialize(data);

        if (_researchData == null) return;

        BuildingTier tier = _researchData.GetUnlockedTier();
        if (tier == BuildingTier.Tier1) return;

        bool success = TierUnlockManager.Instance.TryRegisterLab(this, tier);
        if (success)
        {
            _ownedTier = tier;
            _tierRegistered = true;
        }
        else
        {
            Debug.LogWarning($"[Research] Failed to register tier {tier} — already occupied.");
        }
    }

    protected override void OnDestroyed()
    {
        if (_tierRegistered)
            TierUnlockManager.Instance.UnregisterLab(_ownedTier);

        base.OnDestroyed();
    }
}