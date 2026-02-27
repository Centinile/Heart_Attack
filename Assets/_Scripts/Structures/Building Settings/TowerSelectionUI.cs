using UnityEngine;

public class TowerSelectionUI : MonoBehaviour
{
    public static StructureData SelectedStructureData;

    public void SelectStructure(StructureData data)
    {
        if (SelectedStructureData == data)
        {
            SelectedStructureData = null;
            return;
        }

        // Cancel building selection properly
        Object.FindAnyObjectByType<BuildingSelector>().Deselect();

        SelectedStructureData = data;
    }
}
