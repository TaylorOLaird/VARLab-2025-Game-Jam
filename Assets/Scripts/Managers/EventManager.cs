using System;
using UnityEngine;

public static class EventManager
{
    public static event Action<HMD> OnHeadsetDon;
    public static event Action<HMD> OnHeadsetDoff;
    public static event Action<string> OnSpeak;

    public static void HeadsetDon(HMD headset)
    {
        OnHeadsetDon?.Invoke(headset);
    }
    public static void HeadsetDoff(HMD headset)
    {
        OnHeadsetDoff?.Invoke(headset);
    }

    public static void Speak(string text)
    {
        OnSpeak?.Invoke(text);
    }

}