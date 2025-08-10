using System.Collections;
using System.Collections.Generic;
using Meta.WitAi.TTS.Utilities;
using UnityEngine;

public class SpeakerManager : MonoBehaviour
{
    public TTSSpeaker speaker;
    // Start is called before the first frame update
    void Start()
    {
        speaker.transform.SetParent(Camera.main.transform);
        EventManager.OnSpeak += HandleSpeak;
    }

    void HandleSpeak(string text)
    {
        speaker.SpeakQueued(text);
    }
}
