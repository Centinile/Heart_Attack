using UnityEngine;

public class TowerSelectionUI : MonoBehaviour
{
    public static BuildingData SelectedStructureData;

    public void SelectStructure(BuildingData data)
    {
        // Clear existing building selection first
        BuildingSelector selector = Object.FindFirstObjectByType<BuildingSelector>();
        if (selector != null) selector.Deselect();

        // Toggle selection
        if (SelectedStructureData == data)
        {
            SelectedStructureData = null;
        }
        else
        {
            SelectedStructureData = data;
        }
    }
}