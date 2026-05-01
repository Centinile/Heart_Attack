using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private SceneController sceneController;
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Level Setting Toggles")]
    [SerializeField] private Toggle acidRainToggle;
    [SerializeField] private Toggle fogOfWarToggle;
    [SerializeField] private Toggle randomWavesToggle;
    [SerializeField] private Toggle statRampingToggle;
    [SerializeField] private Toggle noBreaksToggle;

    private const string KEY_ACID_RAIN = "EnableAcidRain";
    private const string KEY_FOG_OF_WAR = "EnableFogOfWar";
    private const string KEY_RANDOM_WAVES = "EnableRandomWaves";
    private const string KEY_STAT_RAMPING = "EnableStatRamping";
    private const string KEY_NO_BREAKS = "EnableNoBreaks";

    private void Start()
    {
        // Load saved toggle states (default false if never set)
        acidRainToggle.isOn     = PlayerPrefs.GetInt(KEY_ACID_RAIN, 0) == 1;
        fogOfWarToggle.isOn     = PlayerPrefs.GetInt(KEY_FOG_OF_WAR, 0) == 1;
        randomWavesToggle.isOn  = PlayerPrefs.GetInt(KEY_RANDOM_WAVES, 0) == 1;
        statRampingToggle.isOn  = PlayerPrefs.GetInt(KEY_STAT_RAMPING, 0) == 1;
        noBreaksToggle.isOn     = PlayerPrefs.GetInt(KEY_NO_BREAKS, 0) == 1;

        // Save on change
        acidRainToggle.onValueChanged.AddListener(v    => PlayerPrefs.SetInt(KEY_ACID_RAIN, v ? 1 : 0));
        fogOfWarToggle.onValueChanged.AddListener(v    => PlayerPrefs.SetInt(KEY_FOG_OF_WAR, v ? 1 : 0));
        randomWavesToggle.onValueChanged.AddListener(v => PlayerPrefs.SetInt(KEY_RANDOM_WAVES, v ? 1 : 0));
        statRampingToggle.onValueChanged.AddListener(v => PlayerPrefs.SetInt(KEY_STAT_RAMPING, v ? 1 : 0));
        noBreaksToggle.onValueChanged.AddListener(v    => PlayerPrefs.SetInt(KEY_NO_BREAKS, v ? 1 : 0));
    }

    public void StartGame()
    {
        PlayerPrefs.Save(); // flush to disk before scene transition
        sceneController.SceneChange(gameSceneName);
    }
}