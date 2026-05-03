using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject step1Panel;
    public GameObject step2Panel;
    public GameObject step3Panel;

    [Header("Buttons")]
    public Button step1NextButton;
    public Button step2NextButton;
    public Button doneButton;

    [Header("References")]
    public SceneController sceneController;
    public WaveManager waveManager;

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuName = "Main Menu";

    private int currentStep = 1;
    private bool isFinishing = false;

    void Start()
    {
        // Ensure all buttons start hidden
        if (step1NextButton != null) step1NextButton.gameObject.SetActive(false);
        if (step2NextButton != null) step2NextButton.gameObject.SetActive(false);
        if (doneButton != null) doneButton.gameObject.SetActive(false);
        
        ShowCurrentStep();
    }

    public void AdvanceTutorial()
    {
        currentStep++;
        ShowCurrentStep();
    }

    private void ShowCurrentStep()
    {
        if (step1Panel != null) step1Panel.SetActive(currentStep == 1);
        if (step2Panel != null) step2Panel.SetActive(currentStep == 2);
        if (step3Panel != null) step3Panel.SetActive(currentStep == 3);
    }

    public void TowerPlaced()
    {
        if (currentStep == 1 && step1NextButton != null)
        {
            step1NextButton.gameObject.SetActive(true);
        }
    }

    public void GoButtonClicked()
    {
        if (currentStep == 2)
        {
            if (step2NextButton != null) step2NextButton.gameObject.SetActive(true);
            
            if (waveManager != null)
            {
                waveManager.StartWave();
            }
        }
    }

    public void WaveFinished()
    {
        // Immediately jump to the final step UI
        currentStep = 3;
        ShowCurrentStep();
        
        if (doneButton != null)
        {
            doneButton.gameObject.SetActive(true);
        }

        // Start the automatic 3-second redirect
        StartCoroutine(ForcedRedirectTimer());
    }

    private IEnumerator ForcedRedirectTimer()
    {
        // WaitForSecondsRealtime works even if the game engine is paused
        yield return new WaitForSecondsRealtime(3f);
        FinishTutorial();
    }

    public void FinishTutorial()
    {
        if (isFinishing) return;
        isFinishing = true;

        // Try using your SceneController first
        if (sceneController != null)
        {
            Debug.Log($"TutorialManager: SceneController found. Loading '{mainMenuName}' via Loading Screen.");
            sceneController.SceneChange(mainMenuName);
        }
        else
        {
            // Emergency fallback if the SceneController slot is empty
            Debug.LogWarning("TutorialManager: SceneController missing. Loading Scene directly.");
            SceneManager.LoadScene(mainMenuName);
        }
    }
}