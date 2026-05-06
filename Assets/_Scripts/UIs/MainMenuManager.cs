using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("Level Select Panel")]
    [SerializeField] private GameObject levelSelectPanel;
    [SerializeField] private float panelFadeDuration = 0.25f;
    private CanvasGroup _levelSelectGroup;
    private Coroutine _panelCoroutine;

    [Header("Achievements Panel")]
    [SerializeField] private GameObject achievementsPanel;
    [SerializeField] private float achievementsFadeDuration = 0.25f;
    [SerializeField] private Transform achievementsContent; // parent to spawn achievement rows into
    [SerializeField] private GameObject achievementRowPrefab; // prefab with two TMP_Text children: Name + Description
    private CanvasGroup _achievementsGroup;
    private Coroutine _achievementsPanelCoroutine;
    private bool _achievementsPopulated = false;

    [Header("Scene Transition")]
    [SerializeField] private Image fadeOverlay;         // Full-screen black Image on a top-level Canvas
    [SerializeField] private float sceneFadeDuration = 0.4f;

    private const string KEY_ACID_RAIN   = "EnableAcidRain";
    private const string KEY_FOG_OF_WAR  = "EnableFogOfWar";
    private const string KEY_RANDOM_WAVES = "EnableRandomWaves";
    private const string KEY_STAT_RAMPING = "EnableStatRamping";
    private const string KEY_NO_BREAKS   = "EnableNoBreaks";

    private void Awake()
    {
        // ── Achievements Panel setup ───────────────────────────────────
        if (achievementsPanel != null)
        {
            _achievementsGroup = achievementsPanel.GetComponent<CanvasGroup>();
            if (_achievementsGroup == null)
                _achievementsGroup = achievementsPanel.AddComponent<CanvasGroup>();

            _achievementsGroup.alpha          = 0f;
            _achievementsGroup.interactable   = false;
            _achievementsGroup.blocksRaycasts = false;
            achievementsPanel.SetActive(false);
        }

        // ── Level Select Panel setup ───────────────────────────────────
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
        // ── Toggles ────────────────────────────────────────────────────
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

        // ── Fade overlay setup ─────────────────────────────────────────
        if (fadeOverlay != null)
        {
            // Fade in from black when the scene loads
            fadeOverlay.gameObject.SetActive(true);
            StartCoroutine(FadeOverlay(1f, 0f, sceneFadeDuration));
        }
    }

    // ── Public button callbacks ────────────────────────────────────────

    /// <summary>Called by the "Select Level" button.</summary>
    public void OpenLevelSelectPanel()
    {
        if (levelSelectPanel == null) return;
        levelSelectPanel.SetActive(true);
        SetPanelVisible(true);
    }

    /// <summary>Called by the X / close button inside the Level Select Panel.</summary>
    public void CloseLevelSelectPanel()
    {
        SetPanelVisible(false, () => levelSelectPanel.SetActive(false));
    }

    /// <summary>Called by the Play / Start button to load the game scene.</summary>
    public void StartGame()
    {
        PlayerPrefs.Save();
        StartCoroutine(FadeAndLoad(gameSceneName));
    }

    /// <summary>Called by the Achievements button.</summary>
    public void OpenAchievementsPanel()
    {
        if (achievementsPanel == null) return;

        // Populate once
        if (!_achievementsPopulated)
        {
            PopulateAchievements();
            _achievementsPopulated = true;
        }

        achievementsPanel.SetActive(true);
        SetAchievementsPanelVisible(true);
    }

    /// <summary>Called by the X / close button inside the Achievements Panel.</summary>
    public void CloseAchievementsPanel()
    {
        SetAchievementsPanelVisible(false, () => achievementsPanel.SetActive(false));
    }

    private void SetAchievementsPanelVisible(bool visible, System.Action onComplete = null)
    {
        if (_achievementsPanelCoroutine != null) StopCoroutine(_achievementsPanelCoroutine);
        _achievementsPanelCoroutine = StartCoroutine(FadeCanvasGroup(_achievementsGroup, visible ? 1f : 0f, achievementsFadeDuration, onComplete));
        _achievementsGroup.interactable   = visible;
        _achievementsGroup.blocksRaycasts = visible;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float target, float duration, System.Action onComplete = null)
    {
        float start   = group.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed     += Time.unscaledDeltaTime;
            group.alpha  = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        group.alpha = target;
        onComplete?.Invoke();
    }

    private void PopulateAchievements()
    {
        if (achievementsContent == null || achievementRowPrefab == null) return;

        // Clear old rows
        foreach (Transform child in achievementsContent)
            Destroy(child.gameObject);

        // Get all achievement IDs
        System.Array ids = System.Enum.GetValues(typeof(AchievementID));
        foreach (AchievementID id in ids)
        {
            bool unlocked = AchievementManager.Instance != null && AchievementManager.Instance.IsUnlocked(id);

            GameObject row = Instantiate(achievementRowPrefab, achievementsContent);
            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>();

            // texts[0] = Name, texts[1] = Description (order matches hierarchy)
            string displayName = id.ToString();
            string description = "";

            // Try to get friendly name/description from AchievementManager if available
            if (texts.Length >= 1)
                texts[0].text = (unlocked ? "✓ " : "✗ ") + displayName;
            if (texts.Length >= 2)
                texts[1].text = unlocked ? description : "???";

            // Grey out locked achievements
            CanvasGroup rowGroup = row.GetComponent<CanvasGroup>();
            if (rowGroup == null) rowGroup = row.AddComponent<CanvasGroup>();
            rowGroup.alpha = unlocked ? 1f : 0.45f;
        }
    }

    /// <summary>Called by the Quit button.</summary>
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
        float target = visible ? 1f : 0f;
        _panelCoroutine = StartCoroutine(FadePanel(target, onComplete));

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
            _levelSelectGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha,
                                                  elapsed / panelFadeDuration);
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

        if (to == 0f)
            fadeOverlay.gameObject.SetActive(false);
    }
}