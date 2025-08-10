using UnityEngine;

/// <summary>
/// Attach this to the big box trigger in the main room.
/// Place a small presence-collider (isTrigger) on the XR Origin / Camera and tag it with PlayerPresenceTag.
/// When the presence collider is inside this trigger, headsets can be removed; otherwise removal is prevented.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HeadsetOffTrigger : MonoBehaviour
{
    [Tooltip("Tag of the player presence object (a small collider on the XR Origin or camera).")]
    public string PlayerPresenceTag = "Player";

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(PlayerPresenceTag)) return;
        SetCanUnwearForAll(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(PlayerPresenceTag)) return;
        SetCanUnwearForAll(false);
    }

    void SetCanUnwearForAll(bool value)
    {
        var all = FindObjectsOfType<WearableHeadsetPersistent>();
        foreach (var w in all)
        {
            w.canUnwear = value;
        }
    }
}
