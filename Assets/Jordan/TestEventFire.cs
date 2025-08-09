using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestEventFire : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        EventManager.OnHeadsetDon += HandleHeadsetDon;
        EventManager.OnHeadsetDoff += HandleHeadsetDoff;
    }

    public void HandleHeadsetDon()
    {
        Debug.Log("Headset Don event received!");
    }

    public void HandleHeadsetDoff()
    {
        Debug.Log("Headset Doff event received!");
    }
}
