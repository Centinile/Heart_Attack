using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private SceneController sceneController;
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Level Setting Toggles")]
    [SerializeField] private Toggle acidRainToggle;
    [SerializeField] private Toggle fogOfWarToggle;
    [SerializeField] private Toggle randomWavesToggle;
    [SerializeField] private Toggle statRampingToggle;
    [SerializeField] private Toggle noBreaksToggle;

    [Header("Difficulty")]
    [SerializeField] private TMP_Dropdown difficultyDropdown;

    [Header("Level Select Panel")]
    [SerializeField] private GameObject levelSelectPanel;
    [SerializeField] private float panelFadeDuration = 0.25f;
    private CanvasGroup _levelSelectGroup;
    private Coroutine _panelCoroutine;

    [Header("Scene Transition")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float sceneFadeDuration = 0.4f;

    private const string KEY_ACID_RAIN    = "EnableAcidRain";
    private const string KEY_FOG_OF_WAR   = "EnableFogOfWar";
    private const string KEY_RANDOM_WAVES = "EnableRandomWaves";
    private const string KEY_STAT_RAMPING = "EnableStatRamping";
    private const string KEY_NO_BREAKS    = "EnableNoBreaks";
    private const string KEY_DIFFICULTY   = "Difficulty";

    private void Awake()
    {
        if (levelSelectPanel != null)
        {
            _levelSelectGroup = levelSelectPanel.GetComponent<CanvasGroup>();
            if (_levelSelectGroup == null)
                _levelSelectGroup = levelSelectPanel.AddComponent<CanvasGroup>();

            _levelSelectGroup.alpha          = 0f;
            _levelSelectGroup.interactable   = false;
            _levelSelectGroup.blocksRaycasts = false;
            levelSelectPanel.SetActive(false);
        }
    }

    private void Start()
    {
        // Toggles
        acidRainToggle.isOn    = PlayerPrefs.GetInt(KEY_ACID_RAIN,    0) == 1;
        fogOfWarToggle.isOn    = PlayerPrefs.GetInt(KEY_FOG_OF_WAR,   0) == 1;
        randomWavesToggle.isOn = PlayerPrefs.GetInt(KEY_RANDOM_WAVES, 0) == 1;
        statRampingToggle.isOn = PlayerPrefs.GetInt(KEY_STAT_RAMPING, 0) == 1;
        noBreaksToggle.isOn    = PlayerPrefs.GetInt(KEY_NO_BREAKS,    0) == 1;

        acidRainToggle.onValueChanged.AddListener(v    => PlayerPrefs.SetInt(KEY_ACID_RAIN,    v ? 1 : 0));
        fogOfWarToggle.onValueChanged.AddListener(v    => PlayerPrefs.SetInt(KEY_FOG_OF_WAR,   v ? 1 : 0));
        randomWavesToggle.onValueChanged.AddListener(v => PlayerPrefs.SetInt(KEY_RANDOM_WAVES, v ? 1 : 0));
        statRampingToggle.onValueChanged.AddListener(v => PlayerPrefs.SetInt(KEY_STAT_RAMPING, v ? 1 : 0));
        noBreaksToggle.onValueChanged.AddListener(v    => PlayerPrefs.SetInt(KEY_NO_BREAKS,    v ? 1 : 0));

        // Difficulty dropdown — default 0 (Easy) if no pref saved
        if (difficultyDropdown != null)
        {
            difficultyDropdown.value = PlayerPrefs.GetInt(KEY_DIFFICULTY, 0);
            difficultyDropdown.onValueChanged.AddListener(v => PlayerPrefs.SetInt(KEY_DIFFICULTY, v));
        }

        // Fade in from black
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            StartCoroutine(FadeOverlay(1f, 0f, sceneFadeDuration));
        }
    }

    // ── Public button callbacks ────────────────────────────────────────

    public void OpenLevelSelectPanel()
    {
        if (levelSelectPanel == null) return;
        levelSelectPanel.SetActive(true);
        SetPanelVisible(true);
    }

    public void CloseLevelSelectPanel()
    {
        SetPanelVisible(false, () => levelSelectPanel.SetActive(false));
    }

    public void StartGame()
    {
        PlayerPrefs.Save();
        StartCoroutine(FadeAndLoad(gameSceneName));
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Panel fade helpers ─────────────────────────────────────────────

    private void SetPanelVisible(bool visible, System.Action onComplete = null)
    {
        if (_panelCoroutine != null) StopCoroutine(_panelCoroutine);
        _panelCoroutine = StartCoroutine(FadePanel(visible ? 1f : 0f, onComplete));
        _levelSelectGroup.interactable   = visible;
        _levelSelectGroup.blocksRaycasts = visible;
    }

    private IEnumerator FadePanel(float targetAlpha, System.Action onComplete = null)
    {
        float startAlpha = _levelSelectGroup.alpha;
        float elapsed    = 0f;

        while (elapsed < panelFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _levelSelectGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / panelFadeDuration);
            yield return null;
        }

        _levelSelectGroup.alpha = targetAlpha;
        onComplete?.Invoke();
    }

    // ── Scene fade helpers ─────────────────────────────────────────────

    private IEnumerator FadeAndLoad(string sceneName)
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            yield return StartCoroutine(FadeOverlay(0f, 1f, sceneFadeDuration));
        }

        PlayerPrefs.Save();
        sceneController.SceneChange(sceneName);
    }

    private IEnumerator FadeOverlay(float from, float to, float duration)
    {
        float elapsed = 0f;
        Color c = fadeOverlay.color;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(from, to, elapsed / duration);
            fadeOverlay.color = c;
            yield return null;
        }

        c.a = to;
        fadeOverlay.color = c;

        if (to == 0f) fadeOverlay.gameObject.SetActive(false);
    }
}