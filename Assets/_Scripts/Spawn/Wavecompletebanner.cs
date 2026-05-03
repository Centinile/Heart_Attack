using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to the "Wave Complete Banner" GameObject.
/// It listens to WaveManager.OnWaveCleared and shows the banner for 3 seconds.
/// </summary>
public class WaveCompleteBanner : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeDuration = 0.3f;

    private CanvasGroup _canvasGroup;
    private Coroutine _hideCoroutine;

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Start hidden
        SetVisible(false);
    }

    void OnEnable()
    {
        WaveManager.OnWaveCleared += Show;
    }

    void OnDisable()
    {
        WaveManager.OnWaveCleared -= Show;
    }

    private void Show()
    {
        if (_hideCoroutine != null)
            StopCoroutine(_hideCoroutine);

        _hideCoroutine = StartCoroutine(ShowThenHide());
    }

    private IEnumerator ShowThenHide()
    {
        // Fade in
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

        // Hold
        yield return new WaitForSecondsRealtime(displayDuration);

        // Fade out
        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        SetVisible(false);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        SetVisible(true);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        _canvasGroup.alpha = to;
    }

    private void SetVisible(bool visible)
    {
        _canvasGroup.alpha          = visible ? 1f : 0f;
        _canvasGroup.interactable   = visible;
        _canvasGroup.blocksRaycasts = visible;
        gameObject.SetActive(visible);
    }
}