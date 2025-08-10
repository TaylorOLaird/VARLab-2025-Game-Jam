using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class TriggerSpeak : MonoBehaviour
{
    [Tooltip("Text to speak only once")]
    [TextArea]
    public string TextToSpeakOnce;
    bool hasSpoken = false;
    private void OnTriggerEnter(Collider other)
    {
        if (hasSpoken) return; 
        if (other.CompareTag("Player"))
        {
            EventManager.Speak(TextToSpeakOnce);
            hasSpoken = true;
        }
    }
}

