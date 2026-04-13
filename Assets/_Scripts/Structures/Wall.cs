using UnityEngine;
using UnityEngine.AI;

public class Wall : Building
{
    [Header("References")]
    [SerializeField] private WallData wallData;
    
    [Header("NavMesh Settings")]
    [SerializeField] private float carveSizeMultiplier = 0.8f;
    private NavMeshObstacle navMeshObstacle;

    public void Configure(WallData data)
    {
        wallData = data;
        // Logic specific to wall data initialization goes here
    }

    public override void OnPlaced()
    {
        base.OnPlaced();
        SetupNavMesh();
    }

    private void SetupNavMesh()
    {
        if (navMeshObstacle == null) navMeshObstacle = gameObject.AddComponent<NavMeshObstacle>();
        
        navMeshObstacle.shape = NavMeshObstacleShape.Box;
        navMeshObstacle.carving = true;

        if (TryGetComponent(out BoxCollider2D box))
        {
            // BoxCollider2D is XY, NavMeshObstacle uses XYZ
            navMeshObstacle.size = new Vector3(box.size.x * carveSizeMultiplier, box.size.y * carveSizeMultiplier, 1f);
        }
    }

    public override void TakeDamage(float damage)
    {
        // Use the reduction from the wall data
        float reduction = wallData != null ? wallData.RangedDamageReduction : 0;
        base.TakeDamage(damage * (1f - reduction));
    }

    protected override void OnDestroyed()
    {
        Debug.Log("Wall breached!");
        base.OnDestroyed(); // Frees tile and handles hydration
    }
}