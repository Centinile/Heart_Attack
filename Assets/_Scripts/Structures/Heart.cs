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

    public override void Sell()
    {
        OnHeartSold?.Invoke(); // Fire achievement event
        base.Sell();           // Handles nutrients + OnDestroyed
    }

    protected override void OnDestroyed()
    {
        if (isGameOverTriggered) return;
        isGameOverTriggered = true;

        Debug.Log("<color=red><b>The Heart has been destroyed!</b></color>");
        GameManager.Instance.GameOver();
        
        base.OnDestroyed();
    }
}