using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.VFX;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public static System.Action OnGameOver;
    public static System.Action OnVictory;

    public enum GameState
    {
        Gameplay, RestingPhase, Paused, Gameover, Victory
    }

    public GameState currentState;
    public GameState previousState;

    [Header("Screens")]
    public GameObject pauseScreen;
    public GameObject GameOverScreen;
    public GameObject uiScreen;
    public GameObject victoryScreen;
    public GameObject waveClearPanel;

    [Header("Stat Displays")]
    public TMP_Text currentNutrientsDisplay;
    public TMP_Text currentHydrationDisplay;

    [Header("Resources")]
    [SerializeField] private float startingNutrients = 500f;
    [SerializeField] private float startingHydration = 50f;

    private float currentNutrients;
    private float currentHydration;

    public float CurrentNutrients => currentNutrients;
    public float CurrentHydration => currentHydration;

    private float usedHydration = 0f;
    private List<Building> poweredBuildings = new List<Building>();
    private List<Building> unpoweredBuildings = new List<Building>();
    private bool restorePowerPending = false;

    [Header("Level Settings")]
    public bool enableAcidRain = false;
    public bool enableRandomWaves = false;
    public bool enableFogOfWar = false;
    public bool enableStatRamping = false;
    public bool enableNoBreaks = false;

    [Header("Acid Rain Settings")]
    [SerializeField] private float acidRainDamage = 5f;
    [SerializeField] private float acidRainInterval = 10f;
    [SerializeField] public ParticleSystem acidRainEffect;
    private float _acidRainTimer;

    [Header("Wave Goals")]
    [SerializeField] private int easyWaves = 1;
    [SerializeField] private int mediumWaves = 20;
    [SerializeField] private int hardWaves = 30;
    public int WavesToWin { get; private set; }

    public VisualEffect FogOfWarEffect;

    public bool isGameOver => currentState == GameState.Gameover;

    private const string KEY_ACID_RAIN    = "EnableAcidRain";
    private const string KEY_FOG_OF_WAR   = "EnableFogOfWar";
    private const string KEY_RANDOM_WAVES = "EnableRandomWaves";
    private const string KEY_STAT_RAMPING = "EnableStatRamping";
    private const string KEY_NO_BREAKS    = "EnableNoBreaks";
    private const string KEY_DIFFICULTY   = "Difficulty";

    // ── Lifecycle ──────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Debug.LogWarning("Extra " + this + " Deleted");
            Destroy(gameObject);
            return;
        }

        DisableScreens();
        uiScreen.SetActive(true);

        enableAcidRain    = PlayerPrefs.GetInt(KEY_ACID_RAIN, 0) == 1;
        enableFogOfWar    = PlayerPrefs.GetInt(KEY_FOG_OF_WAR, 0) == 1;
        enableRandomWaves = PlayerPrefs.GetInt(KEY_RANDOM_WAVES, 0) == 1;
        enableStatRamping = PlayerPrefs.GetInt(KEY_STAT_RAMPING, 0) == 1;
        enableNoBreaks    = PlayerPrefs.GetInt(KEY_NO_BREAKS,    0) == 1;

        int difficulty = PlayerPrefs.GetInt(KEY_DIFFICULTY, 0);
        WavesToWin = difficulty switch
        {
            1 => mediumWaves,
            2 => hardWaves,
            _ => easyWaves
        };
    }

    void Start()
    {
        currentNutrients = startingNutrients;
        currentHydration = startingHydration;
        _acidRainTimer   = acidRainInterval;
        currentState     = GameState.RestingPhase;

        UpdateResourceUI();

        if (enableFogOfWar && FogOfWarEffect != null)
            FogOfWarEffect.gameObject.SetActive(true);

        if (enableAcidRain && acidRainEffect != null)
            acidRainEffect.gameObject.SetActive(true);

        // Subscribe to wave cleared event
        WaveManager.OnWaveCleared += ShowWaveClear;
    }

    void OnDestroy()
    {
        WaveManager.OnWaveCleared -= ShowWaveClear;

        if (Instance == this)
            Instance = null;
    }

    // ── Update ─────────────────────────────────────────────────────────

    void Update()
    {
        switch (currentState)
        {
            case GameState.Gameplay:
                CheckForPauseAndResume();
                if (enableAcidRain) TickAcidRain();
                break;

            case GameState.Paused:
                CheckForPauseAndResume();
                break;

            case GameState.RestingPhase:
                break;

            case GameState.Gameover:
                break;

            case GameState.Victory:
                break;

            default:
                Debug.LogWarning("State No Exist");
                break;
        }

        if (restorePowerPending)
        {
            restorePowerPending = false;
            TryRestorePower();
        }
    }

    // ── State ──────────────────────────────────────────────────────────

    public void ChangeState(GameState newState)
    {
        previousState = currentState;
        currentState  = newState;
    }

    public void EnterGameplayPhase()
    {
        if (currentState == GameState.Gameover) return;
        ChangeState(GameState.Gameplay);
        Debug.Log("<color=red>Wave Started!</color>");
    }

    public void EnterRestingPhase()
    {
        if (currentState == GameState.Gameover || currentState == GameState.Victory) return;
        ChangeState(GameState.RestingPhase);
        Debug.Log("<color=green>Resting Phase Started.</color>");
    }

    // ── Victory / Game Over ────────────────────────────────────────────

    /// <summary>Called by WaveManager after each wave to check win condition.</summary>
    private bool _victoryTriggered = false;
    public void CheckVictory(int wavesCleared)
    {
        if (_victoryTriggered) return;
        if (WavesToWin > 0 && wavesCleared >= WavesToWin)
        {
            _victoryTriggered = true;
            WinGame();
        }
    }

    public void WinGame()
    {
        if (currentState == GameState.Gameover || currentState == GameState.Victory) return;
        ChangeState(GameState.Victory);
        Time.timeScale = 0f;
        OnVictory?.Invoke();
        if (victoryScreen != null) victoryScreen.SetActive(true);
        uiScreen.SetActive(false);
        Debug.Log("<color=green><b>You Win!</b></color>");
    }

    public void GameOver()
    {
        if (currentState == GameState.Victory) return;
        ChangeState(GameState.Gameover);
        Time.timeScale = 0f;
        OnGameOver?.Invoke();
        DisplayResults();
    }

    public void DisplayResults()
    {
        if (GameOverScreen != null) GameOverScreen.SetActive(true);
        uiScreen.SetActive(false);
    }

    // ── Pause ──────────────────────────────────────────────────────────

    public void PauseGame()
    {
        if (currentState == GameState.Paused) return;
        ChangeState(GameState.Paused);
        Time.timeScale = 0f;
        if (pauseScreen != null) pauseScreen.SetActive(true);
    }

    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;
        ChangeState(previousState);
        Time.timeScale = 1f;
        if (pauseScreen != null) pauseScreen.SetActive(false);
    }

    public void CheckForPauseAndResume()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton9))
        {
            if (currentState == GameState.Paused) ResumeGame();
            else PauseGame();
        }
    }

    // ── Wave Clear Banner ──────────────────────────────────────────────

    public void ShowWaveClear()
    {
        // Don't show wave clear if game is over or won
        if (currentState == GameState.Gameover || currentState == GameState.Victory) return;

        // Don't show wave clear on the final wave — victory screen will show instead
        WaveManager waveManager = Object.FindFirstObjectByType<WaveManager>();
        if (waveManager != null && WavesToWin > 0 && waveManager.currentWaveIndex + 1 >= WavesToWin) return;

        StartCoroutine(WaveClearRoutine());
    }

    private IEnumerator WaveClearRoutine()
    {
        if (waveClearPanel == null) yield break;

        // Activate Results parent if Wave Clear is nested inside it
        Transform parent = waveClearPanel.transform.parent;
        bool wasParentInactive = parent != null && !parent.gameObject.activeSelf;
        if (wasParentInactive) parent.gameObject.SetActive(true);

        waveClearPanel.SetActive(true);
        yield return new WaitForSecondsRealtime(3f);
        waveClearPanel.SetActive(false);

        // Re-hide parent only if we activated it and no siblings are active
        if (wasParentInactive && parent != null)
        {
            bool anyActive = false;
            foreach (Transform child in parent)
                if (child.gameObject.activeSelf) { anyActive = true; break; }
            if (!anyActive) parent.gameObject.SetActive(false);
        }
    }

    // ── Screens ────────────────────────────────────────────────────────

    void DisableScreens()
    {
        if (pauseScreen    != null) pauseScreen.SetActive(false);
        if (victoryScreen  != null) victoryScreen.SetActive(false);
        if (GameOverScreen != null) GameOverScreen.SetActive(false);
        if (waveClearPanel != null) waveClearPanel.SetActive(false);
    }

    public void ContinueAfterVictory()
    {
        if (currentState != GameState.Victory) return;

        if (victoryScreen != null) victoryScreen.SetActive(false);
        if (uiScreen != null) uiScreen.SetActive(true);

        Time.timeScale = 1f;
        ChangeState(GameState.RestingPhase);

        // _victoryTriggered stays true — WinGame will never fire again this session

        WaveManager waveManager = FindFirstObjectByType<WaveManager>();
        if (waveManager != null)
        {
            if (waveManager.startWaveButton != null)
                waveManager.startWaveButton.interactable = true;
            waveManager.freeplayMode = true;
        }
    }

    // ── Resources ──────────────────────────────────────────────────────

    public bool CanAfford(float amount) => currentNutrients >= amount;

    public bool SpendNutrients(float amount)
    {
        if (!CanAfford(amount)) return false;
        currentNutrients -= amount;
        UpdateResourceUI();
        return true;
    }

    public void AddNutrients(float amount)
    {
        currentNutrients += amount;
        UpdateResourceUI();
    }

    public bool TryUseHydration(float amount)
    {
        if (usedHydration + amount > currentHydration) return false;
        usedHydration += amount;
        UpdateResourceUI();
        return true;
    }

    public void ReleaseHydration(float amount)
    {
        usedHydration = Mathf.Max(0, usedHydration - amount);
        ScheduleRestorePower();
        UpdateResourceUI();
    }

    public void ModifyMaxHydration(float amount)
    {
        currentHydration += amount;
        if (amount < 0 && usedHydration > currentHydration)
            OnHydrationCapacityReduced(-amount);
        else if (amount > 0)
            ScheduleRestorePower();
        UpdateResourceUI();
    }

    private void UpdateResourceUI()
    {
        currentNutrientsDisplay.text = currentNutrients.ToString("0");
        currentHydrationDisplay.text = $"{usedHydration:0}/{currentHydration:0}";
    }

    // ── Power ──────────────────────────────────────────────────────────

    public void RegisterPoweredBuilding(Building building)
    {
        if (!poweredBuildings.Contains(building)) poweredBuildings.Add(building);
    }

    public void RegisterUnpoweredBuilding(Building building)
    {
        if (!unpoweredBuildings.Contains(building)) unpoweredBuildings.Add(building);
    }

    public void UnregisterPoweredBuilding(Building building)   => poweredBuildings.Remove(building);
    public void UnregisterUnpoweredBuilding(Building building) => unpoweredBuildings.Remove(building);

    private void ScheduleRestorePower() => restorePowerPending = true;

    private void TryRestorePower()
    {
        for (int i = 0; i < unpoweredBuildings.Count; i++)
        {
            Building b = unpoweredBuildings[i];
            if (b == null) { unpoweredBuildings.RemoveAt(i--); continue; }
            if (TryUseHydration(b.Data.HydrationCost))
            {
                unpoweredBuildings.RemoveAt(i--);
                b.SetPowered(true);
            }
        }
    }

    public void OnHydrationCapacityReduced(float lostCapacity)
    {
        float toReclaim = usedHydration - currentHydration;
        List<Building> toDepower = new List<Building>();

        for (int i = poweredBuildings.Count - 1; i >= 0 && toReclaim > 0; i--)
        {
            Building b = poweredBuildings[i];
            if (b == null) { poweredBuildings.RemoveAt(i); continue; }
            toDepower.Add(b);
            toReclaim -= b.Data.HydrationCost;
        }

        foreach (Building b in toDepower)
        {
            usedHydration = Mathf.Max(0, usedHydration - b.Data.HydrationCost);
            b.SetPowered(false);
        }

        UpdateResourceUI();
    }

    // ── Acid Rain ──────────────────────────────────────────────────────

    private void TickAcidRain()
    {
        _acidRainTimer -= Time.deltaTime;
        if (_acidRainTimer > 0f) return;
        _acidRainTimer = acidRainInterval;

        for (int i = poweredBuildings.Count - 1; i >= 0; i--)
        {
            Building b = poweredBuildings[i];
            if (b == null) { poweredBuildings.RemoveAt(i); continue; }
            if (b.StructureType == StructureType.Heart) continue;
            b.TakeDamage(acidRainDamage);
        }

        for (int i = unpoweredBuildings.Count - 1; i >= 0; i--)
        {
            Building b = unpoweredBuildings[i];
            if (b == null) { unpoweredBuildings.RemoveAt(i); continue; }
            if (b.StructureType == StructureType.Heart) continue;
            b.TakeDamage(acidRainDamage);
        }

        Debug.Log($"[Acid Rain] Dealt {acidRainDamage} damage to all non-Heart buildings.");
    }
}