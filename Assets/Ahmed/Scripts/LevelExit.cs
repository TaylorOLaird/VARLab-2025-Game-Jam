using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))] // ensures reliable trigger callbacks
public class EndHallwayTrigger : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Tag on the XR Rig root.")]
    public string playerTag = "Player";

    [Header("Fade & Load")]
    [Tooltip("The XR camera.")]
    public Camera xrCamera;
    [Tooltip("Scene name to load")]
    public string sceneName;
    [Tooltip("Optional alternative to Scene Name.")]
    public int sceneBuildIndex = -1;
    [Tooltip("How long to fade to black (seconds).")]
    [Range(0.05f, 5f)] public float fadeOutDuration = 1.5f;
    [Tooltip("Hold on black before switching scenes (seconds).")]
    [Range(0f, 5f)] public float blackHold = 0.5f;

    // Runtime
    Rigidbody _rb;
    BoxCollider _col;
    bool _triggered;

    // Overlay
    Canvas _canvas;
    Image _black;

    void Reset()
    {
        // Collider as trigger
        _col = GetComponent<BoxCollider>();
        _col.isTrigger = true;

        // Kinematic RB for triggers
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.useGravity = false;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    void Awake()
    {
        if (!_col) _col = GetComponent<BoxCollider>();
        _col.isTrigger = true;

        if (!_rb) _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.useGravity = false;

        if (!xrCamera) xrCamera = Camera.main;
    }

    void OnTriggerEnter(Collider other) => TryFinish(other);
    void OnTriggerStay(Collider other)  => TryFinish(other);

    void TryFinish(Collider other)
    {
        if (_triggered || other == null) return;
        var root = other.transform.root;
        if (!root.CompareTag(playerTag)) return;

        // Start once
        _triggered = true;
        StartCoroutine(FadeOutAndLoad());
    }

    IEnumerator FadeOutAndLoad()
    {
        BuildOverlayIfNeeded();

        // Fade to black
        yield return StartCoroutine(FadeTo(_black, 1f, fadeOutDuration));

        // Hold
        if (blackHold > 0f) yield return new WaitForSeconds(blackHold);

        // Load next scene
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
        else if (sceneBuildIndex >= 0)
        {
            SceneManager.LoadScene(sceneBuildIndex, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogWarning("[EndHallwayTrigger] No scene specified. Set Scene Name or Build Index.");
        }
    }

    void BuildOverlayIfNeeded()
    {
        if (_canvas) return;

        _canvas = new GameObject("EndFadeCanvas").AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceCamera;
        _canvas.worldCamera = xrCamera;
        _canvas.planeDistance = 0.5f;    // in front of camera
        _canvas.sortingOrder = 10000;    // on top of everything
        _canvas.gameObject.AddComponent<CanvasScaler>();
        _canvas.gameObject.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(_canvas.gameObject);

        // Fullscreen black image
        var go = new GameObject("Black");
        go.transform.SetParent(_canvas.transform, false);
        _black = go.AddComponent<Image>();
        _black.color = new Color(0, 0, 0, 0);
        var rt = _black.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    IEnumerator FadeTo(Image img, float targetA, float duration)
    {
        float startA = img.color.a;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startA, targetA, Mathf.SmoothStep(0f, 1f, t / Mathf.Max(0.0001f, duration)));
            var c = img.color; c.a = a; img.color = c;
            yield return null;
        }
        var c2 = img.color; c2.a = targetA; img.color = c2;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.15f, 0.85f, 1f, 0.35f);
        if (TryGetComponent(out BoxCollider bc))
        {
            var m = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(bc.center, bc.size);
            Gizmos.matrix = m;
        }
    }
}