using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class FlickerBeam : MonoBehaviour
{
    [SerializeField] private float flickerOnDuration  = 0.08f;
    [SerializeField] private float flickerOffDuration = 0.05f;
    [SerializeField] private int   flickerCount       = 1;
    [SerializeField] private GameObject impactEffectPrefab;

    private LineRenderer _lineRenderer;
    private GameObject  _impactInstance;
    private Coroutine   _flickerCoroutine;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
        _lineRenderer.enabled = false;
    }

    // Called by DefenseTower each time it fires
    public void Fire(Vector3 from, Vector3 to)
    {
        if (_flickerCoroutine != null) StopCoroutine(_flickerCoroutine);
        _flickerCoroutine = StartCoroutine(FlickerRoutine(from, to));
    }

    private IEnumerator FlickerRoutine(Vector3 from, Vector3 to)
    {
        // Spawn impact effect once at start
        if (impactEffectPrefab != null && _impactInstance == null)
            _impactInstance = Instantiate(impactEffectPrefab, to, Quaternion.identity);

        for (int i = 0; i < flickerCount; i++)
        {
            _lineRenderer.enabled = true;
            _lineRenderer.SetPosition(0, from);
            _lineRenderer.SetPosition(1, to);
            yield return new WaitForSeconds(flickerOnDuration);

            _lineRenderer.enabled = false;
            yield return new WaitForSeconds(flickerOffDuration);
        }

        // Clean up impact effect after flicker finishes
        if (_impactInstance != null)
        {
            Destroy(_impactInstance);
            _impactInstance = null;
        }

        // Signal pool that we're done
        gameObject.SetActive(false);
    }

    public void CopySettingsFrom(FlickerBeam source)
    {
        flickerOnDuration  = source.flickerOnDuration;
        flickerOffDuration = source.flickerOffDuration;
        flickerCount       = source.flickerCount;
        impactEffectPrefab = source.impactEffectPrefab;

        LineRenderer sourceLR = source.GetComponent<LineRenderer>();
        LineRenderer myLR     = GetComponent<LineRenderer>();
        if (sourceLR != null && myLR != null)
        {
            myLR.startWidth  = sourceLR.startWidth;
            myLR.endWidth    = sourceLR.endWidth;
            myLR.sharedMaterial = sourceLR.sharedMaterial;
            myLR.startColor  = sourceLR.startColor;
            myLR.endColor    = sourceLR.endColor;
        }
    }

    private void OnDisable()
    {
        _lineRenderer.enabled = false;
        if (_impactInstance != null)
        {
            Destroy(_impactInstance);
            _impactInstance = null;
        }
    }
}