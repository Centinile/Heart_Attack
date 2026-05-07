using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Pool Settings")]
    [SerializeField] private int poolSize = 20;

    [Header("Music")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip[] gameplayTracks;
    [SerializeField] private float musicVolume = 0.5f;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameSceneName = "GameScene";

    private AudioSource _musicSource;
    private List<AudioClip> _shuffledTracks = new List<AudioClip>();
    private int _currentTrackIndex = 0;
    private Coroutine _gameplayMusicCoroutine;

    private List<AudioSource> _pool = new List<AudioSource>();
    private Dictionary<int, AudioSource> _loopingSources = new Dictionary<int, AudioSource>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Dedicated music source — separate from the SFX pool
        _musicSource = new GameObject("MusicSource").AddComponent<AudioSource>();
        _musicSource.transform.SetParent(transform);
        _musicSource.playOnAwake = false;
        _musicSource.loop = false;
        _musicSource.volume = musicVolume;

        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source = new GameObject($"PooledAudio_{i}").AddComponent<AudioSource>();
            source.transform.SetParent(transform);
            source.playOnAwake = false;
            _pool.Add(source);
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == mainMenuSceneName)
            PlayMainMenuMusic();
        else if (scene.name == gameSceneName)
            StartGameplayMusic();
    }

    // ── Music ──────────────────────────────────────────────────────────

    private void PlayMainMenuMusic()
    {
        StopGameplayMusic();

        if (mainMenuMusic == null) return;

        _musicSource.clip   = mainMenuMusic;
        _musicSource.loop   = true;
        _musicSource.volume = musicVolume;
        _musicSource.Play();
    }

    private void StartGameplayMusic()
    {
        _musicSource.Stop();
        _musicSource.loop = false;

        if (gameplayTracks == null || gameplayTracks.Length == 0) return;

        ShuffleTracks();
        _currentTrackIndex = 0;

        if (_gameplayMusicCoroutine != null) StopCoroutine(_gameplayMusicCoroutine);
        _gameplayMusicCoroutine = StartCoroutine(GameplayMusicRoutine());
    }

    private void StopGameplayMusic()
    {
        if (_gameplayMusicCoroutine != null)
        {
            StopCoroutine(_gameplayMusicCoroutine);
            _gameplayMusicCoroutine = null;
        }
        _musicSource.Stop();
    }

    private IEnumerator GameplayMusicRoutine()
    {
        while (true)
        {
            AudioClip track = _shuffledTracks[_currentTrackIndex];
            _musicSource.clip   = track;
            _musicSource.volume = musicVolume;
            _musicSource.Play();

            yield return new WaitForSeconds(track.length);

            _currentTrackIndex++;

            // Reshuffle when all tracks have played
            if (_currentTrackIndex >= _shuffledTracks.Count)
            {
                ShuffleTracks();
                _currentTrackIndex = 0;
            }
        }
    }

    private void ShuffleTracks()
    {
        _shuffledTracks = new List<AudioClip>(gameplayTracks);
        for (int i = 0; i < _shuffledTracks.Count; i++)
        {
            int randomIndex = Random.Range(i, _shuffledTracks.Count);
            AudioClip temp = _shuffledTracks[i];
            _shuffledTracks[i] = _shuffledTracks[randomIndex];
            _shuffledTracks[randomIndex] = temp;
        }
    }

    // Optional — call this from a volume slider in settings
    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        _musicSource.volume = volume;
    }

    // ── One-shot ───────────────────────────────────────────────────────

    public void PlayOneShot(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSource();
        if (source == null) return;

        source.transform.position = position;
        source.clip   = clip;
        source.volume = volume;
        source.loop   = false;
        source.Play();
        StartCoroutine(ReturnAfterPlay(source));
    }

    private IEnumerator ReturnAfterPlay(AudioSource source)
    {
        yield return new WaitForSeconds(source.clip.length);
        source.Stop();
        source.clip = null;
    }

    // ── Looping ────────────────────────────────────────────────────────

    public void PlayLooping(AudioClip clip, Vector3 position, int ownerID, float volume = 1f)
    {
        if (clip == null) return;
        if (_loopingSources.ContainsKey(ownerID)) return;

        AudioSource source = GetAvailableSource();
        if (source == null) return;

        source.transform.position = position;
        source.clip   = clip;
        source.volume = volume;
        source.loop   = true;
        source.Play();
        _loopingSources[ownerID] = source;
    }

    public void UpdateLoopingPosition(int ownerID, Vector3 position)
    {
        if (_loopingSources.TryGetValue(ownerID, out AudioSource source))
            source.transform.position = position;
    }

    public void StopLooping(int ownerID)
    {
        if (_loopingSources.TryGetValue(ownerID, out AudioSource source))
        {
            source.Stop();
            source.clip = null;
            source.loop = false;
            _loopingSources.Remove(ownerID);
        }
    }

    // ── Pool helpers ───────────────────────────────────────────────────

    private AudioSource GetAvailableSource()
    {
        foreach (var source in _pool)
            if (!source.isPlaying) return source;

        AudioSource newSource = new GameObject($"PooledAudio_extra").AddComponent<AudioSource>();
        newSource.transform.SetParent(transform);
        newSource.playOnAwake = false;
        _pool.Add(newSource);
        return newSource;
    }
}