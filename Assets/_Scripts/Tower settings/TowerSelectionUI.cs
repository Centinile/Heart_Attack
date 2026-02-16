using UnityEngine;

public class TowerSelectionUI : MonoBehaviour
{
    public static GameObject SelectedTowerPrefab;

    public void SelectTower(GameObject towerPrefab)
    {
        if(towerPrefab == SelectedTowerPrefab)
        {
            SelectedTowerPrefab = null;
            return;
        }

        SelectedTowerPrefab = towerPrefab;
    }
}
