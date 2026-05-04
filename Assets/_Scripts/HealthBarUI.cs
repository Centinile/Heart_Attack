using System.Collections;
using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private RectTransform topBar;
    [SerializeField] private RectTransform bottomBar;
    [SerializeField] private Vector2 worldOffset = new Vector2(0f, 0.6f);
    [SerializeField] private float animationSpeed = 10f;

    private float _fullWidth;
    private float _currentValue;
    private float _maxValue;
    private float TargetWidth => _maxValue > 0 ? (_currentValue / _maxValue) * _fullWidth : 0f;

    private Transform _anchor;
    private Camera _cam;
    private RectTransform _canvasRect;
    private Coroutine _animCoroutine;

    private void Awake()
    {
        _fullWidth = topBar.rect.width;
    }

    private void LateUpdate()
    {
        if (_anchor == null) return;

        Vector2 screenPoint = _cam.WorldToScreenPoint(_anchor.position + (Vector3)worldOffset);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPoint, null, out Vector2 localPoint);

        ((RectTransform)transform).anchoredPosition = localPoint;
    }

    public void Attach(Transform anchor, Camera cam, RectTransform canvasRect)
    {
        _anchor = anchor;
        _cam = cam;
        _canvasRect = canvasRect;
    }

    public void Detach()
    {
        _anchor = null;
        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
    }

    public void Initialize(float maxValue)
    {
        _maxValue = maxValue;
        _currentValue = maxValue;
        // Only snap to full here, on genuine first-time setup
        topBar.SetWidth(_fullWidth);
        bottomBar.SetWidth(_fullWidth);
    }

    public void UpdateBar(float current, float max)
    {
        bool isDamage = current < _currentValue;
        _maxValue = max;
        _currentValue = Mathf.Clamp(current, 0f, max);

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateBar(isDamage));
    }

    private IEnumerator AnimateBar(bool isDamage)
    {
        var snapBar  = isDamage ? topBar    : bottomBar;
        var trailBar = isDamage ? bottomBar : topBar;

        snapBar.SetWidth(TargetWidth);

        while (Mathf.Abs(trailBar.rect.width - TargetWidth) > 0.5f)
        {
            trailBar.SetWidth(Mathf.Lerp(trailBar.rect.width, TargetWidth, Time.deltaTime * animationSpeed));
            yield return null;
        }
        trailBar.SetWidth(TargetWidth);
    }
}