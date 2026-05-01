using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStarter : MonoBehaviour
{
    void Start()
    {
        int hasPlayed = PlayerPrefs.GetInt("TutorialPlayed", 0);

        if (hasPlayed == 0)
        {
            PlayerPrefs.SetInt("TutorialPlayed", 1);
            PlayerPrefs.Save();
            SceneManager.LoadScene("TutorialScene");
        }
        else
        {
            SceneManager.LoadScene("MainMenuScene");
        }
    }
}