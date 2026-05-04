using UnityEngine;

/// <summary>
/// Attach to the Congratulations panel.
/// Wire:
///   Main Menu button -> OnClick -> VictoryScreenManager.GoToMainMenu()
///   Endless button   -> OnClick -> VictoryScreenManager.ContinueEndless()
/// </summary>
public class VictoryScreenManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SceneController sceneController;
    [SerializeField] private WaveManager waveManager;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    /// <summary>Called by the Main Menu button.</summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        sceneController.SceneChange(mainMenuSceneName);
    }

    /// <summary>
    /// Called by the Endless / Continue button.
    /// Hides the victory screen, restores time, and lets WaveManager keep going.
    /// </summary>
    public void ContinueEndless()
    {
        // Hide this panel
        gameObject.SetActive(false);

        // Restore the UI screen
        GameManager.Instance.uiScreen.SetActive(true);

        // Resume time
        Time.timeScale = 1f;

        // Put GameManager back into resting phase so the GO button works again
        GameManager.Instance.ChangeState(GameManager.GameState.RestingPhase);

        // Clear the win condition so waves can continue forever
        // We do this by telling WaveManager to keep going in freeplay
        if (waveManager != null)
        {
            waveManager.freeplayMode = true;

            // Make the start wave button interactable again
            if (waveManager.startWaveButton != null)
                waveManager.startWaveButton.interactable = true;
        }

        Debug.Log("<color=cyan>[Victory] Continuing in Endless/Freeplay mode.</color>");
    }
}
