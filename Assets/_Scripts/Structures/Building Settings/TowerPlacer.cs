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
    public BuildingData heartData;

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

        ghostInstance.transform.position = new Vector3(
            worldCenter.x,
            worldCenter.y + heartSpawnMap.cellSize.y);

        bool valid = IsPlacementValid(cellPos, TowerSelectionUI.SelectedStructureData);
        ghostInstance.GetComponent<GhostTower>().SetValid(valid);
    }

    private void HandlePlacementClick()
    {
        BuildingData data = TowerSelectionUI.SelectedStructureData;
        if (data == null || !Input.GetMouseButtonDown(0)) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Vector3 mouseWorldPos = GetMouseWorldPos();
        Vector3Int cellPos = placementMap.WorldToCell(mouseWorldPos);

        if (!IsTileValid(cellPos)) return;
        if (!TierUnlockManager.Instance.IsTierUnlocked(data.Tier)) return;

        if (data is ResearchData labData)
        {
            BuildingTier labTier = labData.GetUnlockedTier();
            if (labTier != BuildingTier.Tier1 && TierUnlockManager.Instance.IsLabTierOccupied(labTier))
                return;
        }

        if (!GameManager.Instance.SpendNutrients(data.NutrientCost)) return;

        GameObject newBuilding = Instantiate(data.Prefab, ghostInstance.transform.position, Quaternion.identity);
        Building building = newBuilding.GetComponent<Building>();
        data.ConfigureBuilding(building);
        building.Initialize(data);
        occupiedTiles.Add(cellPos);

        // Select the placed building — this triggers the slide-in via BuildingSelector
        BuildingSelector selector = Object.FindFirstObjectByType<BuildingSelector>();
        if (selector != null) selector.SelectBuildingExternal(building);
    }

    private bool IsTileValid(Vector3Int cellPos)
    {
        return placementMap.HasTile(cellPos) && 
               !occupiedTiles.Contains(cellPos) && 
               (nonPlaceableTiles == null || !nonPlaceableTiles.HasTile(cellPos));
    }

    public bool IsPlacementValid(Vector3Int cellPos, BuildingData data)
    {
        if (!IsTileValid(cellPos)) return false;

        if (!TierUnlockManager.Instance.IsTierUnlocked(data.Tier)) return false;

        if (data is ResearchData labData)
        {
            BuildingTier labTier = labData.GetUnlockedTier();
            if (labTier != BuildingTier.Tier1 && TierUnlockManager.Instance.IsLabTierOccupied(labTier))
                return false;
        }

        return true;
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
                Vector3 spawnPos = new Vector3(
                    worldCenter.x,
                    worldCenter.y + heartSpawnMap.cellSize.y - 0.02f);

                GameObject heartObj = Instantiate(heartData.Prefab, spawnPos, Quaternion.identity);
                Building building = heartObj.GetComponent<Building>();
                heartData.ConfigureBuilding(building);
                building.Initialize(heartData);

                occupiedTiles.Add(placementMap.WorldToCell(worldCenter));
                return;
            }
        }
    }

    public void FreeTile(Vector3Int cellPos)
    {
        occupiedTiles.Remove(cellPos);
    }

    public void FreeTile(Vector3 worldPosition)
    {
        // Undo the y-offset before converting — mirrors how cellPos was captured at placement
        Vector3 corrected = new Vector3(
            worldPosition.x,
            worldPosition.y - heartSpawnMap.cellSize.y,
            worldPosition.z);
        occupiedTiles.Remove(placementMap.WorldToCell(corrected));
    }
}