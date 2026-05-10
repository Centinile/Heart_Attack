using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Jukebox : MonoBehaviour
{
    public static Jukebox Instance;  // Optional: Keep for easy access, but not persistent

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;  // Assign an AudioSource component to this GameObject

    [Header("Music Lists")]
    [SerializeField] private List<AudioClip> mainMenuMusic = new List<AudioClip>();  // List of main menu tracks
    [SerializeField] private List<AudioClip> gameMusic = new List<AudioClip>();      // List of game tracks

    [Header("Settings")]
    [SerializeField] private bool loopMusic = false;  // Whether to loop the current track (now false by default for cycling)
    [SerializeField] private float fadeDuration = 1f;  // Time to fade between tracks
    [SerializeField] private bool shuffleMusic = true;  // Whether to shuffle the playlist or play sequentially

    private List<AudioClip> currentMusicList = new List<AudioClip>();  // The list currently being played
    private List<AudioClip> shuffledList = new List<AudioClip>();  // Shuffled version of the list
    private int currentIndex = 0;  // Current index in the playlist
    private AudioClip currentClip;
    private bool isTransitioning = false;  // Flag to prevent rapid cycling during fades

    void Awake()
    {
        // Persistent singleton - only one Jukebox across all scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    Debug.LogError("Jukebox: No AudioSource found! Please add one to the GameObject.");
                }
            }
            
            // Listen for scene changes
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);  // Destroy duplicates
        }
    }

    void Start()
    {
        // Play music for the initial scene
        SwitchMusicForScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Switch music when scene changes
        SwitchMusicForScene(scene.name);
    }

    private void SwitchMusicForScene(string sceneName)
    {
        string lowerSceneName = sceneName.ToLower();

        if (lowerSceneName == "main menu")
        {
            PlayMusicFromList(mainMenuMusic);
        }
        else if (lowerSceneName == "level1" || lowerSceneName == "tutorialscene")
        {
            PlayMusicFromList(gameMusic);
        }
        else
        {
            Debug.LogWarning("Jukebox: Unknown scene type for '" + sceneName + "'. No music played.");
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        // Check if the current track has finished playing and we're not transitioning
        if (audioSource != null && !audioSource.isPlaying && currentMusicList.Count > 0 && !loopMusic && !isTransitioning)
        {
            PlayNextTrack();
        }
    }

    // Public function to choose and play music from a specific list
    public void PlayMusicFromList(List<AudioClip> musicList)
    {
        if (musicList == null || musicList.Count == 0)
        {
            Debug.LogWarning("Jukebox: No music in the list to play.");
            return;
        }

        currentMusicList = new List<AudioClip>(musicList);  // Copy the list
        currentIndex = 0;

        if (shuffleMusic)
        {
            shuffledList = new List<AudioClip>(currentMusicList);
            ShuffleList(shuffledList);
            PlayMusic(shuffledList[currentIndex]);
        }
        else
        {
            PlayMusic(currentMusicList[currentIndex]);
        }
    }

    // Play the next track in the list
    private void PlayNextTrack()
    {
        if (currentMusicList.Count == 0) return;

        currentIndex = (currentIndex + 1) % (shuffleMusic ? shuffledList.Count : currentMusicList.Count);

        if (shuffleMusic)
        {
            if (currentIndex == 0)  // If we've gone through the shuffled list, reshuffle
            {
                ShuffleList(shuffledList);
            }
            PlayMusic(shuffledList[currentIndex]);
        }
        else
        {
            PlayMusic(currentMusicList[currentIndex]);
        }
    }

    // Public function to play a specific music clip
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || audioSource == null)
        {
            Debug.LogWarning("Jukebox: Cannot play music - clip or AudioSource is null.");
            return;
        }

        if (currentClip != clip)
        {
            currentClip = clip;
            StartCoroutine(FadeAndPlay(clip));  // Optional fade for smooth transitions
            Debug.Log("Jukebox: Playing music: " + clip.name);
        }
    }

    // Coroutine to fade out current music and fade in new one
    private IEnumerator FadeAndPlay(AudioClip newClip)
    {
        isTransitioning = true;  // Prevent Update() from triggering during fade

        // Fade out current music
        float startVolume = audioSource.volume;
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            if (audioSource.isPlaying)
                audioSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }

        // Play new music
        audioSource.clip = newClip;
        audioSource.loop = loopMusic;  // Only loop if enabled
        audioSource.Play();

        // Fade in new music
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(0f, startVolume, t / fadeDuration);
            yield return null;
        }

        isTransitioning = false;  // Allow Update() to check for end again
    }

    // Shuffle the list using Fisher-Yates algorithm
    private void ShuffleList(List<AudioClip> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            AudioClip temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    // Public function to stop music
    public void StopMusic()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            Debug.Log("Jukebox: Music stopped.");
        }
    }

    // Public function to pause/resume music
    public void PauseMusic(bool pause)
    {
        if (audioSource != null)
        {
            if (pause)
            {
                audioSource.Pause();
                Debug.Log("Jukebox: Music paused.");
            }
            else
            {
                audioSource.UnPause();
                Debug.Log("Jukebox: Music resumed.");
            }
        }
    }

    // Optional: Get the current clip for debugging
    public AudioClip GetCurrentClip() => currentClip;
}
