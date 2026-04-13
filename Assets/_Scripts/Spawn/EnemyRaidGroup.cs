using UnityEngine;

[CreateAssetMenu(fileName = "Raid Group", menuName = "Waves/Raid Group")]
public class EnemyRaidGroup : SpawnData
{
    [Tooltip("How much this group costs from the wave's total budget")]
    public int weightCost = 5;
    public float NutrientReward = 0;

    [Tooltip("The likelihood of this group being picked (1 = Rare, 10 = Common)")]
    public int selectionWeight = 10; 

    [Tooltip("Nickname of the raid")]
    public string raidName;
}
