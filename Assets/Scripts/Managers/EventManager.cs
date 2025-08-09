using System;
using UnityEngine;

public static class EventManager
{
    public static event Action<GameObject> OnHeadsetDon;
    public static event Action<GameObject> OnHeadsetDoff;

    public static void HeadsetDon(GameObject headset)
    {
        OnHeadsetDon?.Invoke(headset);
    }
    public static void HeadsetDoff(GameObject headset)
    {
        OnHeadsetDoff?.Invoke(headset);
    }

}