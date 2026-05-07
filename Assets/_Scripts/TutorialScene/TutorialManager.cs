using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Tutorial Bubble")]
    public GameObject tutorialBubble;

    [Tooltip("Parts 0-12 (part 1 through part 13). Drag in order.")]
    public TMP_Text[] parts;

    [Header("Buttons")]
    public Button step1NextButton;
    public Button step2NextButton;
    public Button doneButton;

    [Header("References")]
    public SceneController sceneController;
    public WaveManager waveManager;

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuName = "Main Menu";

    [Header("Typewriter Settings")]
    [SerializeField] private float charDelay = 0.03f;

    // Parts that require a specific action before the player can advance
    // Index 2  = part 3  → place a defense tower
    // Index 4  = part 5  → switch to resource tab
    // Index 5  = part 6  → place a nutrient mine
    // Index 7  = part 8  → place a water pump
    // Index 10 = part 11 → press GO button
    private int _currentPart = 0;
    private bool _isTyping = false;
    private bool _skipRequested = false;
    private bool _tutorialDone = false;
    private bool _isFinishing = false;
    private bool _actionCompleted = false;
    private Coroutine _typeCoroutine;

    // ── Tracks whether AdvanceToNextPart has already been called for the
    //    current part, so duplicate calls (click + auto-advance coroutine)
    //    don't skip an extra step.
    private bool _hasAdvancedFromCurrentPart = false;

    // ── Public accessors for TutorialTowerPlacer ──────────────────
    public int CurrentPart => _currentPart;
    public bool IsTyping   => _isTyping;

    // ── Lifecycle ──────────────────────────────────────────────────────

    void Awake() => Instance = this;

    void Start()
    {
        if (step1NextButton != null) step1NextButton.gameObject.SetActive(false);
        if (step2NextButton != null) step2NextButton.gameObject.SetActive(false);
        if (doneButton      != null) doneButton.gameObject.SetActive(false);

        foreach (var p in parts) if (p != null) p.gameObject.SetActive(false);

        if (tutorialBubble != null) tutorialBubble.SetActive(true);

        ShowPart(0);
    }

    void Update()
    {
        if (_tutorialDone) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (_isTyping)
            {
                // Skip the typewriter animation; do NOT advance the part
                _skipRequested = true;
            }
            else if (!IsActionGatedPart(_currentPart))
            {
                // Free part — click advances normally
                AdvanceToNextPart();
            }
            // Action-gated parts: clicks are intentionally ignored here.
            // Only CompleteAction() → AutoAdvanceAfterAction() will advance them.
        }
    }

    // ── Action gates ───────────────────────────────────────────────────

    private bool IsActionGatedPart(int index)
    {
        return index == 2   // part 3:  place defense tower
            || index == 4   // part 5:  switch to resource tab
            || index == 5   // part 6:  place nutrient mine
            || index == 7   // part 8:  place water pump
            || index == 10; // part 11: press GO
    }

    /// <summary>Called by TutorialTowerPlacer when a defense tower is placed.</summary>
    public void OnDefenseTowerPlaced()
    {
        if (_currentPart == 2) CompleteAction();
    }

    /// <summary>Called by KeybindManager or tab toggle when resource tab is opened.</summary>
    public void OnResourceTabOpened()
    {
        if (_currentPart == 4) CompleteAction();
    }

    /// <summary>Called by TutorialTowerPlacer when a mine is placed.</summary>
    public void OnMinePlaced()
    {
        if (_currentPart == 5) CompleteAction();
    }

    /// <summary>Called by TutorialTowerPlacer when a water pump is placed.</summary>
    public void OnWaterPumpPlaced()
    {
        if (_currentPart == 7) CompleteAction();
    }

    /// <summary>Called by GO button onClick.</summary>
    public void OnGoButtonPressed()
    {
        if (_currentPart == 10)
        {
            CompleteAction();
            waveManager?.StartWave();
        }
    }

    private void CompleteAction()
    {
        if (_actionCompleted) return;
        _actionCompleted = true;

        // We stop the typewriter immediately so the auto-advance 
        // doesn't have to wait for the animation to finish.
        _skipRequested = true; 
        
        StartCoroutine(AutoAdvanceAfterAction());
    }

    private IEnumerator AutoAdvanceAfterAction()
    {
        yield return new WaitForSecondsRealtime(0.5f);

        // If the typewriter is still running, wait for it to finish first
        while (_isTyping) yield return null;

        AdvanceToNextPart();
    }

    // ── Core logic ─────────────────────────────────────────────────────

    private void ShowPart(int index)
    {
        if (index >= parts.Length) { OnAllPartsShown(); return; }

        // Reset per-part state
        _actionCompleted          = false;
        _hasAdvancedFromCurrentPart = false;

        foreach (var p in parts) if (p != null) p.gameObject.SetActive(false);

        TMP_Text current = parts[index];
        if (current == null) { AdvanceToNextPart(); return; }

        current.gameObject.SetActive(true);

        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        _typeCoroutine = StartCoroutine(TypeText(current));
    }

    private IEnumerator TypeText(TMP_Text text)
    {
        _isTyping      = true;
        _skipRequested = false;

        string fullText = text.text;
        text.text = fullText;
        text.maxVisibleCharacters = 0;
        text.ForceMeshUpdate();

        int total = text.textInfo.characterCount;

        for (int i = 0; i <= total; i++)
        {
            if (_skipRequested)
            {
                text.maxVisibleCharacters = total;
                break;
            }
            text.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(charDelay);
        }

        text.maxVisibleCharacters = total;
        _isTyping      = false;
        _skipRequested = false;
    }

    /// <summary>
    /// Advances to the next part. Idempotent per-part: only the first call
    /// for a given part index takes effect. Subsequent calls (e.g. a stale
    /// coroutine waking up late) are silently ignored.
    /// </summary>
    private void AdvanceToNextPart()
    {
        if (_hasAdvancedFromCurrentPart) return;
        _hasAdvancedFromCurrentPart = true;

        _currentPart++;
        ShowPart(_currentPart);
    }

    private void OnAllPartsShown()
    {
        _tutorialDone = true;
        if (tutorialBubble != null) tutorialBubble.SetActive(false);
        if (doneButton != null) doneButton.gameObject.SetActive(true);

        // Unlock tutorial achievement
        if (AchievementManager.Instance != null)
            AchievementManager.Instance.Unlock(AchievementID.CompleteTutorial);

        StartCoroutine(ForcedRedirectTimer());
    }

    // ── Scene transition ───────────────────────────────────────────────

    private IEnumerator ForcedRedirectTimer()
    {
        yield return new WaitForSecondsRealtime(3f);
        FinishTutorial();
    }

    public void FinishTutorial()
    {
        if (_isFinishing) return;
        _isFinishing = true;

        if (sceneController != null)
            sceneController.SceneChange(mainMenuName);
        else
            SceneManager.LoadScene(mainMenuName);
    }

    // ── Legacy ─────────────────────────────────────────────────────────
    public void AdvanceTutorial() => AdvanceToNextPart();
    public void TowerPlaced()     => OnDefenseTowerPlaced();
    public void GoButtonClicked() => OnGoButtonPressed();
    public void WaveFinished()    { _currentPart = parts.Length; OnAllPartsShown(); }
}