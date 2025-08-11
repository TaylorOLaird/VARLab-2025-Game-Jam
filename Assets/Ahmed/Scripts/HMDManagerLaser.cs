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
    public XRSocketInteractor socketInteractor;      // face socket
    public XRGrabInteractable grabInteractable;      // doff handle/button on face

    [Header("State (debug)")]
    public Stack<HMD> HMDStack = new Stack<HMD>();   // bottom..top (top = currently worn)
    public HMD currentlyWorn;                        // null if none

    public SceneSwitcher sceneSwitcher;
    List<HMD> _allHMDs = new List<HMD>();

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Parent face hitboxes to XR camera
        var cam = Camera.main;
        if (cam)
        {
            if (HMDDoffHitbox) { HMDDoffHitbox.transform.SetParent(cam.transform, false); HMDDoffHitbox.transform.localPosition = Vector3.zero; }
            if (HMDDonHitbox)  { HMDDonHitbox.transform.SetParent(cam.transform, false);  HMDDonHitbox.transform.localPosition = Vector3.zero; }
        }
        else Debug.LogWarning("Main Camera not found.");

        if (socketInteractor) socketInteractor.selectEntered.AddListener(DonWaitAndProcess);
        if (grabInteractable) grabInteractable.selectEntered.AddListener(ProcessHeadsetDoff);

        // cache every HMD in the scene (for reset)
        _allHMDs.AddRange(FindObjectsOfType<HMD>(true));
    }

    // --- Wear flow ---
    void DonWaitAndProcess(SelectEnterEventArgs args) => StartCoroutine(DelayedHeadsetDon(args));

    IEnumerator DelayedHeadsetDon(SelectEnterEventArgs args)
    {
        yield return new WaitForSeconds(0.1f);
        ProcessHeadsetDon(args);
    }

    void ProcessHeadsetDon(SelectEnterEventArgs args)
    {
        var go  = args.interactableObject.transform.gameObject;
        var hmd = go.GetComponent<HMD>();
        if (!hmd) { Debug.LogWarning("Slotted object has no HMD component."); return; }

        // Special “end” headset?
        if (go.name.Equals("EndHeadset"))
        {
            EventManager.RoomNumber = 2;
            sceneSwitcher.SwitchScene("MainScene");
            return;
        }

        // If this HMD is already the top, ignore
        if (currentlyWorn == hmd) return;

        // If this HMD is buried lower in the stack (edge case), remove that older entry first
        if (HMDStack.Contains(hmd))
        {
            var tmp = new Stack<HMD>();
            // Unwind until we find it
            while (HMDStack.Count > 0)
            {
                var t = HMDStack.Pop();
                if (t == hmd) break;
                tmp.Push(t);
            }
            // Re-stack the ones we popped off
            while (tmp.Count > 0) HMDStack.Push(tmp.Pop());
        }

        // Hide the physical item while worn
        go.SetActive(false);

        // Push onto the stack and mark as worn
        HMDStack.Push(hmd);
        currentlyWorn = hmd;

        // UI event
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

        // Pop the top (currently worn)
        var hmd = HMDStack.Pop();
        var headsetGO = hmd.gameObject;

        // Reactivate physical object
        headsetGO.SetActive(true);

        // UI event for removing the icon
        EventManager.HeadsetDoff(hmd);

        // Hand it to the grabbing hand
        var handBase = args.interactorObject as XRBaseInteractor;
        if (handBase == null)
        {
            Debug.LogWarning("Interactor null during doff.");
        }
        else
        {
            var manager      = handBase.interactionManager;
            var grabbable    = headsetGO.GetComponent<XRGrabInteractable>();
            var selInteractor = args.interactorObject as IXRSelectInteractor
                                ?? handBase as IXRSelectInteractor
                                ?? handBase.GetComponent<IXRSelectInteractor>();

            if (manager != null && grabbable != null && selInteractor != null)
                manager.SelectEnter(selInteractor, (IXRSelectInteractable)grabbable);
            else
            {
                if (manager == null) Debug.LogWarning("Interaction Manager missing.");
                if (grabbable == null) Debug.LogWarning("XRGrabInteractable missing on headset.");
                if (selInteractor == null) Debug.LogWarning("Interactor is not an IXRSelectInteractor.");
            }
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
            ApplyLaserSuppression(null);                  // no suppression
        }
    }

    // --- Global helpers ---

    // Suppress all lasers of a color; null = show everything
    public static void ApplyLaserSuppression(LaserEmitter.LaserType? colorOrNull)
    {
        foreach (var e in LaserEmitter.All)
            e.SetSuppressed(colorOrNull.HasValue && e.ColorType == colorOrNull.Value);
    }

    // Called by LevelManager on death
    public void ResetHeadsetsToSpawn()
    {
        // 1) Clear face socket (if anything is slotted)
        if (socketInteractor && socketInteractor.hasSelection)
        {
            var sel = socketInteractor.firstInteractableSelected;
            if (sel != null && socketInteractor.interactionManager != null)
                socketInteractor.interactionManager.SelectExit(socketInteractor, sel);
        }

        // 2) Show all HMDs at their original locations
        foreach (var h in _allHMDs)
            if (h) h.ResetToSpawn();

        // 3) Clear local state
        HMDStack.Clear();
        currentlyWorn = null;

        // 4) Show all lasers again
        ApplyLaserSuppression(null);
    }
}