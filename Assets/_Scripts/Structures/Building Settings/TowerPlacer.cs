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
    [Tooltip("Paint a single tile here to designate where the Heart spawns.")]
    public Tilemap heartSpawnMap; 

    [Header("Heart Setup")]
    public StructureData heartData;

    [Header("Prefabs")]
    public GameObject ghostPrefab;

    private HashSet<Vector3Int> occupiedTiles = new HashSet<Vector3Int>();
    private GameObject ghostInstance;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Automatically spawn the heart at the start
        SpawnHeartAtTargetLocation();
    }

    private void SpawnHeartAtTargetLocation()
    {
        if (heartSpawnMap == null || heartData == null)
        {
            Debug.LogWarning("TowerPlacer: HeartSpawnMap or HeartData is missing!");
            return;
        }

        // Scan the heartSpawnMap for the first tile painted
        foreach (var pos in heartSpawnMap.cellBounds.allPositionsWithin)
        {
            if (heartSpawnMap.HasTile(pos))
            {
                // Calculate position (matching your tower offset logic)
                Vector3 worldCenter = heartSpawnMap.GetCellCenterWorld(pos);
                Vector3 spawnPos = worldCenter + new Vector3(0, heartSpawnMap.cellSize.y * 0.25f);

                // Instantiate and Configure
                GameObject heartObj = Instantiate(heartData.Prefab, spawnPos, Quaternion.identity);
                
                Building building = heartObj.GetComponent<Building>();
                heartData.ConfigureBuilding(building);
                building.Initialize(heartData);

                // Register the tile so no towers can be built here
                // We use placementMap.WorldToCell to ensure the coordinate systems match
                Vector3Int placementCell = placementMap.WorldToCell(worldCenter);
                occupiedTiles.Add(placementCell);

                Debug.Log($"Heart spawned at {placementCell}");
                
                // Usually there is only one heart, so we stop after finding the first tile
                return;
            }
        }
    }

    void Update()
    {
        HandlePlacementHover();
        HandlePlacementClick();
    }

    // ... (rest of your HandlePlacementHover remains the same)

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

        // Valid if: On placement map AND NOT non-placeable AND NOT occupied
        bool valid = placementMap.HasTile(cellPos) && 
                     !occupiedTiles.Contains(cellPos) && 
                     (nonPlaceableTiles == null || !nonPlaceableTiles.HasTile(cellPos));

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
        if(nonPlaceableTiles != null && nonPlaceableTiles.HasTile(cellpos)) return;

        StructureData data = TowerSelectionUI.SelectedStructureData;
        
        if (!GameManager.Instance.SpendNutrients(data.NutrientCost))
            return;

        GameObject newBuilding = Instantiate(data.Prefab, ghostInstance.transform.position, Quaternion.identity);

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