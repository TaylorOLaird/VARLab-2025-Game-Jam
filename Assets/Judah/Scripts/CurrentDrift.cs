using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CurrentDrift : MonoBehaviour
{
    [SerializeField] float driftSpeed = 1.5f;
    [SerializeField] float directionSmoothing = 12f;
    [SerializeField] float skin = 0.01f;
    [SerializeField] float maxStep = 1f;

    BoxCollider box;
    readonly HashSet<Collider> activeTriggers = new HashSet<Collider>();
    Vector3 currentDir = Vector3.zero;

    void Awake()
    {
        box = GetComponent<BoxCollider>();
    }

    void FixedUpdate()
    {
        // world box
        Vector3 centerWS = transform.TransformPoint(box.center);
        Vector3 halfWS = Vector3.Scale(box.size * 0.5f, transform.lossyScale);
        Quaternion rotWS = transform.rotation;

        // find overlapping triggers (no RB needed)
        var overlaps = Physics.OverlapBox(centerWS, halfWS, rotWS, ~0, QueryTriggerInteraction.Collide);

        Vector3 summed = Vector3.zero;
        foreach (var c in overlaps)
            if (c && c.isTrigger) summed += c.transform.up.normalized;

        Vector3 targetDir = summed.sqrMagnitude > 0f ? summed.normalized : Vector3.zero;
        currentDir = Vector3.Slerp(currentDir, targetDir, 1f - Mathf.Exp(-directionSmoothing * Time.fixedDeltaTime));

        if (currentDir.sqrMagnitude == 0f) return;

        Vector3 desiredMove = currentDir * driftSpeed * Time.fixedDeltaTime;
        SweepAndMove(desiredMove); 
    }


    void SweepAndMove(Vector3 fullMove)
    {
        float remaining = fullMove.magnitude;
        if (remaining <= Mathf.Epsilon) return;

        Vector3 dir = fullMove.normalized;
        Vector3 pos = transform.position;

        while (remaining > 0f)
        {
            float step = Mathf.Min(remaining, maxStep);

            // World-space box info
            Vector3 centerWS = pos + transform.rotation * box.center;
            Vector3 halfWS = Vector3.Scale(box.size * 0.5f, transform.lossyScale);
            Quaternion rotWS = transform.rotation;

            // BoxCast against all colliders except triggers
            if (Physics.BoxCast(centerWS, halfWS, dir, out RaycastHit hit, rotWS,
                                step, ~0, QueryTriggerInteraction.Ignore))
            {
                float travel = Mathf.Max(hit.distance - skin, 0f);
                pos += dir * travel;
                transform.position = pos;
                return; // Stop on first solid hit
            }
            else
            {
                pos += dir * step;
                remaining -= step;
            }
        }

        transform.position = pos;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger)
            activeTriggers.Add(other);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.isTrigger)
            activeTriggers.Remove(other);
    }
}
