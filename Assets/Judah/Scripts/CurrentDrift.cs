using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CurrentDrift : MonoBehaviour
{
    [SerializeField] float driftSpeed = 0.5f;
    [SerializeField] float directionSmoothing = 12f;

    Rigidbody rb;
    readonly HashSet<Collider> activeTriggers = new HashSet<Collider>();
    Vector3 currentDir = Vector3.zero; 

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void FixedUpdate()
    {
        Vector3 summed = Vector3.zero;
        foreach (var col in activeTriggers)
        {
            if (!col) continue;

            Vector3 dir = col.transform.up;
            summed += dir.normalized;
        }

        Vector3 targetDir = summed.sqrMagnitude > 0f ? summed.normalized : Vector3.zero;
        currentDir = Vector3.Slerp(currentDir, targetDir, 1f - Mathf.Exp(-directionSmoothing * Time.fixedDeltaTime));

        if (currentDir.sqrMagnitude > 0f)
        {
            rb.MovePosition(rb.position + currentDir * driftSpeed * Time.fixedDeltaTime);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) activeTriggers.Add(other);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.isTrigger) activeTriggers.Remove(other);
    }
}
