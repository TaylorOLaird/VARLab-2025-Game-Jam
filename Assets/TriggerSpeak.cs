using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TriggerSpeak : MonoBehaviour
{
    public UnityEvent Speak;
    public void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            Speak.Invoke();
        }
    }
}
