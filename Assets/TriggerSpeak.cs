using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class TriggerSpeak : MonoBehaviour
{
    public UnityEvent<string> Speak;

    [TextArea]
    public string TextToSpeak;
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("[Trigger] Entered: " + other.name, other);
        if (other.CompareTag("Player"))
        {
            Speak.Invoke(TextToSpeak);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("[Trigger] Collision: " + collision.collider.name, collision.collider);
        if (collision.collider.CompareTag("Player"))
        {
            Speak.Invoke(TextToSpeak);
        }
    }
}

