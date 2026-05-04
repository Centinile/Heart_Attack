using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class BuildingSelector : MonoBehaviour
{
    public static Building SelectedBuilding;

    [Header("Action UI")]
    public GameObject actionPanel;
    public LayerMask buildingLayer;

    [Header("Repair UI")]
    [SerializeField] private GameObject repairButton;
    [SerializeField] private TMP_Text repairCostText;

    [Header("Stats Display")]
    [SerializeField] private GameObject statsPanel;

    // ── Stat rows ──────────────────────────────────────────────────────
    [SerializeField] private TMP_Text structureNameLabel;
    [SerializeField] private TMP_Text structureNameValue;

    [SerializeField] private TMP_Text maxHPLabel;
    [SerializeField] private TMP_Text maxHPValue;

    [SerializeField] private TMP_Text damageLabel;
    [SerializeField] private TMP_Text damageValue;

    [SerializeField] private TMP_Text nutrientCostLabel;
    [SerializeField] private TMP_Text nutrientCostValue;

    [SerializeField] private TMP_Text hydrationCostLabel;
    [SerializeField] private TMP_Text hydrationCostValue;

    [SerializeField] private TMP_Text upgradeCostLabel;
    [SerializeField] private TMP_Text upgradeCostValue;

    [SerializeField] private TMP_Text nutrientsPerWaveLabel;
    [SerializeField] private TMP_Text nutrientsPerWaveValue;

    [SerializeField] private TMP_Text hydrationCapacityBoostLabel;
    [SerializeField] private TMP_Text hydrationCapacityBoostValue;

    // ── Slide animation ────────────────────────────────────────────────
    [Header("Slide Animation")]
    [SerializeField] private float slideDuration = 0.3f;
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private RectTransform _panelRect;
    private Vector2 _restingPosition;  // where the panel sits when fully visible
    private Vector2 _hiddenPosition;   // off-screen to the right
    private Coroutine _slideCoroutine;

    // ──────────────────────────────────────────────────────────────────

    void Start()
    {
        _panelRect = actionPanel.GetComponent<RectTransform>();

        // Record the position set in the editor — that is the fully-open resting spot.
        _restingPosition = _panelRect.anchoredPosition;

        // Hide immediately so it doesn't flash, then defer the width calculation
        // to the end of the first frame when Unity has finished its layout pass.
        actionPanel.SetActive(false);
        if (statsPanel != null) statsPanel.SetActive(false);

        StartCoroutine(InitAfterLayout());
    }

    private IEnumerator InitAfterLayout()
    {
        // Wait one frame so RectTransform.rect.width is populated correctly.
        yield return null;

        Canvas.ForceUpdateCanvases();

        // Hidden = resting position shifted right by the panel's own width.
        _hiddenPosition = _restingPosition + new Vector2(_panelRect.rect.width, 0f);

        // Snap to hidden position while the panel is still inactive.
        _panelRect.anchoredPosition = _hiddenPosition;
    }

    void Update()
    {
        if (TowerSelectionUI.SelectedStructureData != null) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(mousePos, buildingLayer);

            if (hit != null)
            {
                Building building = hit.GetComponent<Building>();
                if (building != null)
                {
                    SelectBuilding(building);
                    return;
                }
            }
            Deselect();
        }

        if (SelectedBuilding != null)
            RefreshRepairUI();
    }

    void SelectBuilding(Building building)
    {
        if (SelectedBuilding == building) return;

        // Swap without sliding out first — just update content and slide in.
        if (SelectedBuilding != null)
        {
            SelectedBuilding.OnDeselected();
            SelectedBuilding = null;
        }

        SelectedBuilding = building;
        SelectedBuilding.OnSelected();

        RefreshRepairUI();
        RefreshStatsUI();
        SlideIn();
    }

    // ── Slide ──────────────────────────────────────────────────────────

    private void SlideIn()
    {
        if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
        actionPanel.SetActive(true);
        _slideCoroutine = StartCoroutine(
            SlideCoroutine(_panelRect.anchoredPosition, _restingPosition));
    }

    private void SlideOut(System.Action onComplete = null)
    {
        if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
        _slideCoroutine = StartCoroutine(
            SlideCoroutine(_panelRect.anchoredPosition, _hiddenPosition, () =>
            {
                actionPanel.SetActive(false);
                onComplete?.Invoke();
            }));
    }

    private IEnumerator SlideCoroutine(Vector2 from, Vector2 to, System.Action onComplete = null)
    {
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;  // unscaled so it works during pause menus
            float t = slideCurve.Evaluate(Mathf.Clamp01(elapsed / slideDuration));
            _panelRect.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
            yield return null;
        }
        _panelRect.anchoredPosition = to;
        onComplete?.Invoke();
    }

    // ── Stat helpers ───────────────────────────────────────────────────

    private void RefreshStatsUI()
    {
        if (statsPanel == null || SelectedBuilding == null) return;

        BuildingData data = SelectedBuilding.Data;
        if (data == null) { statsPanel.SetActive(false); return; }

        SetRowVisible(structureNameLabel,          structureNameValue,          false);
        SetRowVisible(maxHPLabel,                  maxHPValue,                  false);
        SetRowVisible(damageLabel,                 damageValue,                 false);
        SetRowVisible(nutrientCostLabel,           nutrientCostValue,           false);
        SetRowVisible(hydrationCostLabel,          hydrationCostValue,          false);
        SetRowVisible(upgradeCostLabel,            upgradeCostValue,            false);
        SetRowVisible(nutrientsPerWaveLabel,       nutrientsPerWaveValue,       false);
        SetRowVisible(hydrationCapacityBoostLabel, hydrationCapacityBoostValue, false);

        switch (data.GetStructureType())
        {
            case StructureType.Defense:  ShowDefenseStats(data);  break;
            case StructureType.Heart:    ShowHeartStats(data);    break;
            case StructureType.Resource: ShowResourceStats(data); break;
            case StructureType.Wall:     ShowWallStats(data);     break;
        }

        statsPanel.SetActive(true);
    }

    private void ShowDefenseStats(BuildingData d)
    {
        ShowRow(structureNameLabel, structureNameValue, "Structure",      d.StructureName);
        ShowRow(maxHPLabel,         maxHPValue,         "Max HP",         d.MaxHP.ToString());
        if (d is DefenseData dd)
            ShowRow(damageLabel, damageValue, "Damage", dd.damage.ToString());
        ShowRow(nutrientCostLabel,  nutrientCostValue,  "Nutrient Cost",  d.NutrientCost.ToString());
        ShowRow(hydrationCostLabel, hydrationCostValue, "Hydration Cost", d.HydrationCost.ToString());
        ShowRow(upgradeCostLabel,   upgradeCostValue,   "Upgrade Cost",   d.UpgradeCost.ToString());
    }

    private void ShowHeartStats(BuildingData d)
    {
        ShowRow(maxHPLabel,       maxHPValue,       "Max HP",       d.MaxHP.ToString());
        ShowRow(upgradeCostLabel, upgradeCostValue, "Upgrade Cost", d.UpgradeCost.ToString());
    }

    private void ShowResourceStats(BuildingData d)
    {
        if (!(d is ResourceData rd)) return;

        ShowRow(maxHPLabel,                  maxHPValue,                  "Max HP",                   d.MaxHP.ToString());
        ShowRow(nutrientsPerWaveLabel,       nutrientsPerWaveValue,       "Nutrients/Wave",            rd.nutrientsPerWave.ToString());
        ShowRow(hydrationCapacityBoostLabel, hydrationCapacityBoostValue, "Hydration Capacity Boost",  rd.hydrationCapacityBoost.ToString());

        // Mine also shows costs; Water Pump does not
        if (d.StructureName == "Mine")
        {
            ShowRow(nutrientCostLabel,  nutrientCostValue,  "Nutrient Cost",  d.NutrientCost.ToString());
            ShowRow(hydrationCostLabel, hydrationCostValue, "Hydration Cost", d.HydrationCost.ToString());
        }

        ShowRow(upgradeCostLabel, upgradeCostValue, "Upgrade Cost", d.UpgradeCost.ToString());
    }

    private void ShowWallStats(BuildingData d)
    {
        ShowRow(maxHPLabel,        maxHPValue,        "Max HP",        d.MaxHP.ToString());
        ShowRow(nutrientCostLabel, nutrientCostValue, "Nutrient Cost", d.NutrientCost.ToString());
        ShowRow(upgradeCostLabel,  upgradeCostValue,  "Upgrade Cost",  d.UpgradeCost.ToString());
    }

    // ── Row utilities ──────────────────────────────────────────────────

    private void ShowRow(TMP_Text label, TMP_Text value, string labelText, string valueText)
    {
        if (label != null) { label.text = labelText; label.gameObject.SetActive(true); }
        if (value != null) { value.text = valueText; value.gameObject.SetActive(true); }
    }

    private void SetRowVisible(TMP_Text label, TMP_Text value, bool visible)
    {
        if (label != null) label.gameObject.SetActive(visible);
        if (value != null) value.gameObject.SetActive(visible);
    }

    // ── Repair UI ──────────────────────────────────────────────────────

    private void RefreshRepairUI()
    {
        if (repairButton == null) return;
        bool canRepair = SelectedBuilding != null && SelectedBuilding.CanRepair;
        repairButton.SetActive(canRepair);
        if (canRepair && repairCostText != null)
            repairCostText.text = $"Repair ({SelectedBuilding.RepairCost} Nutrients)";
    }

    // ── Public button callbacks ────────────────────────────────────────

    public void Deselect()
    {
        if (SelectedBuilding != null)
        {
            SelectedBuilding.OnDeselected();
            SelectedBuilding = null;
        }
        if (statsPanel != null) statsPanel.SetActive(false);
        SlideOut();
    }

    public void UpgradeSelected()
    {
        if (SelectedBuilding == null) return;
        SelectedBuilding.Upgrade();
    }

    public void SellSelected()
    {
        if (SelectedBuilding == null) return;
        SelectedBuilding.Sell();
        Deselect();
    }

    public void RepairSelected()
    {
        if (SelectedBuilding == null) return;
        SelectedBuilding.Repair();
        RefreshRepairUI();
    }

    public void SelectBuildingExternal(Building building)
    {
        SelectBuilding(building);
    }
}