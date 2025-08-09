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

    public void HandleHeadsetDon(GameObject headset)
    {
        Debug.Log("Headset Don event received!");
        Debug.Log("Headset GameObject: " + headset.name);
    }

    public void HandleHeadsetDoff(GameObject headset)
    {
        Debug.Log("Headset Doff event received!");
        Debug.Log("Headset GameObject: " + headset.name);
    }
}
