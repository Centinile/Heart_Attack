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
        UpdateResourceUI();
    }

    public void ModifyMaxHydration(float amount)
    {
        currentHydration += amount;
        UpdateResourceUI();
    }

    private void UpdateResourceUI()
    {
        currentNutrientsDisplay.text = currentNutrients.ToString("0");
        currentHydrationDisplay.text = $"{usedHydration:0}/{currentHydration:0}";
    }

}