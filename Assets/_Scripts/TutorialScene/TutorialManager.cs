using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Tutorial Canvas")]
    [Tooltip("The Tutorial Canvas GameObject — hides during wave, shows after.")]
    public GameObject tutorialCanvas;

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

    // ── Private state ──────────────────────────────────────────────────
    private int  _currentPart = 0;
    private bool _isTyping    = false;
    private bool _skipRequested = false;
    private bool _tutorialDone  = false;
    private bool _isFinishing   = false;
    private bool _actionCompleted = false;
    private bool _hasAdvancedFromCurrentPart = false;
    private bool _waitingForWave = false;
    private Coroutine _typeCoroutine;

    // ── Public accessors ───────────────────────────────────────────────
    public int  CurrentPart => _currentPart;
    public bool IsTyping    => _isTyping;

    // ── Lifecycle ──────────────────────────────────────────────────────

    void Awake() => Instance = this;

    void Start()
    {
        if (step1NextButton != null) step1NextButton.gameObject.SetActive(false);
        if (step2NextButton != null) step2NextButton.gameObject.SetActive(false);
        if (doneButton      != null) doneButton.gameObject.SetActive(false);

        foreach (var p in parts) if (p != null) p.gameObject.SetActive(false);

        if (tutorialCanvas != null) tutorialCanvas.SetActive(true);

        // Subscribe to wave cleared so we know when the tutorial wave ends
        WaveManager.OnWaveCleared += OnTutorialWaveCleared;

        ShowPart(0);
    }

    void OnDestroy()
    {
        WaveManager.OnWaveCleared -= OnTutorialWaveCleared;
    }

    void Update()
    {
        if (_tutorialDone || _waitingForWave) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (_isTyping)
                _skipRequested = true;
            else if (!IsActionGatedPart(_currentPart))
                AdvanceToNextPart();
        }
    }

    // ── Action gates ───────────────────────────────────────────────────

    private bool IsActionGatedPart(int index)
    {
        return index == 2   // part 3:  place defense tower
            || index == 4   // part 5:  switch to resource tab
            || index == 5   // part 6:  place nutrient mine
            || index == 7   // part 8:  place water pump
            || index == 10; // part 11: press GO (starts wave)
    }

    public void OnDefenseTowerPlaced() { if (_currentPart == 2)  CompleteAction(); }
    public void OnResourceTabOpened()  { if (_currentPart == 4)  CompleteAction(); }
    public void OnMinePlaced()         { if (_currentPart == 5)  CompleteAction(); }
    public void OnWaterPumpPlaced()    { if (_currentPart == 7)  CompleteAction(); }

    /// <summary>Wire GO button OnClick to this.</summary>
    public void OnGoButtonPressed()
    {
        if (_currentPart == 10)
        {
            waveManager?.StartWave();
            CompleteAction();
        }
    }

    /// <summary>Called by WaveManager.OnWaveCleared after the tutorial wave ends.</summary>
    private void OnTutorialWaveCleared()
    {
        if (!_waitingForWave) return;
        _waitingForWave = false;

        // Re-show the tutorial canvas
        if (tutorialCanvas != null) tutorialCanvas.SetActive(true);

        // Jump to part 12 (index 11)
        _currentPart = 11;
        ShowPart(_currentPart);
    }

    private void CompleteAction()
    {
        if (_actionCompleted) return;
        _actionCompleted = true;
        _skipRequested   = true;

        // Part 11 (index 10) hides the tutorial canvas and waits for wave
        if (_currentPart == 10)
        {
            StartCoroutine(HideCanvasAndWaitForWave());
            return;
        }

        StartCoroutine(AutoAdvanceAfterAction());
    }

    private IEnumerator HideCanvasAndWaitForWave()
    {
        yield return new WaitForSecondsRealtime(0.3f);
        while (_isTyping) yield return null;

        // Hide the tutorial canvas while wave plays
        if (tutorialCanvas != null) tutorialCanvas.SetActive(false);

        _waitingForWave = true;
        // OnTutorialWaveCleared() will take over from here
    }

    private IEnumerator AutoAdvanceAfterAction()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        while (_isTyping) yield return null;
        AdvanceToNextPart();
    }

    // ── Core logic ─────────────────────────────────────────────────────

    private void ShowPart(int index)
    {
        if (index >= parts.Length) { OnAllPartsShown(); return; }

        _actionCompleted            = false;
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

        if (tutorialCanvas != null) tutorialCanvas.SetActive(false);

        // Show done button as a manual escape hatch
        if (doneButton != null) doneButton.gameObject.SetActive(true);

        if (AchievementManager.Instance != null)
            AchievementManager.Instance.Unlock(AchievementID.CompleteTutorial);

        // Auto-redirect after 3 seconds regardless of button press
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
    public void WaveFinished()    => OnTutorialWaveCleared();
}