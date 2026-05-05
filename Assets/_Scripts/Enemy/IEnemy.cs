using UnityEngine;

public interface IEnemy
{
    EnemyData Data { get; }
    Transform transform { get; }
    float CurrentHP { get; }
    void SetHP(float amount);
    void TakeDamage(float damage);
    void ModifySpeed(float mult, float dur);
}