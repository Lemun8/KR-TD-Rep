using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// Plays all the game's sound. Anything that needs a sound calls AudioManager.Play(SfxEvent.Something).
// Which clip belongs to which event is set in ONE place: the Sounds list on this component.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Serializable]
    public class SoundSet
    {
        public SfxEvent sfxEvent;

        [Tooltip("Drop one or more clips here. With several, a random one plays each time.")]
        public AudioClip[] clips;

        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("Random pitch change on every play, so repeated sounds don't feel robotic.")]
        [Range(0f, 0.3f)] public float pitchVariation = 0.05f;
    }

    [Header("References")]
    [Tooltip("Leave empty to find it automatically. Not needed in the main menu.")]
    [SerializeField] private WaveManager waveManager;

    [Header("Music")]
    [SerializeField] private AudioClip music;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.4f;

    [Header("Sound effects")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;
    [Tooltip("How many sounds can play at the same time.")]
    [Min(4)]
    [SerializeField] private int voices = 16;
    [Tooltip("The same sound can't start again within this many seconds. Stops 20 goblins dying at once from becoming a roar.")]
    [SerializeField] private float minTimeBetweenSame = 0.06f;
    [SerializeField] private List<SoundSet> sounds = new List<SoundSet>();

    private readonly Dictionary<SfxEvent, SoundSet> lookup = new Dictionary<SfxEvent, SoundSet>();
    private readonly Dictionary<SfxEvent, float> lastPlayed = new Dictionary<SfxEvent, float>();

    private AudioSource musicSource;
    private AudioSource[] voiceSources;
    private int nextVoice;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        foreach (SoundSet set in sounds)
        {
            if (set != null && set.sfxEvent != SfxEvent.None) lookup[set.sfxEvent] = set;
        }

        musicSource = CreateSource("Music");
        musicSource.loop = true;

        voiceSources = new AudioSource[voices];
        for (int i = 0; i < voices; i++) voiceSources[i] = CreateSource("Sfx " + i);

        VolumeSettings.Changed += ApplyMusicVolume;
    }

    private void Start()
    {
        if (music != null)
        {
            musicSource.clip = music;
            ApplyMusicVolume();
            musicSource.Play();
        }

        // The main menu has no WaveManager, and that is fine: it just means no wave start sound
        if (waveManager == null) waveManager = FindFirstObjectByType<WaveManager>();
        if (waveManager != null) waveManager.WaveStarted += HandleWaveStarted;
    }

    private void OnDestroy()
    {
        VolumeSettings.Changed -= ApplyMusicVolume;

        if (Instance == this) Instance = null;
        if (waveManager != null) waveManager.WaveStarted -= HandleWaveStarted;
    }

    // ---------- Public API ----------

    // Safe to call from anywhere: does nothing if there is no AudioManager or no clip for that event
    public static void Play(SfxEvent sfxEvent)
    {
        if (Instance != null) Instance.PlayInternal(sfxEvent);
    }

    // ---------- Internals ----------

    private void PlayInternal(SfxEvent sfxEvent)
    {
        if (sfxEvent == SfxEvent.None) return;
        if (!lookup.TryGetValue(sfxEvent, out SoundSet set)) return;
        if (set.clips == null || set.clips.Length == 0) return;

        // Same sound twice in a blink: skip the second one
        float now = Time.unscaledTime;
        if (lastPlayed.TryGetValue(sfxEvent, out float last) && now - last < minTimeBetweenSame) return;
        lastPlayed[sfxEvent] = now;

        AudioClip clip = set.clips[Random.Range(0, set.clips.Length)];
        if (clip == null) return;

        // Round robin: if every voice is busy, the oldest one gets replaced
        AudioSource source = voiceSources[nextVoice];
        nextVoice = (nextVoice + 1) % voiceSources.Length;

        source.clip = clip;
        source.volume = set.volume * sfxVolume * VolumeSettings.Sfx;
        source.pitch = 1f + Random.Range(-set.pitchVariation, set.pitchVariation);
        source.Play();
    }

    private void ApplyMusicVolume()
    {
        if (musicSource != null) musicSource.volume = musicVolume * VolumeSettings.Bgm;
    }

    private void HandleWaveStarted(int wave)
    {
        PlayInternal(SfxEvent.WaveStart);
    }

    private AudioSource CreateSource(string sourceName)
    {
        GameObject child = new GameObject(sourceName);
        child.transform.SetParent(transform, false);

        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;   // 2D sound: same volume everywhere, no distance fade
        return source;
    }
}