using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[DisallowMultipleComponent]
public class HMD : MonoBehaviour
{
    [Header("Visual")]
    public Sprite headsetSprite; // keep your existing sprite field

    [Header("Puzzle Color")]
    public LaserEmitter.LaserType color = LaserEmitter.LaserType.Red;

    // --- Spawn snapshot (captured at Awake) ---
    Vector3 _spawnPos;
    Quaternion _spawnRot;
    Transform _spawnParent;
    bool _spawnCaptured;

    // --- Cached components ---
    XRGrabInteractable _grab;
    Rigidbody _rb;

    void Awake()
    {
        // Cache starting transform so we can reset on death
        CaptureSpawnIfNeeded();

        _grab = GetComponent<XRGrabInteractable>();
        _rb   = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// If you move the HMD in the editor and want to update its spawn,
    /// call this at runtime or via context menu.
    /// </summary>
    [ContextMenu("Capture Spawn Now")]
    public void CaptureSpawnNow()
    {
        _spawnParent = transform.parent;
        _spawnPos    = transform.position;
        _spawnRot    = transform.rotation;
        _spawnCaptured = true;
    }

    void CaptureSpawnIfNeeded()
    {
        if (_spawnCaptured) return;
        _spawnParent = transform.parent;
        _spawnPos    = transform.position;
        _spawnRot    = transform.rotation;
        _spawnCaptured = true;
    }

    /// <summary>
    /// Puts the HMD back where it started, visible, not held or socketed.
    /// Called by HMDManager on player death.
    /// </summary>
    public void ResetToSpawn()
{
    CaptureSpawnIfNeeded();

    // If currently selected by any interactor (hand or socket), force a release
    if (_grab && _grab.isSelected && _grab.interactionManager != null)
    {
        var mgr = _grab.interactionManager;

        // Make a copy because SelectExit mutates the collection
        var selecting = new List<IXRSelectInteractor>(_grab.interactorsSelecting);
        foreach (IXRSelectInteractor interactor in selecting)
        {
            mgr.SelectExit(interactor, _grab); // _grab implements IXRSelectInteractable
        }
    }

    // Restore transform & parent
    transform.SetParent(_spawnParent);
    transform.SetPositionAndRotation(_spawnPos, _spawnRot);

    // Reactivate and zero motion
    if (!gameObject.activeSelf) gameObject.SetActive(true);
    if (_rb)
    {
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }
}


#if UNITY_EDITOR
    // Little gizmo to see spawn in the editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = color switch
        {
            LaserEmitter.LaserType.Red   => new Color(1f, 0.2f, 0.2f, 0.9f),
            LaserEmitter.LaserType.Green => new Color(0.2f, 1f, 0.3f, 0.9f),
            LaserEmitter.LaserType.Blue  => new Color(0.3f, 0.6f, 1f, 0.9f),
            _ => Color.white
        };
        Gizmos.DrawWireSphere(Application.isPlaying ? _spawnPos : transform.position, 0.08f);
    }
#endif
}
