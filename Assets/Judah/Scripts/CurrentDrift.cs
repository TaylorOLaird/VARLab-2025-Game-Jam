using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CurrentDrift : MonoBehaviour
{

    [SerializeField] WaterManager waterManager;
    [SerializeField] float driftSpeed = 1.5f;
    [SerializeField] float directionSmoothing = 12f;
    [SerializeField] float skin = 0.01f;
    [SerializeField] float maxStep = 1f;
    [SerializeField] float oppositeSnapDot = -0.9995f;
    public static int goalsReached = 0;

    BoxCollider box;
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

        // sum trigger up vectors
        Vector3 summed = Vector3.zero;
        foreach (var c in overlaps)
        {
            if (c && c.isTrigger)
            {
                // use rotation*Vector3.up for a stable world up from the trigger
                Vector3 up = (c.transform.rotation * Vector3.up).normalized;
                summed += up;
            }
        }

        Vector3 targetDir = summed.sqrMagnitude > 0f ? summed.normalized : Vector3.zero;

        //  snap logic for 180° flips to eliminate sideways drift 
        if (currentDir != Vector3.zero && targetDir != Vector3.zero)
        {
            float dot = Vector3.Dot(currentDir.normalized, targetDir); // both safe
            if (dot <= oppositeSnapDot)
            {
                currentDir = targetDir; // instant realign on flip
            }
            else
            {
                currentDir = Vector3.Slerp(
                    currentDir, targetDir,
                    1f - Mathf.Exp(-directionSmoothing * Time.fixedDeltaTime));
            }
        }
        else
        {
            // entering/exiting flow: just adopt target quickly
            currentDir = Vector3.Slerp(
                currentDir, targetDir,
                1f - Mathf.Exp(-directionSmoothing * Time.fixedDeltaTime));
        }
        // -------------------------------------------------------------

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

            // Worldspace box info for this slice
            Vector3 centerWS = pos + transform.rotation * box.center;
            Vector3 halfWS = Vector3.Scale(box.size * 0.5f, transform.lossyScale);
            Quaternion rotWS = transform.rotation;

            // BoxCast against all colliders except triggers
            if (Physics.BoxCast(centerWS, halfWS, dir, out RaycastHit hit, rotWS,
                                step, ~0, QueryTriggerInteraction.Ignore))
            {
                float travel = Mathf.Max(hit.distance - skin, 0f);
                pos += dir * travel;
                transform.position = pos; // stop flush
                return;
            }
            else
            {
                pos += dir * step;
                remaining -= step;
            }
        }

        transform.position = pos;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Goal"))
        {
            ++goalsReached;
            if (goalsReached == waterManager.goals)
            {
                waterManager.EndPuzzle();
            }
            GetComponent<CurrentDrift>().enabled = false;         
        }
    }
}
