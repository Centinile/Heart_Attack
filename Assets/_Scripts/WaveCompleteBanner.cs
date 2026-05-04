using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to the "Wave Clear" GameObject.
/// Shows the banner for a few seconds after each wave is cleared.
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

        SetVisible(false);

        // Subscribe in Awake so it works even when this object starts disabled
        WaveManager.OnWaveCleared += Show;
    }

    void OnDestroy()
    {
        WaveManager.OnWaveCleared -= Show;
    }

    private void Show()
    {
        if (_hideCoroutine != null)
            StopCoroutine(_hideCoroutine);

        // Make sure Results parent and this object are active
        gameObject.SetActive(true);
        if (transform.parent != null)
            transform.parent.gameObject.SetActive(true);

        _hideCoroutine = StartCoroutine(ShowThenHide());
    }

    private IEnumerator ShowThenHide()
    {
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));
        yield return new WaitForSecondsRealtime(displayDuration);
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
    }
}
