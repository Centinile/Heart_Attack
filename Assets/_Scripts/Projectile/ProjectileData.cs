using UnityEngine;

[CreateAssetMenu(menuName = "Projectile", fileName = "ProjectileData_", order = 50)]
public class ProjectileData : ScriptableObject
{
    [Header("Projectile Settings")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private Sprite sprite;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private bool homing = true;

    [Header("Visual Effects")]
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private GameObject trailEffect;

    [Header("DOT Settings")]
    [SerializeField] private bool applyDOT = false;
    [SerializeField] private float dotDamagePerTick = 5f;
    [SerializeField] private float dotTickInterval = 0.5f;
    [SerializeField] private float dotDuration = 3f;
    [SerializeField] private float dotRadius = 1.5f;
    [SerializeField] private GameObject dotZonePrefab;

    public GameObject Prefab => prefab;
    public Sprite Sprite => sprite;
    public float Speed => speed;
    public float Lifetime => lifetime;
    public bool Homing => homing;
    public GameObject ImpactEffect => impactEffect;
    public GameObject TrailEffect => trailEffect;
    public bool ApplyDOT => applyDOT;
    public float DOTDamagePerTick => dotDamagePerTick;
    public float DOTTickInterval => dotTickInterval;
    public float DOTDuration => dotDuration;
    public float DOTRadius => dotRadius;
    public GameObject DOTZonePrefab => dotZonePrefab;
}