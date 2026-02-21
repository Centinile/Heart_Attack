using UnityEngine;


/// Data class for projectile properties.
[CreateAssetMenu(
    menuName = "Projectile",
    fileName = "ProjectileData_",
    order = 50)]
public class ProjectileData : ScriptableObject
{
    [Header("Projectile Settings")]
    [Tooltip("Prefab to spawn when the projectile is fired.")]
    [SerializeField] private GameObject prefab;
    
    [Tooltip("Sprite shown for the projectile.")]
    [SerializeField] private Sprite sprite;
    
    [Tooltip("Speed of the projectile.")]
    [SerializeField] private float speed = 10f;
    
    [Tooltip("Lifetime of the projectile in seconds.")]
    [SerializeField] private float lifetime = 5f;
    
    [Tooltip("Should the projectile home in on the target?")]
    [SerializeField] private bool homing = true;
    
    [Header("Visual Effects")]
    [Tooltip("Particle effect on impact.")]
    [SerializeField] private GameObject impactEffect;
    
    [Tooltip("Trail effect behind the projectile.")]
    [SerializeField] private GameObject trailEffect;
    
    // Public accessors
    public GameObject Prefab => prefab;
    public Sprite Sprite => sprite;
    public float Speed => speed;
    public float Lifetime => lifetime;
    public bool Homing => homing;
    public GameObject ImpactEffect => impactEffect;
    public GameObject TrailEffect => trailEffect;
}