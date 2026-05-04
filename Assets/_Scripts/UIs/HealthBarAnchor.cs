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

        if (_bar == null)
        {
            _bar = HealthBarManager.Instance.Rent(transform);
            _bar.Initialize(_maxHP); // only initialize on first rent, not every update
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