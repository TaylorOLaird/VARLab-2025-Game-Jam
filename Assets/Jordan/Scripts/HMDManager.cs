using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class HMDManager : MonoBehaviour
{
    public GameObject HMDDoffHitbox;
    public GameObject HMDDonHitbox;
    public XRSocketInteractor socketInteractor;
    public XRGrabInteractable grabInteractable;
    public Stack<GameObject> HMDStack = new Stack<GameObject>();

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
        yield return new WaitForSeconds(2f);
        ProcessHeadsetDon(args);
    }

    private void ProcessHeadsetDon(SelectEnterEventArgs args)
    {
        // Get the GameObject that was slotted
        GameObject slottedObject = args.interactableObject.transform.gameObject;

        // Fire the headset don event
        EventManager.HeadsetDon(slottedObject);

        slottedObject.SetActive(false);

        // Add the slotted object to the stack
        HMDStack.Push(slottedObject);
    }

    private void ProcessHeadsetDoff(SelectEnterEventArgs args)
    {
        // Pop the top object from the stack
        if (HMDStack.Count > 0)
        {
            GameObject headset = HMDStack.Pop();
            headset.SetActive(true);
            // Fire the headset doff event
            EventManager.HeadsetDoff(headset);
        }
        else
        {
            Debug.LogWarning("No headset available to doff.");
        }

    }
}
