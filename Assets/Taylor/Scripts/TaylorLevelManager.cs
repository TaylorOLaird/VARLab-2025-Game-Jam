using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class TaylorLevelManager : MonoBehaviour
{
    [SerializeField] private Volume volume;
    [SerializeField] private Volume CameraFlash;
    [SerializeField] private AudioSource cameraSound;
    [SerializeField] private float flashOffset = 0.0f;
    [SerializeField] private CameraColider cameraColliderScript;
    [SerializeField] private List<GameObject> objectsToSwitch;
    private string currentRelmName = "Lab";
    private int FlashDuration = 0;

    void Start()
    {
        EventManager.OnHeadsetDon += addHeadset;
        EventManager.OnHeadsetDoff += removeHeadset;

        SceneManager.LoadSceneAsync("Assets/Taylor/Scenes/Lab.unity", LoadSceneMode.Additive);
        displayObjectsInRelm();
    }

    void Update()
    {
        if (FlashDuration > 0)
        {
            FlashDuration --;
        }
        else
        {
            CameraFlash.weight = 0f;
        }
    }

    private void displayObjectsInRelm()
    {
        foreach (GameObject obj in objectsToSwitch)
        {
            // get the ObjectSwitching component
            ObjectSwitching objectSwitching = obj.GetComponent<ObjectSwitching>();
            if (objectSwitching != null && objectSwitching.relmName == currentRelmName)
            {
                // enable the object
                obj.SetActive(true);
            }
            else
            {
                // disable the object
                obj.SetActive(false);
            }
        }
    }

    public void addHeadset(HMD headset)
    {
        Debug.Log("Headset added - B&W enabled");
        volume.weight = 1f;
        SceneManager.LoadSceneAsync("Assets/Taylor/Scenes/Old.unity", LoadSceneMode.Additive);
        SceneManager.UnloadSceneAsync("Assets/Taylor/Scenes/Lab.unity");
        currentRelmName = "Old";
        displayObjectsInRelm();
    }

    public void removeHeadset(HMD headset)
    {
        Debug.Log("Headset removed - B&W disabled");
        volume.weight = 0f;
        SceneManager.LoadSceneAsync("Assets/Taylor/Scenes/Lab.unity", LoadSceneMode.Additive);
        SceneManager.UnloadSceneAsync("Assets/Taylor/Scenes/Old.unity");
        currentRelmName = "Lab";
        displayObjectsInRelm();
    }

    public void CameraFlashEffect()
    {
        CameraFlash.weight = 1f;
        FlashDuration = 3;
        // play camera sound starting at 0.1 seconds in
        cameraSound.time = flashOffset;
        cameraSound.Play();
        // check if the camera collider scripts currentColliderName is not empty
        if (!string.IsNullOrEmpty(cameraColliderScript.currentColliderName))
        {
            if (cameraColliderScript.currentColliderName == "TestTrigger")
            {
                // find game object named "TestTriggerBlock"
                GameObject testTriggerBlock = GameObject.Find("TestTriggerBlock");
                // find its ObjectSwitching component
                ObjectSwitching objectSwitching = testTriggerBlock.GetComponent<ObjectSwitching>();

                if (currentRelmName == "Lab")
                {
                    objectSwitching.relmName = "Old";
                }
                else if (currentRelmName == "Old")
                {
                    objectSwitching.relmName = "Lab";
                }
                displayObjectsInRelm();
            }
        }
    }
}
