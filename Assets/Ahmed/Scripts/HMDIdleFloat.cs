using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DisallowMultipleComponent]
[RequireComponent(typeof(XRGrabInteractable))]
public class HMDIdleFloat : MonoBehaviour
{
    [Header("Idle Motion")]
    [Tooltip("Degrees per second around world up when idle.")]
    public float spinSpeed = 25f;
    [Tooltip("Vertical bob amplitude in meters.")]
    public float bobAmplitude = 0.05f;
    [Tooltip("Bob cycles per second.")]
    public float bobFrequency = 0.8f;
    [Tooltip("How quickly we follow the bob target height.")]
    public float bobLerpSpeed = 4f;

    [Header("Return After Drop")]
    [Tooltip("Seconds to glide back to the base height after being dropped.")]
    public float returnDuration = 0.6f;
    [Tooltip("Ease curve for the return (0..1 time -> 0..1). If null, SmoothStep is used.")]
    public AnimationCurve returnEase;

    [Header("Options")]
    [Tooltip("Re-capture the base height when this object is enabled (good if you re-spawn it).")]
    public bool rebaseOnEnable = false;

    // cached
    XRGrabInteractable _grab;
    Rigidbody _rb;

    // state
    bool _idle = true;
    float _baseY;
    float _bobPhase;
    Coroutine _returnRoutine;

    bool _pendingReturn;

    void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
        _rb   = GetComponent<Rigidbody>();

        // XR usually uses kinematic bodies for grabbed objects; keep kinematic to avoid gravity drift.
        if (_rb) _rb.isKinematic = true;

        CaptureBaseHeight(); // initial base height
    }

    void OnEnable()
    {
        if (_grab != null)
        {
            _grab.selectEntered.AddListener(OnSelectEntered);
            _grab.selectExited.AddListener(OnSelectExited);
        }

        // If we needed to return but couldn't (because we were inactive),
        // kick it off now — but only if no one currently holds/sockets it.
        if (_pendingReturn && _grab != null && !_grab.isSelected)
        {
            _pendingReturn = false;
            StartReturn();
        }

        _idle = _grab == null || !_grab.isSelected;
        if (_idle) _returnRoutine = null; // just in case
    }

    void OnDisable()
    {
        // Unsubscribe
        _grab.selectEntered.RemoveListener(OnSelectEntered);
        _grab.selectExited.RemoveListener(OnSelectExited);

        if (_returnRoutine != null) { StopCoroutine(_returnRoutine); _returnRoutine = null; }
    }

    void Update()
    {
        if (!_idle) return;

        // Stop any rigidbody motion while idle so it stays put
        if (_rb)
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        // Spin
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

        // Bob
        _bobPhase += bobFrequency * Mathf.PI * 2f * Time.deltaTime;
        float targetY = _baseY + Mathf.Sin(_bobPhase) * bobAmplitude;

        var p = transform.position;
        p.y = Mathf.Lerp(p.y, targetY, 1f - Mathf.Exp(-bobLerpSpeed * Time.deltaTime));
        transform.position = p;
    }

    void CaptureBaseHeight()
    {
        _baseY = transform.position.y;
        // Make bob loop seamlessly at start height
        _bobPhase = 0f;
    }

    // XR events
    void OnSelectEntered(SelectEnterEventArgs _)
    {
        // Stop idle motion immediately
        _idle = false;
        if (_returnRoutine != null) { StopCoroutine(_returnRoutine); _returnRoutine = null; }
    }

    void OnSelectExited(SelectExitEventArgs _)
    {
        // Glide back to base height, then resume idle
        if (_returnRoutine != null) StopCoroutine(_returnRoutine);
        _returnRoutine = StartCoroutine(ReturnToBaseHeight());
    }

    void StartReturn()
    {
        if (_returnRoutine != null) StopCoroutine(_returnRoutine);
        _returnRoutine = StartCoroutine(ReturnToBaseHeight());
    }

    IEnumerator ReturnToBaseHeight()
    {
        _idle = false;

        Vector3 start = transform.position;
        Vector3 end   = new Vector3(start.x, _baseY, start.z);
        float t = 0f;
        float dur = Mathf.Max(0.01f, returnDuration);

        // Ease helper
        float Ease(float x)
        {
            if (returnEase != null && returnEase.keys != null && returnEase.keys.Length > 0)
                return Mathf.Clamp01(returnEase.Evaluate(Mathf.Clamp01(x)));
            // default smoothstep
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(x));
        }

        while (t < dur)
        {
            t += Time.deltaTime;
            float u = Ease(t / dur);
            transform.position = Vector3.LerpUnclamped(start, end, u);
            yield return null;
        }

        // Snap final Y and resume idle
        var p = transform.position;
        p.y = _baseY;
        transform.position = p;

        // Restart bob at a phase that matches current offset (seamless)
        float offset = transform.position.y - _baseY;
        _bobPhase = (bobAmplitude > 0.0001f) ? Mathf.Asin(Mathf.Clamp(offset / bobAmplitude, -1f, 1f)) : 0f;

        _idle = true;
        _returnRoutine = null;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Visualize base height band in editor
        var p = transform.position;
        var col = new Color(0.9f, 0.9f, 1f, 0.6f);
        UnityEditor.Handles.color = col;
        UnityEditor.Handles.DrawWireDisc(new Vector3(p.x, Application.isPlaying ? _baseY : p.y, p.z), Vector3.up, 0.25f);
        if (bobAmplitude > 0f)
        {
            UnityEditor.Handles.color = new Color(0.6f, 0.8f, 1f, 0.4f);
            UnityEditor.Handles.DrawWireDisc(new Vector3(p.x, (Application.isPlaying ? _baseY : p.y) + bobAmplitude, p.z), Vector3.up, 0.25f);
            UnityEditor.Handles.DrawWireDisc(new Vector3(p.x, (Application.isPlaying ? _baseY : p.y) - bobAmplitude, p.z), Vector3.up, 0.25f);
        }
    }
#endif
}
