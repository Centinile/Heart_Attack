using UnityEngine;

public interface IEnemy
{
    EnemyData Data { get; }
    Transform transform { get; }
    float CurrentHP { get; }
    void TakeDamage(float damage);
    void ModifySpeed(float mult, float dur);
}