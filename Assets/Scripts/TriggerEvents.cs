using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class TriggerEvents : MonoBehaviour
{
    [Tooltip("Events to call when player enters the trigger")]
    public UnityEvent onTriggerEnter;

    [Tooltip("Events to call when player exits the trigger")]
    public UnityEvent onTriggerExit;

    private void Reset()
    {
        // Ensure collider is set to trigger
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            onTriggerEnter?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            onTriggerExit?.Invoke();
    }
}
