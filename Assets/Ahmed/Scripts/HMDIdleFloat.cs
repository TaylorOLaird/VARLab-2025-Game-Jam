using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DisallowMultipleComponent]
[RequireComponent(typeof(XRGrabInteractable))]
public class HMDIdleFloat : MonoBehaviour
{
    [Header("Idle Motion")]
    public float spinSpeed = 25f;
    public float bobAmplitude = 0.05f;
    public float bobFrequency = 0.8f;
    public float bobLerpSpeed = 4f;

    [Header("Return After Drop")]
    public float returnDuration = 0.6f;
    public AnimationCurve returnEase;

    [Header("Options")]
    public bool rebaseOnEnable = false;

    XRGrabInteractable _grab;
    Rigidbody _rb;

    bool _idle = true;
    float _baseY;
    float _bobPhase;
    Coroutine _returnRoutine;

    // If we tried to start a return while inactive, run it on enable instead
    bool _pendingReturn;

    void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
        _rb   = GetComponent<Rigidbody>();

        if (_rb) _rb.isKinematic = true; // typical for XR grabbables
        CaptureBaseHeight();
    }

    void OnEnable()
    {
        if (rebaseOnEnable) CaptureBaseHeight();

        if (_grab != null)
        {
            _grab.selectEntered.AddListener(OnSelectEntered);
            _grab.selectExited.AddListener(OnSelectExited);
        }

        // If we queued a return while inactive, do it now (only if not held)
        if (_pendingReturn && _grab != null && !_grab.isSelected)
        {
            _pendingReturn = false;
            StartReturn();
        }

        _idle = _grab == null || !_grab.isSelected;
        if (_idle) _returnRoutine = null;
    }

    void OnDisable()
    {
        if (_grab != null)
        {
            _grab.selectEntered.RemoveListener(OnSelectEntered);
            _grab.selectExited.RemoveListener(OnSelectExited);
        }

        if (_returnRoutine != null) { StopCoroutine(_returnRoutine); _returnRoutine = null; }
    }

    void Update()
    {
        if (!_idle) return;

        // Only touch velocities if the body is dynamic
        if (_rb && !_rb.isKinematic)
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
        _bobPhase = 0f;
    }

    // XR events
    void OnSelectEntered(SelectEnterEventArgs _)
    {
        _idle = false;
        if (_returnRoutine != null) { StopCoroutine(_returnRoutine); _returnRoutine = null; }
    }

    void OnSelectExited(SelectExitEventArgs _)
    {
        // If we're being turned off (e.g., hidden when worn), don't start coroutines now
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            _pendingReturn = true; // run when re-enabled
            return;
        }

        // Ensure physics won't fight the return:
        if (_rb)
        {
            if (!_rb.isKinematic)
            {
                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
            _rb.isKinematic = true;
            _rb.useGravity  = false;
        }

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

        if (_rb)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }

        Vector3 start = transform.position;
        Vector3 end   = new Vector3(start.x, _baseY, start.z);
        float t = 0f;
        float dur = Mathf.Max(0.01f, returnDuration);

        float Ease(float x)
        {
            if (returnEase != null && returnEase.keys != null && returnEase.keys.Length > 0)
                return Mathf.Clamp01(returnEase.Evaluate(Mathf.Clamp01(x)));
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(x));
        }

        while (t < dur)
        {
            t += Time.deltaTime;
            float u = Ease(t / dur);
            transform.position = Vector3.LerpUnclamped(start, end, u);
            yield return null;
        }

        var p = transform.position;
        p.y = _baseY;
        transform.position = p;

        float offset = transform.position.y - _baseY;
        _bobPhase = (bobAmplitude > 0.0001f) ? Mathf.Asin(Mathf.Clamp(offset / bobAmplitude, -1f, 1f)) : 0f;

        _idle = true;
        _returnRoutine = null;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
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
