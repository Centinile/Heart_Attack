using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BeamRenderer : MonoBehaviour
{
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private bool persistentBeam = true; // false = flash only on hit tick
    public bool IsPersistent => persistentBeam;

    private LineRenderer _lineRenderer;
    private GameObject _impactEffectInstance;
    private Vector3 _lastImpactPoint;

    // Flash duration when persistentBeam is false
    private const float FLASH_DURATION = 0.08f;
    private float _flashTimer = 0f;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
        _lineRenderer.enabled = false;
    }

    private void Update()
    {
        // Non-persistent beam auto-hides after flash duration
        if (!persistentBeam && _flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f)
                HideBeam();
        }
    }

    public void ShowBeam(Vector3 from, Vector3 to)
    {
        _lineRenderer.enabled = true;
        _lineRenderer.SetPosition(0, from);
        _lineRenderer.SetPosition(1, to);

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

        // For non-persistent beams, start the flash timer each time ShowBeam is called
        if (!persistentBeam)
            _flashTimer = FLASH_DURATION;
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