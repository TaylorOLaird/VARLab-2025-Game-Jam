using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class HMDManager : MonoBehaviour
{
    public GameObject HMDDoffHitbox;
    public GameObject HMDDonHitbox;
    public XRSocketInteractor socketInteractor;
    public XRGrabInteractable grabInteractable;
    public Stack<HMD> HMDStack = new Stack<HMD>();

    void Start()
    {
        // Find the main camera in the XR Origin rig
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            HMDDoffHitbox.transform.parent = mainCamera.transform;
            HMDDoffHitbox.transform.localPosition = Vector3.zero;
            HMDDonHitbox.transform.parent = mainCamera.transform;
            HMDDonHitbox.transform.localPosition = Vector3.zero;
        }
        else
        {
            Debug.LogWarning("Main Camera not found in the scene.");
        }

        if (socketInteractor != null)
        {
            socketInteractor.selectEntered.AddListener(DonWaitAndProcess);
        }

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(ProcessHeadsetDoff);
        }
    }
    private void DonWaitAndProcess(SelectEnterEventArgs args)
    {
        StartCoroutine(DelayedHeadsetDon(args));
    }

    private IEnumerator DelayedHeadsetDon(SelectEnterEventArgs args)
    {
        yield return new WaitForSeconds(0.1f);
        ProcessHeadsetDon(args);
    }

    private void ProcessHeadsetDon(SelectEnterEventArgs args)
    {
        // Get the GameObject that was slotted
        GameObject slottedObject = args.interactableObject.transform.gameObject;

        if(slottedObject.GetComponent<HMD>() == null)
        {
            Debug.LogWarning("The slotted object does not have an HMD component.");
            return;
        }

        HMD hmd = slottedObject.GetComponent<HMD>();

        // Fire the headset don event
        EventManager.HeadsetDon(hmd);

        slottedObject.SetActive(false);

        // Add the slotted object to the stack
        HMDStack.Push(hmd);
    }

    private void ProcessHeadsetDoff(SelectEnterEventArgs args)
    {
        if (HMDStack.Count <= 0)
        {
            Debug.LogWarning("No headset available to doff.");
            return;
        }
        HMD hmd = HMDStack.Pop();
        GameObject headset = hmd.gameObject;
        headset.SetActive(true);
        // Fire the headset doff event
        EventManager.HeadsetDoff(hmd);

        // Get the interactor that grabbed THIS object
        var handInteractor = args.interactorObject as XRBaseInteractor;
        if (handInteractor == null || headset == null)
        {
            Debug.LogWarning("Interactor or headset is null during doff process.");
            return;
        }
            
        var manager = handInteractor.interactionManager;
        if (manager == null)
        {
            Debug.LogWarning("Interaction manager is null during doff process.");
            return;
        }

        // If the hand is holding something else (besides this), drop it
        //if (handInteractor.hasSelection && handInteractor.firstInteractableSelected != this)
        //{
        //    manager.SelectExit(handInteractor, handInteractor.firstInteractableSelected);
        //}

        // Force grab the other object
        var grabInteractable = headset.GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            Debug.LogWarning("The headset does not have an XRGrabInteractable component.");
            return;
        }

        manager.SelectEnter(handInteractor, (IXRSelectInteractable)grabInteractable);
    }
}
