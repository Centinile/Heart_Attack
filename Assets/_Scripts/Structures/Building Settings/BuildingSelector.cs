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

    // ── Stat rows: assign each Label + Value pair in the Inspector ──
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



    // ─────────────────────────────────────────────────────────────────

    void Start()
    {
        actionPanel.SetActive(false);
        if (statsPanel != null) statsPanel.SetActive(false);
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

        // Refresh repair button every frame so cost and visibility
        // stay in sync as the building takes damage while selected
        if (SelectedBuilding != null)
            RefreshRepairUI();
    }

    void SelectBuilding(Building building)
    {
        if (SelectedBuilding == building) return;
        Deselect();

        SelectedBuilding = building;
        SelectedBuilding.OnSelected();

        // Open the whole panel
        actionPanel.SetActive(true);

        RefreshRepairUI();
        RefreshStatsUI();
    }

    // ── Stat helpers ─────────────────────────────────────────────────

    private void RefreshStatsUI()
    {
        if (statsPanel == null || SelectedBuilding == null) return;

        BuildingData data = SelectedBuilding.Data; // expose BuildingData via a public property on Building
        if (data == null) { statsPanel.SetActive(false); return; }

        // Hide every row first, then show only what this building needs
        SetRowVisible(structureNameLabel,         structureNameValue,         false);
        SetRowVisible(maxHPLabel,                 maxHPValue,                 false);
        SetRowVisible(damageLabel,                damageValue,                false);
        SetRowVisible(nutrientCostLabel,          nutrientCostValue,          false);
        SetRowVisible(hydrationCostLabel,         hydrationCostValue,         false);
        SetRowVisible(upgradeCostLabel,           upgradeCostValue,           false);
        SetRowVisible(nutrientsPerWaveLabel,      nutrientsPerWaveValue,      false);
        SetRowVisible(hydrationCapacityBoostLabel,hydrationCapacityBoostValue,false);

        switch (data.GetStructureType())
        {
            case StructureType.Defense:  ShowDefenseStats(data);   break;
            case StructureType.Heart:    ShowHeartStats(data);     break;
            case StructureType.Resource: ShowResourceStats(data);  break;
            case StructureType.Wall:     ShowWallStats(data);      break;
        }

        statsPanel.SetActive(true);
    }

    // Defense towers (Archer, Inferno, etc.): Structure Name, Max HP, Damage, Nutrient Cost, Hydration Cost, Upgrade Cost
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

    // Heart: Max HP, Upgrade Cost
    private void ShowHeartStats(BuildingData d)
    {
        ShowRow(maxHPLabel,       maxHPValue,       "Max HP",       d.MaxHP.ToString());
        ShowRow(upgradeCostLabel, upgradeCostValue, "Upgrade Cost", d.UpgradeCost.ToString());
    }

    // Resource buildings — Mine and Water Pump both use ResourceData
    // Mine:       Max HP, Nutrients Per Wave, Hydration Capacity Boost, Nutrient Cost, Hydration Cost, Upgrade Cost
    // Water Pump: Max HP, Nutrients Per Wave, Hydration Capacity Boost, Upgrade Cost
    private void ShowResourceStats(BuildingData d)
    {
        if (!(d is ResourceData rd)) return;

        ShowRow(maxHPLabel,                  maxHPValue,                  "Max HP",                   d.MaxHP.ToString());
        ShowRow(nutrientsPerWaveLabel,       nutrientsPerWaveValue,       "Nutrients/Wave",           rd.nutrientsPerWave.ToString());
        ShowRow(hydrationCapacityBoostLabel, hydrationCapacityBoostValue, "Hydration Capacity Boost", rd.hydrationCapacityBoost.ToString());

        // Mine also shows costs; Water Pump does not
        if (d.StructureName == "Mine")
        {
            ShowRow(nutrientCostLabel,  nutrientCostValue,  "Nutrient Cost",  d.NutrientCost.ToString());
            ShowRow(hydrationCostLabel, hydrationCostValue, "Hydration Cost", d.HydrationCost.ToString());
        }

        ShowRow(upgradeCostLabel, upgradeCostValue, "Upgrade Cost", d.UpgradeCost.ToString());
    }

    // Wall: Max HP, Nutrient Cost, Upgrade Cost
    private void ShowWallStats(BuildingData d)
    {
        ShowRow(maxHPLabel,        maxHPValue,        "Max HP",        d.MaxHP.ToString());
        ShowRow(nutrientCostLabel, nutrientCostValue, "Nutrient Cost", d.NutrientCost.ToString());
        ShowRow(upgradeCostLabel,  upgradeCostValue,  "Upgrade Cost",  d.UpgradeCost.ToString());
    }

    // ── Row utilities ─────────────────────────────────────────────────

    private void ShowRow(TMP_Text label, TMP_Text value, string labelText, string valueText)
    {
        if (label != null) { label.text = labelText;  label.gameObject.SetActive(true); }
        if (value != null) { value.text = valueText;  value.gameObject.SetActive(true); }
    }

    private void SetRowVisible(TMP_Text label, TMP_Text value, bool visible)
    {
        if (label != null) label.gameObject.SetActive(visible);
        if (value != null) value.gameObject.SetActive(visible);
    }

    // ── Repair UI ─────────────────────────────────────────────────────

    private void RefreshRepairUI()
    {
        if (repairButton == null) return;

        bool canRepair = SelectedBuilding != null && SelectedBuilding.CanRepair;
        repairButton.SetActive(canRepair);

        if (canRepair && repairCostText != null)
            repairCostText.text = $"Repair ({SelectedBuilding.RepairCost} Nutrients)";
    }

    // ── Public button callbacks ───────────────────────────────────────

    public void Deselect()
    {
        if (SelectedBuilding != null)
        {
            SelectedBuilding.OnDeselected();
            SelectedBuilding = null;
        }
        actionPanel.SetActive(false);
        if (statsPanel != null) statsPanel.SetActive(false);
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
}