using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HMDManager : MonoBehaviour
{
    public GameObject HMDhitbox;
    void Start()
    {
        // Find the main camera in the XR Origin rig
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            HMDhitbox.transform.parent = mainCamera.transform;
            HMDhitbox.transform.localPosition = Vector3.zero;
        }
        else
        {
            Debug.LogWarning("Main Camera not found in the scene.");
        }
    }



    void Update()
    {

    }
}
