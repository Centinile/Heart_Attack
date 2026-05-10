using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "Main Menu";
    [SerializeField] private float fadeDuration = 0.4f;

    [Header("Fade Overlay")]
    [SerializeField] private Image fadeOverlay;

    private void Start()
    {
        // Fade in when any scene loads
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            StartCoroutine(Fade(1f, 0f, () => fadeOverlay.gameObject.SetActive(false)));
        }
    }

    // --- Public Button Functions ---

    // Reloads the current active scene — use on "Retry" or "Restart" buttons
    public void ResetCurrentLevel()
    {
        Time.timeScale = 1f;
        StartCoroutine(FadeAndLoad(SceneManager.GetActiveScene().name));
    }

    // Loads the main menu scene — use on "Main Menu" buttons
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        StartCoroutine(FadeAndLoad(mainMenuSceneName));
    }

    // Unpauses the game — use on "Resume" buttons
    public void UnpauseGame()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
            GameManager.Instance.ResumeGame();
    }

    // Generic scene change — used by TutorialManager and MainMenuManager
    public void SceneChange(string sceneName)
    {
        Time.timeScale = 1f;
        StartCoroutine(FadeAndLoad(sceneName));
    }

    // --- Fade Helpers ---

    private IEnumerator FadeAndLoad(string sceneName)
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            yield return StartCoroutine(Fade(0f, 1f, null));
        }

        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator Fade(float from, float to, System.Action onComplete)
    {
        if (fadeOverlay == null) yield break;

        float elapsed = 0f;
        Color c = fadeOverlay.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(from, to, elapsed / fadeDuration);
            fadeOverlay.color = c;
            yield return null;
        }

        c.a = to;
        fadeOverlay.color = c;
        onComplete?.Invoke();
    }
}