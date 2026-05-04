using UnityEngine;

public class TutorialTowerPlacer : MonoBehaviour
{
    public GameObject towerPrefab;
    public TutorialManager tutorialManager;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PlaceTower();
        }
    }

    void PlaceTower()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0f;

        Instantiate(towerPrefab, mousePosition, Quaternion.identity);

        if (tutorialManager != null)
        {
            tutorialManager.TowerPlaced();
        }
    }
}