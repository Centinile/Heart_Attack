using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class TowerPlacer : MonoBehaviour
{
    public static TowerPlacer Instance;
    public Tilemap placementMap;
    public Tilemap nonPlaceableTiles;

    public GameObject ghostPrefab;

    private HashSet<Vector3Int> occupiedTiles = new HashSet<Vector3Int>();
    private GameObject ghostInstance;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        HandlePlacementHover();
        HandlePlacementClick();
    }

    void HandlePlacementHover()
    {
        if (TowerSelectionUI.SelectedStructureData == null)
        {
            if (ghostInstance != null)
                Destroy(ghostInstance);
            return;
        }

        if (ghostInstance == null)
            ghostInstance = Instantiate(ghostPrefab);

        ghostInstance.GetComponent<SpriteRenderer>().sprite =
            TowerSelectionUI.SelectedStructureData.Icon;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        Vector3Int cellPos = placementMap.WorldToCell(mouseWorldPos);
        Vector3 worldCenter = placementMap.GetCellCenterWorld(cellPos);

        ghostInstance.transform.position =
            worldCenter + new Vector3(0, placementMap.cellSize.y * 0.25f);

        bool valid = placementMap.HasTile(cellPos) && !occupiedTiles.Contains(cellPos);

        ghostInstance.GetComponent<GhostTower>().SetValid(valid);
    }

    void HandlePlacementClick()
    {
        if(!Input.GetMouseButtonDown(0)) return;
        if (TowerSelectionUI.SelectedStructureData == null) return;

        if(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        Vector3Int cellpos = placementMap.WorldToCell(mouseWorldPos);

        if(!placementMap.HasTile(cellpos)) return;
        if(occupiedTiles.Contains(cellpos)) return;

        StructureData data = TowerSelectionUI.SelectedStructureData;
        GameObject prefab = data.Prefab;

        if (!GameManager.Instance.SpendNutrients(data.NutrientCost))
            return;

        GameObject newBuilding = Instantiate(prefab, ghostInstance.transform.position, Quaternion.identity);

        Building building = newBuilding.GetComponent<Building>();
        data.ConfigureBuilding(building);
        building.Initialize(data);

        occupiedTiles.Add(cellpos); 
    }

    public void FreeTile(Vector3 worldPosition)
    {
        Vector3Int cellPos = placementMap.WorldToCell(worldPosition);

        if (occupiedTiles.Contains(cellPos))
        {
            occupiedTiles.Remove(cellPos);
        }
    }
}
