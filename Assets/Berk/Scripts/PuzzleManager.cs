using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events;

public class PuzzleManager : MonoBehaviour
{
    [Header("Pedestals")]
    [Tooltip("If empty, will auto-find all CrystalPedestal in scene at Start.")]
    public List<CrystalPedestal> pedestals = new List<CrystalPedestal>();

    [Header("Finale")]
    [Tooltip("Animator to trigger when all crystals are placed.")]
    public Animator sceneAnimator;
    public string animatorTriggerName = "AllCrystalsPlaced";

    [Tooltip("Optional Timeline (PlayableDirector) to play on completion.")]
    public PlayableDirector director;

    [Tooltip("Optional prefab to spawn (final headset, etc).")]
    public GameObject finalSpawnPrefab;
    public Transform finalSpawnPoint;

    [Tooltip("Optional sound to play on completion.")]
    public AudioClip completionSfx;
    public AudioSource audioSource;

    [Header("Event")]
    public UnityEvent onAllPlaced;

    bool _completed = false;
    int _acceptedCount = 0;

    void Start()
    {
        if (pedestals == null || pedestals.Count == 0)
        {
            pedestals = FindObjectsOfType<CrystalPedestal>().ToList();
        }

        // subscribe to each pedestal's onAccepted
        foreach (var p in pedestals)
        {
            if (p == null) continue;
            p.onAccepted.AddListener(() => OnPedestalAccepted(p));
            // if you're using the Accepted property and pedestals were already accepted (rare), handle that:
#if UNITY_EDITOR
            // nothing
#endif
        }

        // optional: set accepted count to existing accepted pedestals
#if UNITY_EDITOR
        // Editor mode - do nothing automatic
#endif
    }

    void OnPedestalAccepted(CrystalPedestal pedestal)
    {
        if (_completed) return;

        _acceptedCount++;

        Debug.Log($"[PuzzleManager] Pedestal accepted: {_acceptedCount}/{pedestals.Count}");

        // if you're using the Accepted property and want to re-check current state, do that instead:
        // bool all = pedestals.All(x => x != null && x.Accepted);

        if (_acceptedCount >= pedestals.Count)
        {
            CompletePuzzle();
        }
    }

    void CompletePuzzle()
    {
        if (_completed) return;
        _completed = true;

        Debug.Log("[PuzzleManager] All crystals placed — triggering finale.");

        EventManager.RoomNumber = 7;

        EventManager.Speak($"You won.");

        // Animator trigger
        if (sceneAnimator != null && !string.IsNullOrEmpty(animatorTriggerName))
        {
            sceneAnimator.SetTrigger(animatorTriggerName);
        }

        // Play Timeline if provided
        if (director != null)
        {
            director.Play();
        }

        // Spawn final object (headset) if provided
        if (finalSpawnPrefab != null)
        {
            if (finalSpawnPoint != null)
                Instantiate(finalSpawnPrefab, finalSpawnPoint.position, finalSpawnPoint.rotation);
            else
                Instantiate(finalSpawnPrefab, Vector3.zero, Quaternion.identity);
        }

        // play sound
        if (completionSfx != null)
        {
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.PlayOneShot(completionSfx);
        }

        // custom events
        onAllPlaced?.Invoke();
    }

    // for debugging / tests
    [ContextMenu("Reset Puzzle State")]
    public void ResetState()
    {
        _completed = false;
        _acceptedCount = 0;
    }
}
