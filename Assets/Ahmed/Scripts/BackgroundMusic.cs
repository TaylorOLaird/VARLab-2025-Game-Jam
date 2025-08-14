using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    public AudioClip musicClip;
    [Range(0f, 1f)] public float volume = 0.5f;

    static BackgroundMusic _instance;
    AudioSource _src;

    void Awake()
    {
        // Singleton: keep only one across scenes
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        _src = GetComponent<AudioSource>();
        _src.spatialBlend = 0f;       // 2D
        _src.loop = true;
        _src.playOnAwake = false;
        _src.volume = volume;

        if (musicClip) _src.clip = musicClip;
        if (_src.clip) _src.Play();
        else Debug.LogWarning("[BackgroundMusic] No musicClip assigned.");
    }
}
