using UnityEngine;

public class HealthBarAnchor : MonoBehaviour
{
    private HealthBarUI _bar;
    private float _maxHP;

    public void Initialize(float maxHP)
    {
        _maxHP = maxHP;
    }

    public void UpdateBar(float current, float max)
    {
        _maxHP = max;

        bool isFull = current >= max;

        if (isFull)
        {
            // Return bar to pool when at full HP — no need to show it
            ReturnBar();
            return;
        }

        // Rent a bar if we don't have one yet (first damage)
        if (_bar == null)
        {
            _bar = HealthBarManager.Instance.Rent(transform);
            _bar.Initialize(_maxHP);
        }

        _bar.UpdateBar(current, max);
    }
    
    public void ReturnBar()
    {
        if (_bar == null) return;
        HealthBarManager.Instance.Return(_bar);
        _bar = null;
    }

    private void OnDestroy() => ReturnBar();
}