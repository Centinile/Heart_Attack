using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingSelector : MonoBehaviour
{
    public static Building SelectedBuilding;
    [Header("Action UI")]
    public GameObject actionPanel;
    public LayerMask buildingLayer;

    void Start() => actionPanel.SetActive(false);

    void Update()
    {
        // 1. If we are currently placing a tower, selection is disabled
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
    }

    void SelectBuilding(Building building)
    {
        if (SelectedBuilding == building) return;
        Deselect();

        SelectedBuilding = building;
        SelectedBuilding.OnSelected();
        actionPanel.SetActive(true);
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
}