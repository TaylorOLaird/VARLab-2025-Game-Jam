using System.Collections;
using System.Collections.Generic;
using Meta.WitAi.TTS.Utilities;
using UnityEngine;

public class SpeakingEvents : MonoBehaviour
{
    public TTSSpeaker speaker;

    public void Start()
    {
        speaker.SpeakQueued("Wakey wakey you sleep dope. We've got a long day of experimentation ahead of us!");
    }

    public void GoThroughDoor()
    {
        speaker.SpeakQueued("Since it is your birthday today, we've got some special experiments to run. Smiley face.");
    }

}
