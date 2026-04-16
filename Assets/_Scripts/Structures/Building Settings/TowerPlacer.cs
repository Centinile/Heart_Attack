using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class TowerPlacer : MonoBehaviour
{
    public static TowerPlacer Instance;

    [Header("Tilemaps")]
    public Tilemap placementMap;
    public Tilemap nonPlaceableTiles;
    public Tilemap heartSpawnMap; 

    [Header("Heart Setup")]
    public StructureData heartData;

    [Header("Prefabs")]
    public GameObject ghostPrefab;

    private HashSet<Vector3Int> occupiedTiles = new HashSet<Vector3Int>();
    private GameObject ghostInstance;

    void Awake() => Instance = this;

    void Start() => SpawnHeartAtTargetLocation();

    void Update()
    {
        // 1. Right Click to Cancel Placement
        if (Input.GetMouseButtonDown(1) && TowerSelectionUI.SelectedStructureData != null)
        {
            CancelPlacement();
            return;
        }

        HandlePlacementHover();
        HandlePlacementClick();
    }

    public void CancelPlacement()
    {
        TowerSelectionUI.SelectedStructureData = null;
        if (ghostInstance != null) Destroy(ghostInstance);
    }

    private void HandlePlacementHover()
    {
        if (TowerSelectionUI.SelectedStructureData == null)
        {
            if (ghostInstance != null) Destroy(ghostInstance);
            return;
        }

        if (ghostInstance == null) ghostInstance = Instantiate(ghostPrefab);

        ghostInstance.GetComponent<SpriteRenderer>().sprite = TowerSelectionUI.SelectedStructureData.Icon;

        Vector3 mouseWorldPos = GetMouseWorldPos();
        Vector3Int cellPos = placementMap.WorldToCell(mouseWorldPos);
        Vector3 worldCenter = placementMap.GetCellCenterWorld(cellPos);

        ghostInstance.transform.position = worldCenter + new Vector3(0, placementMap.cellSize.y * 0.25f);

        bool valid = IsTileValid(cellPos);
        ghostInstance.GetComponent<GhostTower>().SetValid(valid);
    }

    private void HandlePlacementClick()
    {
        // Must have data and Left Click
        if (TowerSelectionUI.SelectedStructureData == null || !Input.GetMouseButtonDown(0)) return;
        
        // Ignore if clicking UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Vector3 mouseWorldPos = GetMouseWorldPos();
        Vector3Int cellPos = placementMap.WorldToCell(mouseWorldPos);

        if (!IsTileValid(cellPos)) return;

        StructureData data = TowerSelectionUI.SelectedStructureData;
        
        if (!GameManager.Instance.SpendNutrients(data.NutrientCost)) return;

        // Place the building
        GameObject newBuilding = Instantiate(data.Prefab, ghostInstance.transform.position, Quaternion.identity);
        Building building = newBuilding.GetComponent<Building>();
        
        // Use the ScriptableObject's own configuration logic
        data.ConfigureBuilding(building);
        building.Initialize(data);

        occupiedTiles.Add(cellPos);

        // NOTE: We do NOT set SelectedStructureData to null here, 
        // allowing for continuous placement!
    }

    private bool IsTileValid(Vector3Int cellPos)
    {
        return placementMap.HasTile(cellPos) && 
               !occupiedTiles.Contains(cellPos) && 
               (nonPlaceableTiles == null || !nonPlaceableTiles.HasTile(cellPos));
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        pos.z = 0;
        return pos;
    }

    private void SpawnHeartAtTargetLocation()
    {
        if (heartSpawnMap == null || heartData == null) return;

        foreach (var pos in heartSpawnMap.cellBounds.allPositionsWithin)
        {
            if (heartSpawnMap.HasTile(pos))
            {
                Vector3 worldCenter = heartSpawnMap.GetCellCenterWorld(pos);
                Vector3 spawnPos = worldCenter + new Vector3(0, heartSpawnMap.cellSize.y * 0.25f);

                GameObject heartObj = Instantiate(heartData.Prefab, spawnPos, Quaternion.identity);
                Building building = heartObj.GetComponent<Building>();
                heartData.ConfigureBuilding(building);
                building.Initialize(heartData);

                occupiedTiles.Add(placementMap.WorldToCell(worldCenter));
                return;
            }
        }
    }

    public void FreeTile(Vector3 worldPosition)
    {
        Vector3Int cellPos = placementMap.WorldToCell(worldPosition);
        occupiedTiles.Remove(cellPos);
    }
}