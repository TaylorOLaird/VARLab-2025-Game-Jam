using System;
using UnityEngine;

public static class EventManager
{
    public static event Action OnHeadsetDon;
    public static event Action OnHeadsetDoff;

    public static void HeadsetDon()
    {
        OnHeadsetDon?.Invoke();
    }
    public static void HeadsetDoff()
    {
        OnHeadsetDoff?.Invoke();
    }

}