using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BGAudioManager : MonoBehaviour
{
    public static BGAudioManager Instance;

    [Header("Music Tracks")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip[] level1Tracks;   // 3 tracks — one picked randomly
    [SerializeField] private AudioClip tutorialMusic;

    [Header("Settings")]
    [SerializeField] private float volume      = 1f;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Scene Names (must match exactly)")]
    [SerializeField] private string mainMenuSceneName  = "Main_Menu";
    [SerializeField] private string level1SceneName    = "Level1";
    [SerializeField] private string tutorialSceneName  = "TutorialScene";

    private AudioSource _audioSource;
    private string      _currentScene = "";

    // ── Lifecycle ──────────────────────────────────────────────────────

    void Awake()
    {
        // Singleton — persist across scenes
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Must be a root object for DontDestroyOnLoad to work
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.loop   = true;
        _audioSource.volume = volume;
    }

    void Start()
    {
        // Play music for the scene we start in, since OnSceneLoaded
        // already fired before Awake completed on the first scene
        _currentScene = SceneManager.GetActiveScene().name;

        AudioClip clip = null;
        if (_currentScene == mainMenuSceneName)
            clip = mainMenuMusic;
        else if (_currentScene == level1SceneName)
            clip = PickRandom(level1Tracks);
        else if (_currentScene == tutorialSceneName)
            clip = tutorialMusic;

        if (clip != null)
        {
            _audioSource.clip   = clip;
            _audioSource.volume = volume;
            _audioSource.Play();
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ── Scene change handler ───────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string name = scene.name;

        // Don't restart the same track if re-entering the same scene type
        if (name == _currentScene) return;
        _currentScene = name;

        AudioClip clip = null;

        if (name == mainMenuSceneName)
            clip = mainMenuMusic;
        else if (name == level1SceneName)
            clip = PickRandom(level1Tracks);
        else if (name == tutorialSceneName)
            clip = tutorialMusic;

        if (clip != null)
            StartCoroutine(CrossFadeTo(clip));
        else
            StartCoroutine(FadeOut());
    }

    // ── Audio helpers ──────────────────────────────────────────────────

    private AudioClip PickRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }

    private IEnumerator CrossFadeTo(AudioClip newClip)
    {
        // Fade out current track
        if (_audioSource.isPlaying)
        {
            float startVol = _audioSource.volume;
            float elapsed  = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _audioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeDuration);
                yield return null;
            }
            _audioSource.Stop();
        }

        // Start new track and fade in
        _audioSource.clip   = newClip;
        _audioSource.volume = 0f;
        _audioSource.Play();

        float fadeElapsed = 0f;
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.unscaledDeltaTime;
            _audioSource.volume = Mathf.Lerp(0f, volume, fadeElapsed / fadeDuration);
            yield return null;
        }
        _audioSource.volume = volume;
    }

    private IEnumerator FadeOut()
    {
        float startVol = _audioSource.volume;
        float elapsed  = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _audioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeDuration);
            yield return null;
        }
        _audioSource.Stop();
    }

    // ── Public controls ────────────────────────────────────────────────

    public void SetVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        _audioSource.volume = volume;
    }
}