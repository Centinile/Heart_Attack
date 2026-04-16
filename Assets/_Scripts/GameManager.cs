using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

//have dependencies and references flow to the gamemanager instead of the other way
//the game manager is independent from other scripts
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum GameState //define the different states of the game
    {
        Gameplay, RestingPhase, Paused, Gameover
    }

    public GameState currentState; //stores the current state of the game
    public GameState previousState; //store the previous state of the game

    [Header("Screens")]
    public GameObject pauseScreen;
    public GameObject resultsScreen;
    //public GameObject selectionBorder;
    public GameObject uiScreen;


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


    //Helpers
    public bool isGameOver { get { return currentState == GameState.Gameover; } }

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
    }

    void Start()
    {
        currentNutrients = startingNutrients;
        currentHydration = startingHydration;
        UpdateResourceUI();
    }

    void OnDestroy()
    {
        Debug.Log($"[GameManager] OnDestroy: Destroyed in scene '{gameObject.scene.name}' at frame {Time.frameCount}, time {Time.time}. Call stack:\n{System.Environment.StackTrace}");
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        switch (currentState) //switch case for the current game state,
                              //codes under the cases only run when that specific gamestate is currently active, very easy to handle sh*t
        {
            case GameState.Gameplay:
                CheckForPauseAndResume();
                break;

            case GameState.Paused:
                CheckForPauseAndResume();
                break;

            case GameState.RestingPhase:
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

        //set the Game Over Variables here
        ChangeState(GameState.Gameover);
        Time.timeScale = 0f;
        DisplayResults();

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
        resultsScreen.SetActive(true);
        uiScreen.SetActive(false);
    }

    void DisableScreens()
    {
        pauseScreen.SetActive(false);
        resultsScreen.SetActive(false);
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

}