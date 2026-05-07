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
    [SerializeField] private TMP_Text repairCostDisplay;
[SerializeField] private TMP_Text upgradeCostDisplay;

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

    [SerializeField] private TMP_Text descriptionValue;

    // ── Slide animation ────────────────────────────────────────────────
    [Header("Slide Animation")]
    [SerializeField] private float slideDuration = 0.3f;
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private RectTransform _panelRect;
    private Vector2 _restingPosition;
    private Vector2 _hiddenPosition;
    private Coroutine _slideCoroutine;

    // ──────────────────────────────────────────────────────────────────

    void Start()
    {
        _panelRect = actionPanel.GetComponent<RectTransform>();
        _restingPosition = _panelRect.anchoredPosition;

        actionPanel.SetActive(false);
        if (statsPanel != null) statsPanel.SetActive(false);

        StartCoroutine(InitAfterLayout());
    }

    private IEnumerator InitAfterLayout()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        _hiddenPosition = _restingPosition + new Vector2(_panelRect.rect.width, 0f);
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
        {
            RefreshRepairUI();
            RefreshUpgradeUI();
        }
    }

    void SelectBuilding(Building building)
    {
        if (SelectedBuilding == building) return;

        if (SelectedBuilding != null)
        {
            SelectedBuilding.OnDeselected();
            SelectedBuilding = null;
        }

        SelectedBuilding = building;
        SelectedBuilding.OnSelected();

        RefreshRepairUI();
        PopulateStats(building.Data);
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
            elapsed += Time.unscaledDeltaTime;
            float t = slideCurve.Evaluate(Mathf.Clamp01(elapsed / slideDuration));
            _panelRect.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
            yield return null;
        }
        _panelRect.anchoredPosition = to;
        onComplete?.Invoke();
    }

    // ── Stat helpers ───────────────────────────────────────────────────

    // Single source of truth for populating the stats panel from any BuildingData
    private void PopulateStats(BuildingData data)
    {
        if (statsPanel == null || data == null)
        {
            if (statsPanel != null) statsPanel.SetActive(false);
            return;
        }

        // Hide all rows first
        SetRowVisible(structureNameLabel,          structureNameValue,          false);
        SetRowVisible(maxHPLabel,                  maxHPValue,                  false);
        SetRowVisible(damageLabel,                 damageValue,                 false);
        SetRowVisible(nutrientCostLabel,           nutrientCostValue,           false);
        SetRowVisible(hydrationCostLabel,          hydrationCostValue,          false);
        SetRowVisible(upgradeCostLabel,            upgradeCostValue,            false);
        SetRowVisible(nutrientsPerWaveLabel,       nutrientsPerWaveValue,       false);
        SetRowVisible(hydrationCapacityBoostLabel, hydrationCapacityBoostValue, false);

        // Description — shown for all types
        if (descriptionValue != null)
        {
            descriptionValue.text = data.Description;
            descriptionValue.gameObject.SetActive(!string.IsNullOrEmpty(data.Description));
        }

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

        ShowRow(maxHPLabel,                  maxHPValue,                  "Max HP",                  d.MaxHP.ToString());
        ShowRow(nutrientsPerWaveLabel,       nutrientsPerWaveValue,       "Nutrients/Wave",           rd.nutrientsPerWave.ToString());
        ShowRow(hydrationCapacityBoostLabel, hydrationCapacityBoostValue, "Hydration Capacity Boost", rd.hydrationCapacityBoost.ToString());

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

        if (repairCostDisplay != null)
        {
            if (SelectedBuilding == null || !SelectedBuilding.IsAlive)
            {
                repairCostDisplay.gameObject.SetActive(false);
            }
            else if (SelectedBuilding.HPPercent >= 1f)
            {
                repairCostDisplay.text = "Full HP";
                repairCostDisplay.gameObject.SetActive(true);
            }
            else
            {
                repairCostDisplay.text = $"Repair Cost: {SelectedBuilding.RepairCost} Nutrients";
                repairCostDisplay.gameObject.SetActive(true);
            }
        }
    }

    private void RefreshUpgradeUI()
    {
        if (upgradeCostDisplay == null) return;

        if (SelectedBuilding == null)
        {
            upgradeCostDisplay.gameObject.SetActive(false);
            return;
        }

        if (SelectedBuilding.Data.NextLevelData == null)
        {
            upgradeCostDisplay.text = "Max Level";
            upgradeCostDisplay.gameObject.SetActive(true);
        }
        else
        {
            upgradeCostDisplay.text = $"Upgrade Cost: {SelectedBuilding.Data.UpgradeCost} Nutrients";
            upgradeCostDisplay.gameObject.SetActive(true);
        }
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

    // Called from TowerSelectionUI when hovering/selecting from the build menu
    public void ShowDataStats(BuildingData data)
    {
        PopulateStats(data);
        SlideIn();
    }
}