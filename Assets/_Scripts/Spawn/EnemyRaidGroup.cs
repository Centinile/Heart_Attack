using UnityEngine;

[CreateAssetMenu(fileName = "Raid Group", menuName = "Waves/Raid Group")]
public class EnemyRaidGroup : SpawnData
{

    [Tooltip("How much this group contributes to wave weight")]
    public int weightCost = 5;
}
