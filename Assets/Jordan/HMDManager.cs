using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class HMDManager : MonoBehaviour
{
    public GameObject HMDHitbox;
    public XRSocketInteractor socketInteractor;
    public Stack<GameObject> HMDStack = new Stack<GameObject>();

    void Start()
    {
        // Find the main camera in the XR Origin rig
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            HMDHitbox.transform.parent = mainCamera.transform;
            HMDHitbox.transform.localPosition = Vector3.zero;
        }
        else
        {
            Debug.LogWarning("Main Camera not found in the scene.");
        }

        if (socketInteractor != null)
        {
            socketInteractor.selectEntered.AddListener(DonWaitAndProcess);
            socketInteractor.selectExited.AddListener(ProcessHeadsetDoff);
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
        // Fire the headset don event
        EventManager.HeadsetDon();

        // Get the GameObject that was slotted
        GameObject slottedObject = args.interactableObject.transform.gameObject;
    }

    private void ProcessHeadsetDoff(SelectExitEventArgs args)
    {
        // Fire the headset doff event
        EventManager.HeadsetDoff();

        // Get the GameObject that was removed
        GameObject removedObject = args.interactableObject.transform.gameObject;

        Debug.Log($"Object removed: {removedObject.name}");

    }
}
