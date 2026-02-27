using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingSelector : MonoBehaviour
{
    public static Building SelectedBuilding;
    [Header("Action UI")]
    public GameObject actionPanel;

    [Header("Layer Mask")]
    public LayerMask buildingLayer; // Only click objects on this layer

    void Start()
    {
        actionPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Debug.Log($"[BuildingSelector] Mouse clicked at {mousePos}");

            // Only hit colliders on buildingLayer
            Collider2D hitCollider = Physics2D.OverlapPoint(mousePos, buildingLayer);

            if (hitCollider != null)
            {
                Building building = hitCollider.GetComponent<Building>();
                if (building != null)
                {
                    Debug.Log($"[BuildingSelector] Selected building: {building.Data.StructureName}");
                    SelectBuilding(building);
                    return;
                }
            }

            Debug.Log("[BuildingSelector] No building clicked, deselecting");
            Deselect();
        }
    }

    void SelectBuilding(Building building)
    {
        TowerSelectionUI.SelectedStructureData = null;

        if (SelectedBuilding == building)
            return;

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