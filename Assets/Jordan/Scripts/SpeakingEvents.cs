using System.Collections;
using System.Collections.Generic;
using Meta.WitAi.TTS.Utilities;
using UnityEngine;

public class SpeakingEvents : MonoBehaviour
{
    public TTSSpeaker speaker;

    public void Start()
    {
        speaker.SpeakQueued("Wakey wakey you sleepy dope. We've got a long day of experimentation ahead of us!");
    }

    public void GoThroughDoor(string textToSpeak)
    {
        speaker.SpeakQueued(textToSpeak);
    }

}
