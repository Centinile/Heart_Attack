using UnityEngine;

/// <summary>
/// Attach to the "Results" GameObject.
/// Keeps Results always active (so children can show/hide independently)
/// but disables raycasts when nothing is visible, so towers can be placed.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ResultsPanel : MonoBehaviour
{
    [SerializeField] private GameObject waveCompleteBanner;
    [SerializeField] private GameObject gameOverText;
    [SerializeField] private GameObject congratulations;

    private CanvasGroup _canvasGroup;

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();

        // Remove the Image background so it doesn't block clicks
        // (the children have their own backgrounds)
        var img = GetComponent<UnityEngine.UI.Image>();
        if (img != null) img.enabled = false;

        // Disable raycasts by default — children re-enable when they show
        SetBlocking(false);
    }

    void LateUpdate()
    {
        // Each frame: block raycasts only if at least one child is visible
        bool anyVisible =
            (waveCompleteBanner != null && waveCompleteBanner.activeSelf) ||
            (gameOverText       != null && gameOverText.activeSelf)       ||
            (congratulations    != null && congratulations.activeSelf);

        SetBlocking(anyVisible);
    }

    private void SetBlocking(bool block)
    {
        _canvasGroup.blocksRaycasts = block;
        _canvasGroup.interactable   = block;
    }
}
