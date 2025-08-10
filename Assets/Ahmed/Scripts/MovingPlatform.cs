using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))] // solid collider (NOT trigger)
public class MovingPlatform : MonoBehaviour
{
    [Header("Path")]
    [Tooltip("Ordered checkpoints AFTER the starting position (the platform’s initial position).")]
    public Transform[] checkpoints;
    public float speed = 2.5f;
    public float waitAtEndSeconds = 2f;
    public float waitAtStartSeconds = 0f;
    public float arriveTolerance = 0.03f;

    [Header("Rider Detection")]
    public string riderTag = "Player";
    public bool autoTopTrigger = true;
    public float topTriggerPadding = 0.05f;
    public float topTriggerHeight = 0.2f;

    [Header("Path Line (visual)")]
    public bool showPathLine = true;
    public Color pathLineColor = new Color(0.15f, 0.85f, 1f, 1f);
    public float pathLineWidth = 0.03f;
    public bool pathLineAlwaysUpdateInEditor = true;

    Rigidbody _rb;
    BoxCollider _solid;      // non-trigger collider on THIS GameObject
    BoxCollider _topTrigger; // thin trigger above the top face

    Vector3 _startPos;
    int _targetIndex;        // 0 = start, 1..N = checkpoints
    int _dir = 0;            // 0 idle, +1 forward, -1 backward
    float _waitTimer;
    bool _cycleActive;

    readonly List<Vector3> _path = new();
    Vector3 _lastPos;

    // Rider tracking (per XR rig root): contact counts to avoid “sticky” carry
    readonly HashSet<Transform> _riders = new();
    readonly Dictionary<Transform, int> _riderContacts = new();

    // Path line
    LineRenderer _pathLR;

    void Reset()
    {
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        if (!TryGetComponent(out BoxCollider bc))
            bc = gameObject.AddComponent<BoxCollider>();
        bc.isTrigger = false;
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.useGravity = false;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        ResolveSolidCollider();
        if (autoTopTrigger) EnsureTopTrigger();

        BuildPath();
        SetupPathLine();

        _lastPos = transform.position;
        _waitTimer = waitAtStartSeconds;
        _dir = 0;
        _cycleActive = false;
    }

    void OnValidate()
    {
        speed = Mathf.Max(0f, speed);
        arriveTolerance = Mathf.Max(0.001f, arriveTolerance);
        topTriggerHeight = Mathf.Max(0.01f, topTriggerHeight);
        pathLineWidth = Mathf.Max(0.001f, pathLineWidth);

        ResolveSolidCollider();
        if (!Application.isPlaying && autoTopTrigger) EnsureTopTrigger();
        if (!Application.isPlaying && pathLineAlwaysUpdateInEditor) { BuildPath(); SetupPathLine(); }
    }

    void ResolveSolidCollider()
    {
        _solid = null;
        var bcs = GetComponents<BoxCollider>();
        foreach (var bc in bcs) if (!bc.isTrigger) { _solid = bc; break; }
        if (_solid == null && bcs.Length > 0) { _solid = bcs[0]; _solid.isTrigger = false; }
    }

    void BuildPath()
    {
        _path.Clear();
        _startPos = transform.position;
        _path.Add(_startPos);
        if (checkpoints != null)
            foreach (var t in checkpoints) if (t) _path.Add(t.position);
        _targetIndex = 0;
    }

    void EnsureTopTrigger()
    {
        if (_solid == null) return;

        _topTrigger = null;
        foreach (var bc in GetComponents<BoxCollider>())
            if (bc != _solid && bc.isTrigger) { _topTrigger = bc; break; }

        if (_topTrigger == null)
        {
            _topTrigger = gameObject.AddComponent<BoxCollider>();
            _topTrigger.isTrigger = true;
        }

        var size = _solid.size;
        var center = _solid.center;
        float yHalf = size.y * 0.5f;

        _topTrigger.isTrigger = true;
        _topTrigger.size = new Vector3(
            size.x + topTriggerPadding * 2f,
            topTriggerHeight,
            size.z + topTriggerPadding * 2f
        );
        _topTrigger.center = new Vector3(
            center.x,
            center.y + yHalf + (_topTrigger.size.y * 0.5f) + 0.01f,
            center.z
        );
    }

    void SetupPathLine()
{
    if (!showPathLine)
    {
        if (_pathLR) _pathLR.enabled = false;
        return;
    }

    if (_pathLR == null)
    {
        var go = new GameObject("__PathLine");
        go.transform.SetParent(transform, worldPositionStays: false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        _pathLR = go.AddComponent<LineRenderer>();
        _pathLR.useWorldSpace = true;
        _pathLR.textureMode = LineTextureMode.Stretch;
        _pathLR.alignment = LineAlignment.View;
        _pathLR.numCapVertices = 4;
        _pathLR.numCornerVertices = 2;
        
        var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (!sh) sh = Shader.Find("Sprites/Default");

        // Assign via sharedMaterial to avoid instantiation/leaks in edit mode
        var mat = new Material(sh) { name = "__PathLine_Mat" };
        _pathLR.sharedMaterial = mat;
    }

    _pathLR.enabled = true;
    _pathLR.startWidth = _pathLR.endWidth = pathLineWidth;

    var grad = new Gradient();
    grad.SetKeys(
        new[] { new GradientColorKey(pathLineColor, 0f), new GradientColorKey(pathLineColor, 1f) },
        new[] { new GradientAlphaKey(pathLineColor.a, 0f), new GradientAlphaKey(pathLineColor.a, 1f) }
    );
    _pathLR.colorGradient = grad;

    // Set points: start + checkpoints
    _pathLR.positionCount = _path.Count;
    for (int i = 0; i < _path.Count; i++)
        _pathLR.SetPosition(i, _path[i]);
}


    void FixedUpdate()
    {
        // Periodically rebuild path line if checkpoints move in play mode
        if (showPathLine && (Time.frameCount % 5 == 0))
        {
            // Refresh dynamic checkpoint positions
            _path[0] = _startPos; // start stays captured
            for (int i = 1; i < _path.Count; i++)
                if (i - 1 < checkpoints.Length && checkpoints[i - 1])
                    _path[i] = checkpoints[i - 1].position;
            SetupPathLine();
        }

        // Endpoint waits
        if (_waitTimer > 0f)
        {
            _waitTimer -= Time.fixedDeltaTime;
            _lastPos = transform.position;
            PruneRidersIfNotOverlapping(); // extra safety during waits too
            return;
        }

        // Idle at start until ridden
        if (_dir == 0 && !_cycleActive)
        {
            _lastPos = transform.position;
            PruneRidersIfNotOverlapping();
            return;
        }

        // Move toward target
        Vector3 current = transform.position;
        Vector3 target = _path[Mathf.Clamp(_targetIndex, 0, _path.Count - 1)];
        Vector3 to = target - current;
        float dist = to.magnitude;

        if (dist > arriveTolerance)
        {
            float stepLen = speed * Time.fixedDeltaTime;
            Vector3 step = (dist <= stepLen) ? to : to.normalized * stepLen;
            _rb.MovePosition(current + step);
        }
        else
        {
            if (_dir > 0)
            {
                if (_targetIndex < _path.Count - 1) _targetIndex++;
                else { _waitTimer = waitAtEndSeconds; _dir = -1; _targetIndex = _path.Count - 2; }
            }
            else if (_dir < 0)
            {
                if (_targetIndex > 0) _targetIndex--;
                else { _waitTimer = waitAtStartSeconds; _dir = 0; _cycleActive = false; }
            }
        }

        // Carry current riders by platform delta
        Vector3 delta = transform.position - _lastPos;
        if (delta.sqrMagnitude > 0f && _riders.Count > 0)
        {
            foreach (var tr in _riders)
            {
                if (!tr) continue;
                if (tr.TryGetComponent<CharacterController>(out var cc) && cc.enabled) cc.Move(delta);
                else if (tr.TryGetComponent<Rigidbody>(out var prb) && !prb.isKinematic) prb.MovePosition(prb.position + delta);
                else tr.position += delta;
            }
        }

        _lastPos = transform.position;

        // Safety: if anything slipped out of the trigger without Exit, prune it
        if (Time.frameCount % 3 == 0) PruneRidersIfNotOverlapping();
    }

    // --- Trigger handling ---

    void OnTriggerEnter(Collider other)
    {
        if (_topTrigger == null || other == null) return;
        TryAddContact(other);
    }

    void OnTriggerStay(Collider other)
    {
        if (_topTrigger == null || other == null) return;

        // If something started inside the trigger (no Enter fired), ensure it’s tracked:
        var key = RiderKey(other.transform);
        if (key && IsAllowedTag(key) && !_riderContacts.ContainsKey(key))
            TryAddContact(other);
    }

    void OnTriggerExit(Collider other)
    {
        if (_topTrigger == null || other == null) return;
        TryRemoveContact(other);
    }

    // Count contacts per XR rig root
    void TryAddContact(Collider other)
    {
        var key = RiderKey(other.transform);
        if (!key || !IsAllowedTag(key)) return;

        if (_riderContacts.TryGetValue(key, out int c)) _riderContacts[key] = c + 1;
        else
        {
            _riderContacts[key] = 1;
            _riders.Add(key);

            // Kick off cycle if idle at start
            if (_dir == 0 && !_cycleActive)
            {
                _cycleActive = true;
                _dir = +1;
                _targetIndex = Mathf.Min(1, _path.Count - 1);
                _waitTimer = 0f;
            }
        }
    }

    void TryRemoveContact(Collider other)
    {
        var key = RiderKey(other.transform);
        if (!key) return;

        if (_riderContacts.TryGetValue(key, out int c))
        {
            c -= 1;
            if (c <= 0)
            {
                _riderContacts.Remove(key);
                _riders.Remove(key); // <<< fully detached
            }
            else _riderContacts[key] = c;
        }
        else
        {
            _riders.Remove(key); // fallback
        }
    }

    // If a rider somehow didn't send Exit (disable/teleport), prune by overlap test
    void PruneRidersIfNotOverlapping()
    {
        if (_riders.Count == 0 || _topTrigger == null) return;

        // Compute the world box of the trigger
        Vector3 worldCenter = transform.TransformPoint(_topTrigger.center);
        Vector3 half = Vector3.Scale(_topTrigger.size, transform.lossyScale) * 0.5f;
        Quaternion rot = transform.rotation;

        // Collect roots currently overlapping
        var hits = Physics.OverlapBox(worldCenter, half, rot, ~0, QueryTriggerInteraction.Collide);
        var present = new HashSet<Transform>();
        foreach (var h in hits) present.Add(h.transform.root);

        // Remove any rider whose root isn’t in the box
        var toRemove = new List<Transform>();
        foreach (var rider in _riders)
        {
            if (!rider) { toRemove.Add(rider); continue; }
            if (!present.Contains(rider.root))
            {
                toRemove.Add(rider);
                _riderContacts.Remove(rider);
            }
        }
        foreach (var r in toRemove) _riders.Remove(r);
    }

    Transform RiderKey(Transform any)
    {
        var root = any.root;
        // Prefer the transform that actually has the CharacterController
        for (var cur = root; cur != null; cur = cur.parent)
            if (cur.TryGetComponent<CharacterController>(out _)) return cur;
        return root;
    }

    bool IsAllowedTag(Transform key)
    {
        return string.IsNullOrEmpty(riderTag) || key.CompareTag(riderTag);
    }

    // --- Gizmos ---

    void OnDrawGizmosSelected()
    {
        // Draw path spheres/lines in editor for clarity
        var pts = new List<Vector3>();
        pts.Add(Application.isPlaying ? _startPos : transform.position);
        if (checkpoints != null) foreach (var t in checkpoints) if (t) pts.Add(t.position);

        Gizmos.color = new Color(0f, 0.8f, 1f, 0.9f);
        for (int i = 0; i < pts.Count; i++)
        {
            Gizmos.DrawWireSphere(pts[i], 0.08f);
            if (i > 0) Gizmos.DrawLine(pts[i - 1], pts[i]);
        }
    }
}
