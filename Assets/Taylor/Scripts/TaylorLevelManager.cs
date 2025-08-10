using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class TaylorLevelManager : MonoBehaviour
{
    [SerializeField] private Volume volume;

    void Start()
    {
        EventManager.OnHeadsetDon += addHeadset;
        EventManager.OnHeadsetDoff += removeHeadset;

        SceneManager.LoadSceneAsync("Assets/Taylor/Scenes/Lab.unity", LoadSceneMode.Additive);
    }

    void Update()
    {
        
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
}
