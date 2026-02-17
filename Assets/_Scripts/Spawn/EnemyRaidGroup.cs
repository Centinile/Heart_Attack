using UnityEngine;

[CreateAssetMenu(fileName = "Raid Group", menuName = "Scriptable Objects/Raid Group")]
public class EnemyRaidGroup : SpawnData
{

    [Tooltip("How much this group contributes to wave weight")]
    public int weightCost = 5;
}
