using UnityEngine;

public class TowerSelectionUI : MonoBehaviour
{
    public static BuildingData SelectedStructureData;

    public void SelectStructure(BuildingData data)
    {
        BuildingSelector selector = Object.FindFirstObjectByType<BuildingSelector>();

        if (SelectedStructureData == data)
        {
            // Toggling off — deselect and slide out
            SelectedStructureData = null;
            selector?.Deselect();
            return;
        }

        // Deselect any placed building first
        if (BuildingSelector.SelectedBuilding != null)
            selector?.Deselect();

        SelectedStructureData = data;
        selector?.ShowDataStats(data);
    }
}