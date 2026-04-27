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

    void Start() => actionPanel.SetActive(false);

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
        actionPanel.SetActive(true);
        RefreshRepairUI();
    }

    private void RefreshRepairUI()
    {
        if (repairButton == null) return;

        bool canRepair = SelectedBuilding != null && SelectedBuilding.CanRepair;
        repairButton.SetActive(canRepair);

        if (canRepair && repairCostText != null)
            repairCostText.text = $"Repair ({SelectedBuilding.RepairCost} Nutrients)";
    }

    public void Deselect()
    {
        if (SelectedBuilding != null)
        {
            SelectedBuilding.OnDeselected();
            SelectedBuilding = null;
        }
        actionPanel.SetActive(false);
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