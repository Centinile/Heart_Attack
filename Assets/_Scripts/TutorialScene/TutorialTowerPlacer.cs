using UnityEngine;

public class TutorialTowerPlacer : MonoBehaviour
{
    public enum PlacementType
    {
        DefenseTower,
        NutrientMine,
        WaterPump
    }

    public PlacementType placementType;
    public int activeDuringPartIndex;

    private TutorialManager tm;

    void Start()
    {
        tm = FindObjectOfType<TutorialManager>();
    }

    public void PlaceTower()
    {
        if (tm == null) return;

        // Prevent wrong triggers
        if (tm.CurrentPart != activeDuringPartIndex) return;

        switch (placementType)
        {
            case PlacementType.DefenseTower:
                tm.OnDefenseTowerPlaced();
                break;

            case PlacementType.NutrientMine:
                tm.OnMinePlaced();
                break;

            case PlacementType.WaterPump:
                tm.OnWaterPumpPlaced();
                break;
        }
    }
}