using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class TowerPlacer : MonoBehaviour
{
    public Tilemap placementMap;
    public Tilemap nonPlaceableTiles;

    public GameObject ghostPrefab;

    private HashSet<Vector3Int> occupiedTiles = new HashSet<Vector3Int>();
    private GameObject ghostInstance;

    void Update()
    {
        HandlePlacementHover();
        HandlePlacementClick();
    }

    void HandlePlacementHover()
    {
        if(TowerSelectionUI.SelectedTowerPrefab == null)
        {
            if (ghostInstance != null)
                Destroy(ghostInstance);
            return;
        }

        if(ghostInstance == null)
            ghostInstance = Instantiate(ghostPrefab);

        ghostInstance.GetComponent<SpriteRenderer>().sprite = TowerSelectionUI.SelectedTowerPrefab.GetComponent<SpriteRenderer>().sprite;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        Vector3Int cellPos = placementMap.WorldToCell(mouseWorldPos);

        Vector3 worldCenter = placementMap.GetCellCenterWorld(cellPos);
        worldCenter.z = 0;

        ghostInstance.transform.position = worldCenter + new Vector3(0, placementMap.cellSize.y * 0.25f);

        bool valid = placementMap.HasTile(cellPos) && !occupiedTiles.Contains(cellPos);

        ghostInstance.GetComponent<GhostTower>().SetValid(valid);
    }

    void HandlePlacementClick()
    {
        if(!Input.GetMouseButtonDown(0)) return;
        if (TowerSelectionUI.SelectedTowerPrefab == null) return;

        if(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        Vector3Int cellpos = placementMap.WorldToCell(mouseWorldPos);

        if(!placementMap.HasTile(cellpos)) return;
        if(occupiedTiles.Contains(cellpos)) return;

        Instantiate(TowerSelectionUI.SelectedTowerPrefab, ghostInstance.transform.position, Quaternion.identity);

        TowerSelectionUI.SelectedTowerPrefab = null;

        occupiedTiles.Add(cellpos);
        
    }
}
