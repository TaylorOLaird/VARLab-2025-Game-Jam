using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class HMDManager : MonoBehaviour
{
    public static HMDManager Instance { get; private set; }

    [Header("Face Hitboxes")]
    public GameObject HMDDoffHitbox;
    public GameObject HMDDonHitbox;

    [Header("XR")]
    public XRSocketInteractor socketInteractor;      // face socket
    public XRGrabInteractable grabInteractable;      // doff handle/button on face

    [Header("State (debug)")]
    public Stack<HMD> HMDStack = new Stack<HMD>();   // last donned on top
    public HMD currentlyWorn;                        // null if none

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

        // cache every HMD in the scene
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
        var go = args.interactableObject.transform.gameObject;
        var hmd = go.GetComponent<HMD>();
        if (!hmd) { Debug.LogWarning("Slotted object has no HMD component."); return; }

        // Unequip previous (if any)
        if (currentlyWorn != null && currentlyWorn != hmd)
        {
            // push previous back on stack so doff grabs the last one
            HMDStack.Push(currentlyWorn);
        }

        // Equip this one
        currentlyWorn = hmd;
        EventManager.HeadsetDon(hmd);

        // Hide the physical item while worn
        go.SetActive(false);

        // Track stack (top = currently worn)
        HMDStack.Push(hmd);

        // Global suppression by color
        ApplyLaserSuppression(hmd.color);
    }

    void ProcessHeadsetDoff(SelectEnterEventArgs args)
{
    if (HMDStack.Count <= 0)
    {
        Debug.LogWarning("No headset available to doff.");
        return;
    }

    // Pop the worn HMD
    var hmd = HMDStack.Pop();
    var headsetGO = hmd.gameObject;
    headsetGO.SetActive(true);
    EventManager.HeadsetDoff(hmd);

    currentlyWorn = null;
    HMDStack.Clear(); // one-at-a-time

    // Hand it to the grabbing hand
    var handBase = args.interactorObject as XRBaseInteractor;
    if (handBase == null)
    {
        Debug.LogWarning("Interactor null during doff.");
        return;
    }

    var manager   = handBase.interactionManager;
    var grabbable = headsetGO.GetComponent<XRGrabInteractable>();
    var selInteractor = args.interactorObject as IXRSelectInteractor
                        ?? handBase as IXRSelectInteractor
                        ?? handBase.GetComponent<IXRSelectInteractor>();

    if (manager != null && grabbable != null && selInteractor != null)
    {
        manager.SelectEnter(selInteractor, (IXRSelectInteractable)grabbable);
    }
    else
    {
        if (manager == null) Debug.LogWarning("Interaction Manager missing.");
        if (grabbable == null) Debug.LogWarning("XRGrabInteractable missing on headset.");
        if (selInteractor == null) Debug.LogWarning("Interactor is not an IXRSelectInteractor.");
    }

    // Clear suppression (no headset worn)
    HMDManager.ApplyLaserSuppression(null);
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
