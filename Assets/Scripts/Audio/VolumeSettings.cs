using System;
using UnityEngine;

public enum VolumeChannel
{
    Master,
    Bgm,
    Sfx
}

// Holds the three volume settings and saves them. Works in every scene, no object needed.
// Master is applied to ALL sound through AudioListener.volume.
// BGM and SFX are read by the AudioManager and multiplied into its own volumes.
public static class VolumeSettings
{
    private static readonly string[] Keys = { "Volume.Master", "Volume.Bgm", "Volume.Sfx" };

    private static readonly float[] values = { 1f, 1f, 1f };
    private static bool loaded;

    public static event Action Changed;

    public static float Bgm => Get(VolumeChannel.Bgm);
    public static float Sfx => Get(VolumeChannel.Sfx);

    // Loads the saved values before the first scene starts, so the menu already has the right master volume
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        EnsureLoaded();
    }

    public static float Get(VolumeChannel channel)
    {
        EnsureLoaded();
        return values[(int)channel];
    }

    public static void Set(VolumeChannel channel, float value)
    {
        EnsureLoaded();

        int index = (int)channel;
        values[index] = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(Keys[index], values[index]);

        ApplyMaster();
        Changed?.Invoke();
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    private static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;

        for (int i = 0; i < Keys.Length; i++)
        {
            values[i] = PlayerPrefs.GetFloat(Keys[i], 1f);
        }

        ApplyMaster();
    }

    private static void ApplyMaster()
    {
        AudioListener.volume = values[(int)VolumeChannel.Master];
    }
}