using UnityEngine;

public class FogRevealer : MonoBehaviour
{
    private bool _registered = false;

    private void Start()
    {
        if (FogRevealManager.Instance != null && !_registered)
        {
            FogRevealManager.Instance.Register(this);
            _registered = true;
        }
    }

    private void OnDestroy()
    {
        if (FogRevealManager.Instance != null && _registered)
        {
            FogRevealManager.Instance.Unregister(this);
            _registered = false;
        }
    }
}