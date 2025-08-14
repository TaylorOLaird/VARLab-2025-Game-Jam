using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class HMDManagerLaser : MonoBehaviour
{
    public static HMDManagerLaser Instance { get; private set; }

    [Header("Face Hitboxes")]
    public GameObject HMDDoffHitbox;
    public GameObject HMDDonHitbox;

    [Header("XR")]
    public XRSocketInteractor socketInteractor;
    public XRGrabInteractable grabInteractable;

    [Header("State (debug)")]
    public Stack<HMD> HMDStack = new Stack<HMD>();   // top = currently worn
    public HMD currentlyWorn;                        // null if none

    public SceneSwitcher sceneSwitcher;
    readonly List<HMD> _allHMDs = new List<HMD>();

    Camera _cachedMainCam;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        if (socketInteractor) socketInteractor.selectEntered.AddListener(DonWaitAndProcess);
        if (grabInteractable) grabInteractable.selectEntered.AddListener(ProcessHeadsetDoff);
    }

    void OnDisable()
    {
        if (socketInteractor) socketInteractor.selectEntered.RemoveListener(DonWaitAndProcess);
        if (grabInteractable) grabInteractable.selectEntered.RemoveListener(ProcessHeadsetDoff);
    }

    void Start()
    {
        _allHMDs.AddRange(FindObjectsOfType<HMD>(true));

        // initial attach of face hitboxes
        AttachFaceHitboxesToCamera();
        NudgeFaceHitboxes();
    }

    void Update()
    {
        // Keep the face hitboxes glued to the current main camera and facing forward
        if (Camera.main != _cachedMainCam ||
            (HMDDoffHitbox && HMDDoffHitbox.transform.parent != Camera.main?.transform) ||
            (HMDDonHitbox && HMDDonHitbox.transform.parent != Camera.main?.transform))
        {
            AttachFaceHitboxesToCamera();
        }

        NudgeFaceHitboxes();
        SelfHealDoffHandle();
    }

    void DonWaitAndProcess(SelectEnterEventArgs args) => StartCoroutine(DelayedHeadsetDon(args));

    IEnumerator DelayedHeadsetDon(SelectEnterEventArgs args)
    {
        yield return new WaitForSeconds(0.1f);
        ProcessHeadsetDon(args);
    }

    void ProcessHeadsetDon(SelectEnterEventArgs args)
    {
        var go = args.interactableObject.transform.gameObject;
        var hmd = go.GetComponent<HMD>();
        if (!hmd) { Debug.LogWarning("Slotted object has no HMD component."); return; }

        // End headset?
        if (go.name.Equals("EndHeadset"))
        {
            EventManager.RoomNumber = 2;
            sceneSwitcher.SwitchScene("MainScene");
            return;
        }

        // Ignore if this is already the top
        if (currentlyWorn == hmd) return;

        // If this HMD exists lower in the stack, remove that older entry first
        if (HMDStack.Contains(hmd))
        {
            var tmp = new Stack<HMD>();
            while (HMDStack.Count > 0)
            {
                var t = HMDStack.Pop();
                if (t == hmd) break;
                tmp.Push(t);
            }
            while (tmp.Count > 0) HMDStack.Push(tmp.Pop());
        }

        // Hide the physical item while worn
        go.SetActive(false);

        // Push and mark current
        HMDStack.Push(hmd);
        currentlyWorn = hmd;

        EventManager.HeadsetDon(hmd);

        // Apply top-of-stack color suppression
        ApplyLaserSuppression(hmd.color);
    }

    void ProcessHeadsetDoff(SelectEnterEventArgs args)
    {
        if (HMDStack.Count <= 0)
        {
            Debug.LogWarning("No headset available to doff.");
            return;
        }

        // Pop the top
        var hmd = HMDStack.Pop();
        var headsetGO = hmd.gameObject;

        // Reactivate
        headsetGO.SetActive(true);

        // UI event for removing the icon
        EventManager.HeadsetDoff(hmd);

        var handBase = args.interactorObject as XRBaseInteractor;
        if (handBase != null)
        {
            var manager = handBase.interactionManager;
            var grabbable = headsetGO.GetComponent<XRGrabInteractable>();
            var selInteractor = args.interactorObject as IXRSelectInteractor
                                ?? handBase as IXRSelectInteractor
                                ?? handBase.GetComponent<IXRSelectInteractor>();

            if (manager != null && selInteractor != null && grabbable != null)
            {
                if (handBase.hasSelection)
                    manager.SelectExit(handBase, handBase.firstInteractableSelected);

                manager.SelectEnter(selInteractor, (IXRSelectInteractable)grabbable);
            }
            else
            {
                if (manager == null) Debug.LogWarning("Interaction Manager missing.");
                if (grabbable == null) Debug.LogWarning("XRGrabInteractable missing on headset.");
                if (selInteractor == null) Debug.LogWarning("Interactor is not an IXRSelectInteractor.");
            }
        }
        else
        {
            Debug.LogWarning("Interactor null during doff.");
        }

        // Who’s now on top?
        if (HMDStack.Count > 0)
        {
            currentlyWorn = HMDStack.Peek();
            ApplyLaserSuppression(currentlyWorn.color);   // re-apply previous headset’s effect
        }
        else
        {
            currentlyWorn = null;
            ApplyLaserSuppression(null);
        }
    }

    void AttachFaceHitboxesToCamera()
    {
        _cachedMainCam = Camera.main;
        var cam = _cachedMainCam;
        if (!cam) return;

        if (HMDDoffHitbox) { HMDDoffHitbox.transform.SetParent(cam.transform, false); HMDDoffHitbox.transform.localPosition = Vector3.zero; }
        if (HMDDonHitbox) { HMDDonHitbox.transform.SetParent(cam.transform, false); HMDDonHitbox.transform.localPosition = Vector3.zero; }
    }

    void NudgeFaceHitboxes()
    {
        var cam = Camera.main;
        if (!cam) return;

        if (HMDDoffHitbox)
        {
            HMDDoffHitbox.transform.forward = cam.transform.forward;
            if (HMDDoffHitbox.transform.localPosition != Vector3.zero)
                HMDDoffHitbox.transform.localPosition = Vector3.zero;
        }
        if (HMDDonHitbox)
        {
            HMDDonHitbox.transform.forward = cam.transform.forward;
            if (HMDDonHitbox.transform.localPosition != Vector3.zero)
                HMDDonHitbox.transform.localPosition = Vector3.zero;
        }
    }

    void SelfHealDoffHandle()
    {
        // If something disabled the doff handle’s collider or interactable, turn them back on
        if (!grabInteractable) return;

        if (!grabInteractable.enabled) grabInteractable.enabled = true;

        // Make sure at least one collider is enabled
        bool anyCol = false;
        foreach (var col in grabInteractable.GetComponentsInChildren<Collider>(true))
        {
            if (col.enabled) { anyCol = true; break; }
        }
        if (!anyCol)
        {
            foreach (var col in grabInteractable.GetComponentsInChildren<Collider>(true))
                col.enabled = true;
        }
    }

    // Suppress all lasers of a color; null = show everything
    public static void ApplyLaserSuppression(LaserEmitter.LaserType? colorOrNull)
    {
        foreach (var e in LaserEmitter.All)
            e.SetSuppressed(colorOrNull.HasValue && e.ColorType == colorOrNull.Value);
    }

    // Called by LevelManager on death
    public void ResetHeadsetsToSpawn()
    {
        // Clear face socket
        if (socketInteractor && socketInteractor.hasSelection)
        {
            var sel = socketInteractor.firstInteractableSelected;
            if (sel != null && socketInteractor.interactionManager != null)
                socketInteractor.interactionManager.SelectExit(socketInteractor, sel);
        }

        // Show all HMDs at their original locations
        foreach (var h in _allHMDs)
            if (h) h.ResetToSpawn();

        // Clear local state
        HMDStack.Clear();
        currentlyWorn = null;

        // Show all lasers again
        ApplyLaserSuppression(null);

        // Reattach face hitboxes
        AttachFaceHitboxesToCamera();
        NudgeFaceHitboxes();
    }
    
    public void RespawnHMD(HMD hmd)
    {
        if (!hmd) return;

        // If this HMD is currently worn/inactive, it's not the one that fell—ignore.
        if (currentlyWorn == hmd || !hmd.gameObject.activeInHierarchy)
            return;

        // If some interactor is still selecting it, force release (ResetToSpawn also does this,
        // but calling here first avoids transient states).
        var grab = hmd.GetComponent<XRGrabInteractable>();
        if (grab && grab.isSelected && grab.interactionManager != null)
        {
            var mgr = grab.interactionManager;
            var selecting = new List<IXRSelectInteractor>(grab.interactorsSelecting);
            foreach (var interactor in selecting)
                mgr.SelectExit(interactor, grab);
        }

        // Reset transform/velocities/parent and reactivate
        hmd.ResetToSpawn();
    }
}
