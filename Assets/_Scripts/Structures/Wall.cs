using UnityEngine;
using UnityEngine.AI;

/// Represents a defensive wall structure.
/// Walls block enemy movement, adjusting their path, and absorb damage.
public class Wall : Building
{
    [Header("NavMesh Settings")]
    [SerializeField] private bool isNavMeshObstacle = true;
    [SerializeField] private bool carveNavMesh = true;
    
    [Tooltip("Multiplied with collider size to determine NavMesh carve size.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float carveSizeMultiplier = 0.5f;
    
    [Header("References")]
    [SerializeField] private WallData data;
    
    private NavMeshObstacle navMeshObstacle;
    
    public WallData Data => data;
    
    protected override void Awake()
    {
        base.Awake();
        structureType = StructureType.Wall;
        
        // Add NavMeshObstacle component
        navMeshObstacle = gameObject.AddComponent<NavMeshObstacle>();
        navMeshObstacle.enabled = isNavMeshObstacle;
        navMeshObstacle.carving = carveNavMesh;
        navMeshObstacle.shape = NavMeshObstacleShape.Box;
        navMeshObstacle.center = Vector3.zero;
        
        // Set size based on collider with multiplier
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            float width = boxCollider.size.x * carveSizeMultiplier;
            float height = boxCollider.size.y * carveSizeMultiplier;
            navMeshObstacle.size = new Vector3(width, height, 0.1f);
        }
    }
    

    /// Configures the wall with data from a ScriptableObject.
    public void Configure(WallData wallData)
    {
        data = wallData;
        SetHealth(wallData.MaxHP);
    }
    
    public override void TakeDamage(float damage)
    {
        // Apply ranged damage reduction if applicable
        float actualDamage = damage;
        
        if (data.RangedDamageReduction > 0)
        {
            actualDamage = damage * (1f - data.RangedDamageReduction);
        }
        
        base.TakeDamage(actualDamage);
    }
    
    protected override void OnDestroyed()
    {
        Debug.Log("Wall destroyed!");
        Destroy(gameObject);
    }
    
    public override void OnPlaced()
    {
        Debug.Log("Wall placed!");
    }
}