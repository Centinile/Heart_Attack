using System;
using System.Collections.Generic;
using UnityEngine;

public class TierUnlockManager : MonoBehaviour
{
    public static TierUnlockManager Instance { get; private set; }

    // Tracks which tiers are currently unlocked by an active Lab
    private Dictionary<BuildingTier, ResearchStructure> _activeLabs = new();

    public static event Action OnTierUnlocksChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // Returns true if registration succeeded, false if a Lab already owns this tier
    public bool TryRegisterLab(ResearchStructure lab, BuildingTier tier)
    {
        if (_activeLabs.ContainsKey(tier))
            return false;

        _activeLabs[tier] = lab;
        OnTierUnlocksChanged?.Invoke();
        return true;
    }

    public void UnregisterLab(BuildingTier tier)
    {
        if (!_activeLabs.ContainsKey(tier)) return;
        _activeLabs.Remove(tier);
        OnTierUnlocksChanged?.Invoke();
    }

    public bool IsTierUnlocked(BuildingTier tier)
    {
        // Tier 1 is always unlocked — no Lab needed
        if (tier == BuildingTier.Tier1) return true;
        return _activeLabs.ContainsKey(tier);
    }

    public bool IsLabTierOccupied(BuildingTier tier) => _activeLabs.ContainsKey(tier);
}