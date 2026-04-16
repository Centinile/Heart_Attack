using UnityEngine;

public class TowerSelectionUI : MonoBehaviour
{
    public static StructureData SelectedStructureData;

    public void SelectStructure(StructureData data)
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