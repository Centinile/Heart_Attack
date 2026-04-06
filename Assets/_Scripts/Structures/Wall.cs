using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Represents a defensive wall structure that blocks paths and absorbs damage.
/// </summary>
public class Wall : Building
{
    [Header("NavMesh Settings")]
    //[SerializeField] private bool isNavMeshObstacle = true;
    //[SerializeField] private bool carveNavMesh = true;
    //[Range(0.1f, 1f)] [SerializeField] private float carveSizeMultiplier = 0.5f;

    [Header("References")]
    [SerializeField] private WallData data;
    
    private NavMeshObstacle navMeshObstacle;

    protected override void Awake()
    {
        base.Awake();
        structureType = StructureType.Wall;
        //SetupNavMesh();
    }

    // private void SetupNavMesh()
    // {
    //     navMeshObstacle = gameObject.AddComponent<NavMeshObstacle>();
    //     navMeshObstacle.enabled = isNavMeshObstacle;
    //     navMeshObstacle.carving = carveNavMesh;
    //     navMeshObstacle.shape = NavMeshObstacleShape.Box;

    //     if (TryGetComponent(out BoxCollider2D box))
    //     {
    //         navMeshObstacle.size = new Vector3(box.size.x * carveSizeMultiplier, box.size.y * carveSizeMultiplier, 0.1f);
    //     }
    // }

    public void Configure(WallData wallData)
    {
        data = wallData;
        SetHealth(data.MaxHP);
    }

    public override void TakeDamage(float damage)
    {
        // Reduce damage if a reduction value exists, otherwise use base damage
        float reduction = data != null ? data.RangedDamageReduction : 0;
        base.TakeDamage(damage * (1f - reduction));
    }

    public override void OnPlaced() => Debug.Log($"{gameObject.name} placed!");

    protected override void OnDestroyed()
    {
        Debug.Log("Wall destroyed!");
        Destroy(gameObject);
    }
}