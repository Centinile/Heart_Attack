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

    [Header("Difficulty Setting")]
    [SerializeField] private TMP_Dropdown difficultyDropdown;

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

    private const string KEY_DIFFICULTY   = "Difficulty";
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
        if (difficultyDropdown != null)
            {
                // Load the saved index (default to 0 / Easy)
                difficultyDropdown.value = PlayerPrefs.GetInt(KEY_DIFFICULTY, 0);

                // Add listener to save whenever the user changes the option
                difficultyDropdown.onValueChanged.AddListener(index => {
                    PlayerPrefs.SetInt(KEY_DIFFICULTY, index);
                    PlayerPrefs.Save(); // Force save to be safe
                });
            }

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

        AchievementManager achievementManager = AchievementManager.Instance;
        if (achievementManager == null) return;

        // Clear old rows
        foreach (Transform child in achievementsContent)
            Destroy(child.gameObject);

        IReadOnlyList<AchievementDefinition> definitions = achievementManager.GetDefinitions();
        foreach (AchievementDefinition definition in definitions)
        {
            bool unlocked = achievementManager.IsUnlocked(definition.ID);

            GameObject row = Instantiate(achievementRowPrefab, achievementsContent);

            // Row background image (BG) - used to enforce full opacity when unlocked.
            Image backgroundImage = FindChildComponentByNames<Image>(row.transform,
                "BG", "Background", "BACKGROUND");
            if (backgroundImage == null && row.transform.childCount >= 1)
            {
                Transform bgChild = row.transform.GetChild(0);
                backgroundImage = bgChild.GetComponent<Image>() ?? bgChild.GetComponentInChildren<Image>(true);
            }

            // Prefer named children so prefab child ordering doesn't matter
            Image trophyImage = FindChildComponentByNames<Image>(row.transform,
                "TROPHY IMAGE", "Trophy Image", "Trophy", "TrophyIcon", "Trophy Icon");

            TMP_Text nameText = FindChildComponentByNames<TMP_Text>(row.transform,
                "NAME TEXT", "Name Text", "NameText", "Name");
            TMP_Text descriptionText = FindChildComponentByNames<TMP_Text>(row.transform,
                "DESCRIPTION TEXT", "Description Text", "DescriptionText", "Description");
            TMP_Text statusText = FindChildComponentByNames<TMP_Text>(row.transform,
                "STATUS", "Status", "Status Text", "StatusText");

            // Fallback: prefer the exact child-order you described (BG, TROPHY IMAGE, NAME TEXT, DESCRIPTION TEXT, STATUS)
            TMP_Text[] allTexts = row.GetComponentsInChildren<TMP_Text>(true);
            if ((nameText == null || descriptionText == null || statusText == null || trophyImage == null) && row.transform.childCount >= 5)
            {
                Transform trophyChild = row.transform.GetChild(1);
                Transform nameChild   = row.transform.GetChild(2);
                Transform descChild   = row.transform.GetChild(3);
                Transform statusChild = row.transform.GetChild(4);

                if (trophyImage == null)
                    trophyImage = trophyChild.GetComponent<Image>() ?? trophyChild.GetComponentInChildren<Image>(true);

                if (nameText == null)
                    nameText = nameChild.GetComponent<TMP_Text>() ?? nameChild.GetComponentInChildren<TMP_Text>(true);

                if (descriptionText == null)
                    descriptionText = descChild.GetComponent<TMP_Text>() ?? descChild.GetComponentInChildren<TMP_Text>(true);

                if (statusText == null)
                    statusText = statusChild.GetComponent<TMP_Text>() ?? statusChild.GetComponentInChildren<TMP_Text>(true);
            }

            // Final fallback: take the first available TMP_Texts in the prefab
            if (nameText == null && allTexts.Length >= 1) nameText = allTexts[0];
            if (descriptionText == null && allTexts.Length >= 2) descriptionText = allTexts.Length >= 2 ? allTexts[1] : allTexts[0];
            if (statusText == null && allTexts.Length >= 3) statusText = allTexts.Length >= 3 ? allTexts[2] : (allTexts.Length >= 2 ? allTexts[1] : allTexts[0]);

            if (nameText != null)
                nameText.text = definition.Name;
            if (descriptionText != null)
                descriptionText.text = definition.Description;
            if (statusText != null)
                statusText.text = unlocked ? "Unlocked" : "Locked";

            if (backgroundImage != null && unlocked)
            {
                // Unity inspector's 255 alpha equivalent in code.
                Color32 c = backgroundImage.color;
                c.a = 255;
                backgroundImage.color = c;
            }

            if (trophyImage != null)
            {
                if (definition.TrophySprite != null)
                {
                    trophyImage.enabled = true;
                    trophyImage.sprite = definition.TrophySprite;
                    trophyImage.color = unlocked ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                }
                else
                {
                    trophyImage.enabled = false;
                }
            }

            // Grey out locked achievements
            CanvasGroup rowGroup = row.GetComponent<CanvasGroup>();
            if (rowGroup == null) rowGroup = row.AddComponent<CanvasGroup>();
            rowGroup.alpha = unlocked ? 1f : 0.55f;
        }
    }

    private T FindChildComponentByNames<T>(Transform parent, params string[] names) where T : Component
    {
        if (parent == null) return null;

        // direct child lookup first
        foreach (string n in names)
        {
            Transform t = parent.Find(n);
            if (t != null)
            {
                T comp = t.GetComponent<T>();
                if (comp != null) return comp;
            }
        }

        // fallback: scan all children and compare normalized names
        T[] comps = parent.GetComponentsInChildren<T>(true);
        foreach (T c in comps)
        {
            string childName = c.gameObject.name.Replace(" ", "").ToLower();
            foreach (string n in names)
            {
                if (childName == n.Replace(" ", "").ToLower())
                    return c;
            }
        }

        // final fallback: return first match
        return comps.Length > 0 ? comps[0] : null;
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