using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SoundEntry
{
    public string id;
    public AudioClip clip;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Tooltip("Named sounds")]
    public List<SoundEntry> sounds = new List<SoundEntry>();

    [Header("Sources (optional - will be auto-created if null)")]
    public AudioSource ambientSource; // looping ambient / background
    public AudioSource sfxSource;     // one-shot sfx

    Dictionary<string, AudioClip> _lookup;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // create sources if missing
        if (ambientSource == null) ambientSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        // ambient defaults
        ambientSource.loop = true;
        ambientSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        // build lookup
        _lookup = new Dictionary<string, AudioClip>();
        foreach (var s in sounds)
        {
            if (s == null || string.IsNullOrEmpty(s.id) || s.clip == null) continue;
            if (!_lookup.ContainsKey(s.id)) _lookup.Add(s.id, s.clip);
        }
    }

    AudioClip GetClip(string id)
    {
        if (string.IsNullOrEmpty(id) || _lookup == null) return null;
        _lookup.TryGetValue(id, out var c);
        return c;
    }

    // Play an ambient (looping) sound. Replaces any currently-playing ambient.
    public void PlayAmbient(string id)
    {
        var clip = GetClip(id);
        if (clip == null)
        {
            Debug.LogWarning($"[AudioManager] PlayAmbient: clip '{id}' not found.");
            return;
        }

        if (ambientSource.clip == clip && ambientSource.isPlaying) return;

        ambientSource.clip = clip;
        ambientSource.loop = true;
        ambientSource.Play();
    }

    // Stops ambient only if it matches the id (safe to call)
    public void StopAmbient(string id)
    {
        var clip = GetClip(id);
        if (clip == null)
        {
            // fallback: stop the ambient source completely
            ambientSource.Stop();
            ambientSource.clip = null;
            return;
        }

        if (ambientSource.clip == clip)
        {
            ambientSource.Stop();
            ambientSource.clip = null;
        }
    }

    // Play one-shot by id
    public void PlayOneShot(string id, float volumeScale = 1f)
    {
        var clip = GetClip(id);
        if (clip == null)
        {
            Debug.LogWarning($"[AudioManager] PlayOneShot: clip '{id}' not found.");
            return;
        }
        sfxSource.PlayOneShot(clip, volumeScale);
    }

    // Play one-shot by AudioClip reference (useful if you already have a clip variable)
    public void PlayOneShotClip(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, volumeScale);
    }
}
