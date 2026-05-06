using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Pool Settings")]
    [SerializeField] private int poolSize = 20;
    [SerializeField] private AudioSource loopingSourcePrefab;

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

        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source = new GameObject($"PooledAudio_{i}").AddComponent<AudioSource>();
            source.transform.SetParent(transform);
            source.playOnAwake = false;
            _pool.Add(source);
        }
    }

    // ── One-shot ───────────────────────────────────────────────────────

    public void PlayOneShot(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSource();
        if (source == null) return;

        source.transform.position = position;
        source.clip = clip;
        source.volume = volume;
        source.loop = false;
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

    // ownerID should be GetInstanceID() of the object owning the loop
    public void PlayLooping(AudioClip clip, Vector3 position, int ownerID, float volume = 1f)
    {
        if (clip == null) return;
        if (_loopingSources.ContainsKey(ownerID)) return; // already playing

        AudioSource source = GetAvailableSource();
        if (source == null) return;

        source.transform.position = position;
        source.clip = clip;
        source.volume = volume;
        source.loop = true;
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

        // Pool exhausted — expand by one rather than silently failing
        AudioSource newSource = new GameObject($"PooledAudio_extra").AddComponent<AudioSource>();
        newSource.transform.SetParent(transform);
        newSource.playOnAwake = false;
        _pool.Add(newSource);
        return newSource;
    }
}