// SinusoidalShieldBlock.cs
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class MovingBlock : MonoBehaviour
{
    public enum Axis { X, Y, Z }

    [Header("Motion")]
    public Axis moveAxis = Axis.X;
    [Tooltip("Use the block's local axes (true) or world axes (false).")]
    public bool useLocalAxis = true;
    [Tooltip("Peak distance from the center (meters). Path length is 2x this.")]
    public float amplitude = 3f;
    [Tooltip("Cycles per second.")]
    public float frequency = 0.25f;
    [Tooltip("Phase offset in degrees.")]
    public float phaseDegrees = 0f;

    [Header("Center")]
    [Tooltip("Captured on play. Use 'Recenter Path To Current' to set a new center.")]
    public Vector3 pathCenter;  // read-only at runtime

    Rigidbody _rb;
    double _t0;
    bool _initialized;

    void Reset()
    {
        var bc = GetComponent<BoxCollider>();
        bc.isTrigger = false;                 // must be solid to block lasers

        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.useGravity = false;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        pathCenter = transform.position;
        _t0 = Time.timeAsDouble;
        _initialized = true;
    }

    void OnValidate()
    {
        if (!_initialized) pathCenter = transform.position;
        amplitude = Mathf.Max(0f, amplitude);
        frequency = Mathf.Max(0f, frequency);
    }

    void FixedUpdate()
    {
        // Choose the axis direction
        Vector3 localAxis = moveAxis switch
        {
            Axis.X => Vector3.right,
            Axis.Y => Vector3.up,
            _      => Vector3.forward
        };
        Vector3 dir = useLocalAxis ? transform.TransformDirection(localAxis) : localAxis;
        dir = dir.normalized;

        double t = Time.timeAsDouble - _t0;
        float omega = Mathf.PI * 2f * Mathf.Max(0f, frequency);
        float phase = phaseDegrees * Mathf.Deg2Rad;

        Vector3 target = pathCenter + dir * (amplitude * Mathf.Sin(omega * (float)t + phase));
        _rb.MovePosition(target);
    }

    // Editor helper to set a new path center at current position
    [ContextMenu("Recenter Path To Current")]
    void Recenter()
    {
        pathCenter = transform.position;
        _t0 = Time.timeAsDouble;
    }

    void OnDrawGizmosSelected()
    {
        // Draw the travel line
        Vector3 localAxis = moveAxis switch
        {
            Axis.X => Vector3.right,
            Axis.Y => Vector3.up,
            _      => Vector3.forward
        };
        Vector3 dir = useLocalAxis ? transform.TransformDirection(localAxis) : localAxis;
        dir = dir.normalized;

        Vector3 center = Application.isPlaying ? pathCenter : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(center - dir * amplitude, center + dir * amplitude);
        Gizmos.DrawWireSphere(center - dir * amplitude, 0.06f);
        Gizmos.DrawWireSphere(center + dir * amplitude, 0.06f);
    }
}
