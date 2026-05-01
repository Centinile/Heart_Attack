using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BeamRenderer : MonoBehaviour
{
    [SerializeField] private GameObject impactEffectPrefab;

    private LineRenderer _lineRenderer;
    private GameObject _impactEffectInstance;
    private Vector3 _lastImpactPoint;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
        _lineRenderer.enabled = false;
    }

    public void ShowBeam(Vector3 from, Vector3 to)
    {
        _lineRenderer.enabled = true;
        _lineRenderer.SetPosition(0, from);
        _lineRenderer.SetPosition(1, to);

        // Only spawn a new impact effect if the position has changed meaningfully
        if (_impactEffectInstance == null || Vector3.Distance(to, _lastImpactPoint) > 0.1f)
        {
            if (_impactEffectInstance != null)
                Destroy(_impactEffectInstance);

            if (impactEffectPrefab != null)
            {
                _impactEffectInstance = Instantiate(impactEffectPrefab, to, Quaternion.identity);
                _lastImpactPoint = to;
            }
        }
    }

    public void HideBeam()
    {
        _lineRenderer.enabled = false;

        if (_impactEffectInstance != null)
        {
            Destroy(_impactEffectInstance);
            _impactEffectInstance = null;
        }
    }

    private void OnDestroy() => HideBeam();
}