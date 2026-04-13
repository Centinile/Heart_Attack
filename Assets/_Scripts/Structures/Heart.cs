using UnityEngine;

public class Heart : Building
{
    [Header("References")]
    [SerializeField] private HeartData heartData;
    
    private bool isGameOverTriggered = false;

    public void Configure(HeartData data)
    {
        heartData = data;
    }


    protected override void OnDestroyed()
    {
        if (isGameOverTriggered) return;
        isGameOverTriggered = true;

        Debug.Log("<color=red><b>The Heart has been destroyed!</b></color>");
        
        // Trigger game over logic in GameManager
        GameManager.Instance.GameOver();
        
        base.OnDestroyed();
    }
}