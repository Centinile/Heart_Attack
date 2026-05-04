using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.VFX;

//have dependencies and references flow to the gamemanager instead of the other way
//the game manager is independent from other scripts
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public static System.Action OnGameOver;
    public static System.Action OnVictory;

    public enum GameState //define the different states of the game
    {
        Gameplay, RestingPhase, Paused, Gameover, Victory
    }

    public GameState currentState; //stores the current state of the game
    public GameState previousState; //store the previous state of the game

    [Header("Screens")]
    public GameObject pauseScreen;
    public GameObject GameOverScreen;
    //public GameObject selectionBorder;
    public GameObject uiScreen;
    public GameObject victoryScreen;


    [Header("Stat Displays")]
    public TMP_Text currentNutrientsDisplay; //currency
    public TMP_Text currentHydrationDisplay; //energy

    //[Header("Results Screen Displays")]

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

    private float _acidRainTimer;


    //Helpers
    public bool isGameOver { get { return currentState == GameState.Gameover; } }

    [Header("Wave Goals")]
    [SerializeField] private int easyWaves = 1;
    [SerializeField] private int mediumWaves = 20;
    [SerializeField] private int hardWaves = 30;
    public int WavesToWin { get; private set; }
    public VisualEffect FogOfWarEffect;

    private const string KEY_ACID_RAIN     = "EnableAcidRain";
    private const string KEY_FOG_OF_WAR    = "EnableFogOfWar";
    private const string KEY_RANDOM_WAVES  = "EnableRandomWaves";
    private const string KEY_STAT_RAMPING  = "EnableStatRamping";
    private const string KEY_NO_BREAKS     = "EnableNoBreaks";
    private const string KEY_DIFFICULTY = "Difficulty"; // 0=Easy, 1=Medium, 2=Hard

    void Awake()
    {

        if (Instance == null) //the usual GameManager Instance checker
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Extra " + this + "Deleted");
            Destroy(gameObject);
        }

        DisableScreens();
        uiScreen.SetActive(true);

        //enableAcidRain = PlayerPrefs.GetInt(KEY_ACID_RAIN, 0) == 1;
        //enableFogOfWar = PlayerPrefs.GetInt(KEY_FOG_OF_WAR, 0) == 1;
        enableRandomWaves = PlayerPrefs.GetInt(KEY_RANDOM_WAVES, 0) == 1;
        enableStatRamping = PlayerPrefs.GetInt(KEY_STAT_RAMPING, 0) == 1;
        enableNoBreaks = PlayerPrefs.GetInt(KEY_NO_BREAKS, 0) == 1;
        int difficulty = PlayerPrefs.GetInt(KEY_DIFFICULTY, 0); // default Easy
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
        _acidRainTimer = acidRainInterval;
        currentState = GameState.RestingPhase;


        UpdateResourceUI();

        if (enableFogOfWar && FogOfWarEffect != null)
        {
            FogOfWarEffect.gameObject.SetActive(true);
        }
    }

    void OnDestroy()
    {
        Debug.Log($"[GameManager] OnDestroy: Destroyed in scene '{gameObject.scene.name}' at frame {Time.frameCount}, time {Time.time}. Call stack:\n{System.Environment.StackTrace}");
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void CheckVictory(int wavesCleared)
    {
        if (WavesToWin > 0 && wavesCleared >= WavesToWin)
            WinGame();
    }

    public void WinGame()
    {
        if (currentState == GameState.Gameover || currentState == GameState.Victory) return;
        ChangeState(GameState.Victory);
        Time.timeScale = 0f;
        OnVictory?.Invoke();
        if (victoryScreen != null)
        {
            victoryScreen.SetActive(true); // Results parent

            // Find and activate Congratulations child directly
            Transform congratsChild = victoryScreen.transform.Find("Congratulations!");
            if (congratsChild == null)
                congratsChild = victoryScreen.transform.Find("Congratulations");
            if (congratsChild != null)
                congratsChild.gameObject.SetActive(true);
        }
        uiScreen.SetActive(false);
        Debug.Log("<color=green><b>You Win!</b></color>");
    }

    void Update()
    {
        switch (currentState) //switch case for the current game state,
                              //codes under the cases only run when that specific gamestate is currently active, very easy to handle sh*t
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

    public void ChangeState(GameState newState) //defines the method to change the state of the game
    {
        previousState = currentState;
        currentState = newState;
    }

    public void PauseGame()
    {
        if (currentState != GameState.Paused)
        {
            ChangeState(GameState.Paused);
            Time.timeScale = 0f;
            pauseScreen.SetActive(true);
            Debug.Log("Game is Paused");
        }
    }

    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            ChangeState(previousState);
            Time.timeScale = 1f;
            pauseScreen.SetActive(false);
            Debug.Log("Game is Resumed");
        }
    }

    public void CheckForPauseAndResume()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton9))
        {
            if (currentState == GameState.Paused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void GameOver()
    {
        if (currentState == GameState.Victory) return;
        ChangeState(GameState.Gameover);
        Time.timeScale = 0f;
        OnGameOver?.Invoke();
        DisplayResults(); // loss screen
    }

    public void EnterRestingPhase()
    {
        if (currentState == GameState.Gameover) return;
        
        ChangeState(GameState.RestingPhase);
        Debug.Log("<color=green>Resting Phase Started.</color>");
        // Here you could trigger a UI animation or sound effect
    }

    public void EnterGameplayPhase()
    {
        if (currentState == GameState.Gameover) return;

        ChangeState(GameState.Gameplay);
        Debug.Log("<color=red>Wave Started!</color>");
    }

    public void RestingPhase()
    {
        
    }

    public void DisplayResults()
    {
        // Activate the Results parent so children can show
        if (GameOverScreen != null)
        {
            GameOverScreen.SetActive(true); // Results parent

            // Find and activate Game Over Text child directly
            Transform gameOverChild = GameOverScreen.transform.Find("Game Over Text");
            if (gameOverChild != null)
                gameOverChild.gameObject.SetActive(true);
        }
        uiScreen.SetActive(false);
    }

    void DisableScreens()
    {
        pauseScreen.SetActive(false);
        victoryScreen.SetActive(false);
        GameOverScreen.SetActive(false);
    }


    public bool CanAfford(float amount)
    {
        return currentNutrients >= amount;
    }

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
        if (usedHydration + amount > currentHydration)
            return false;

        usedHydration += amount;
        UpdateResourceUI();
        return true;
    }

    public void ReleaseHydration(float amount)
    {
        usedHydration -= amount;
        usedHydration = Mathf.Max(0, usedHydration);
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

    public void RegisterPoweredBuilding(Building building)
    {
        if (!poweredBuildings.Contains(building))
            poweredBuildings.Add(building);
    }

    public void RegisterUnpoweredBuilding(Building building)
    {
        if (!unpoweredBuildings.Contains(building))
            unpoweredBuildings.Add(building);
    }

    public void UnregisterPoweredBuilding(Building building)
    {
        poweredBuildings.Remove(building);
    }

    public void UnregisterUnpoweredBuilding(Building building)
    {
        unpoweredBuildings.Remove(building);
    }

    private void TryRestorePower()
    {
        // Walk forwards (first to lose power = first to regain it)
        for (int i = 0; i < unpoweredBuildings.Count; i++)
        {
            Building b = unpoweredBuildings[i];
            if (b == null) { unpoweredBuildings.RemoveAt(i--); continue; }

            if (GameManager.Instance.TryUseHydration(b.Data.HydrationCost))
            {
                unpoweredBuildings.RemoveAt(i--);
                b.SetPowered(true);
            }
        }
    }

    private void ScheduleRestorePower()
    {
        restorePowerPending = true;
    }

    // Called when a hydration source is destroyed and capacity drops
    public void OnHydrationCapacityReduced(float lostCapacity)
    {
        float hydrationToReclaim = usedHydration - currentHydration;

        // Collect first, don't modify the list mid-iteration
        List<Building> toDepower = new List<Building>();
        for (int i = poweredBuildings.Count - 1; i >= 0 && hydrationToReclaim > 0; i--)
        {
            Building b = poweredBuildings[i];
            if (b == null) { poweredBuildings.RemoveAt(i); continue; }

            toDepower.Add(b);
            hydrationToReclaim -= b.Data.HydrationCost;
        }

        // Now safely depower — SetPowered will modify poweredBuildings here, not mid-loop
        foreach (Building b in toDepower)
        {
            usedHydration -= b.Data.HydrationCost;
            usedHydration = Mathf.Max(0, usedHydration);
            b.SetPowered(false); // This calls UnregisterPoweredBuilding safely now
        }

        UpdateResourceUI();
        // Don't call ScheduleRestorePower here — we just lost capacity, nothing to restore
    }

    private void TickAcidRain()
    {
        _acidRainTimer -= Time.deltaTime;
        if (_acidRainTimer > 0f) return;

        _acidRainTimer = acidRainInterval;

        // Damage all powered buildings except Heart
        for (int i = poweredBuildings.Count - 1; i >= 0; i--)
        {
            Building b = poweredBuildings[i];
            if (b == null) { poweredBuildings.RemoveAt(i); continue; }
            if (b.StructureType == StructureType.Heart) continue;
            b.TakeDamage(acidRainDamage);
        }

        // Damage all unpowered buildings except Heart
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