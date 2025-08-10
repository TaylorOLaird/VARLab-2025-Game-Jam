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
    private int FlashDuration = 0;

    void Start()
    {
        EventManager.OnHeadsetDon += addHeadset;
        EventManager.OnHeadsetDoff += removeHeadset;

        SceneManager.LoadSceneAsync("Assets/Taylor/Scenes/Lab.unity", LoadSceneMode.Additive);
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

    public void addHeadset(HMD headset)
    {
        Debug.Log("Headset added - B&W enabled");
        volume.weight = 1f;
        SceneManager.LoadSceneAsync("Assets/Taylor/Scenes/Old.unity", LoadSceneMode.Additive);
        SceneManager.UnloadSceneAsync("Assets/Taylor/Scenes/Lab.unity");
    }

    public void removeHeadset(HMD headset)
    {
        Debug.Log("Headset removed - B&W disabled");
        volume.weight = 0f;
        SceneManager.LoadSceneAsync("Assets/Taylor/Scenes/Lab.unity", LoadSceneMode.Additive);
        SceneManager.UnloadSceneAsync("Assets/Taylor/Scenes/Old.unity");
    }

    public void CameraFlashEffect()
    {
        CameraFlash.weight = 1f;
        FlashDuration = 3;
        // play camera sound starting at 0.1 seconds in
        cameraSound.time = flashOffset;
        cameraSound.Play();
    }
}
