using UnityEngine;

public class TowerSelectionUI : MonoBehaviour
{
    public static BuildingData SelectedStructureData;

    [SerializeField] private BuildingSelector buildingSelector; // wire up in inspector

    public void SelectStructure(BuildingData data)
    {
        if (buildingSelector != null) buildingSelector.Deselect(); // slides out if open

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