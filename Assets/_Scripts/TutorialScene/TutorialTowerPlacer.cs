using UnityEngine;
using UnityEngine.EventSystems;

public class TutorialTowerPlacer : MonoBehaviour
{
    public enum PlacementType { DefenseTower, Mine, WaterPump }

    [Header("Placement")]
    public GameObject towerPrefab;
    public PlacementType placementType = PlacementType.DefenseTower;

    [Header("References")]
    public TutorialManager tutorialManager;

    // Which tutorial part index this placer is active on
    // Defense = 2, Mine = 5, WaterPump = 7
    [Header("Gate")]
    [Tooltip("The tutorial part index (0-based) during which this placer is active.")]
    public int activeDuringPartIndex = 2;

    private bool _placed = false; // only allow one placement per gate

    void Update()
    {
        if (_placed) return;
        if (tutorialManager == null) return;

        // Only active during the correct part
        // Access current part via the public field or static instance
        TutorialManager tm = TutorialManager.Instance;
        if (tm == null) return;

        // Don't place if over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (Input.GetMouseButtonDown(0))
        {
            // Let TutorialManager.Update() handle click-to-advance on non-gated parts.
            // Only intercept when we're on the correct gated part.
            if (tm.CurrentPart != activeDuringPartIndex) return;

            // Only place if typing is done
            if (tm.IsTyping) return;

            PlaceTower();
        }
    }

    void PlaceTower()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        if (towerPrefab != null)
            Instantiate(towerPrefab, mousePos, Quaternion.identity);

        _placed = true;

        TutorialManager tm = TutorialManager.Instance;
        if (tm == null) return;

        switch (placementType)
        {
            case PlacementType.DefenseTower: tm.OnDefenseTowerPlaced(); break;
            case PlacementType.Mine:         tm.OnMinePlaced();         break;
            case PlacementType.WaterPump:    tm.OnWaterPumpPlaced();    break;
        }
    }
}