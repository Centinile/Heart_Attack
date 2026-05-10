using UnityEngine;
using UnityEngine.AI;

public class Wall : Building
{
    [Header("References")]
    [SerializeField] private WallData wallData; 
    
    [Header("NavMesh Settings")]
    [SerializeField] private float carveSizeMultiplier = 0.8f;
    private NavMeshObstacle navMeshObstacle;

    protected override void Awake()
    {
        base.Awake();
    }

    public void Configure(WallData data)
    {
        wallData = data;
    }

    public override void OnPlaced()
    {
        base.OnPlaced();
        SetupNavMesh();
    }

    private void SetupNavMesh()
    {
        if (navMeshObstacle == null) 
            navMeshObstacle = gameObject.AddComponent<NavMeshObstacle>();
        
        navMeshObstacle.shape = NavMeshObstacleShape.Box;
        navMeshObstacle.carving = true;

        if (TryGetComponent(out BoxCollider2D box))
        {
            navMeshObstacle.size = new Vector3(box.size.x * carveSizeMultiplier, box.size.y * carveSizeMultiplier, 1f);
        }
    }

    protected override void OnDestroyed()
    {
        //Debug.Log("Wall breached!");
        base.OnDestroyed(); 
    }
}