using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Respawn")]
    public Transform respawnPoint;

    // CHANGED: give yourself a longer “black time” before fade-in
    public float blackFadeIn  = 0.35f;  // to black
    public float blackHold    = 0.10f;  // existing hold
    public float blackHoldExtra = 1.50f; // NEW: extra hold (1–2s as requested)
    public float blackFadeOut = 0.40f;  // back to scene

    [Header("Hit Feedback")]
    public float redFlashIn   = 0.08f;
    public float redFlashHold = 0.06f;
    public float redFlashOut  = 0.12f;
    public ParticleSystem deathExplosionPrefab;

    [Header("XR Camera (for overlays)")]
    public Camera xrCamera; // assign your XR camera; if null, uses Camera.main

    [Header("Player (optional override)")]
    public Transform playerRootOverride;
    public string playerTag = "Player";

    [Header("Respawn Safety")]
    [Tooltip("BoxCollider that bounds the hallway you want the player to appear inside.")]
    public BoxCollider respawnHallwayBounds;   // NEW
    [Tooltip("Small clearance so the capsule doesn't clip walls.")]
    public float boundsMargin = 0.02f;         // NEW
    [Tooltip("Extra settle time (physics frames) before we fade back in.")]
    public int settleFixedFrames = 2;          // NEW

    Canvas _canvas;
    Image _red;
    Image _black;
    bool _isRespawning;
    public bool IsRespawning => _isRespawning;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (!xrCamera) xrCamera = Camera.main;
        BuildOverlayUI();
    }

    void BuildOverlayUI()
    {
        _canvas = new GameObject("DeathOverlayCanvas").AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceCamera;
        _canvas.worldCamera = xrCamera;
        _canvas.planeDistance = 0.5f;
        _canvas.sortingOrder = 10000;
        _canvas.gameObject.AddComponent<CanvasScaler>();
        _canvas.gameObject.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(_canvas.gameObject);

        RectTransform FullRect(string name, Color col, out Image img)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_canvas.transform, false);
            img = go.AddComponent<Image>();
            img.color = col;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        FullRect("BlackFade", new Color(0,0,0,0), out _black);
        FullRect("RedFlash",  new Color(1,0,0,0), out _red);
    }

    public void KillPlayer(Transform playerRoot)
    {
        if (_isRespawning) return;
        if (!respawnPoint)
        {
            Debug.LogWarning("[LevelManager] No respawnPoint set.");
            return;
        }
        var target = ResolvePlayerRoot(playerRoot);
        if (target == null)
        {
            Debug.LogWarning("[LevelManager] Could not resolve a player root to respawn.");
            return;
        }
        StartCoroutine(DeathSequence(target));
    }

    Transform ResolvePlayerRoot(Transform candidate)
    {
        if (playerRootOverride) return playerRootOverride;
        if (candidate && candidate.CompareTag(playerTag)) return candidate;
        var tagged = GameObject.FindWithTag(playerTag);
        if (tagged) return tagged.transform;
        if (xrCamera)
        {
            var ccs = FindObjectsOfType<CharacterController>();
            float best = float.PositiveInfinity;
            Transform bestT = null;
            foreach (var cc in ccs)
            {
                float d = (cc.transform.position - xrCamera.transform.position).sqrMagnitude;
                if (d < best) { best = d; bestT = cc.transform.root; }
            }
            if (bestT) return bestT;
        }
        return candidate;
    }

    IEnumerator DeathSequence(Transform playerRoot)
    {
        _isRespawning = true;

        // particle burst at player
        if (deathExplosionPrefab)
        {
            var fx = Instantiate(deathExplosionPrefab, playerRoot.position, Quaternion.identity);
            Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax + 0.5f);
        }

        // quick red flash
        yield return StartCoroutine(Flash(_red, redFlashIn, redFlashHold, redFlashOut));

        // fade to black (same speed) …
        yield return StartCoroutine(FadeTo(_black, 1f, blackFadeIn));
        // … then hold longer (extra second or two)
        yield return new WaitForSeconds(blackHold + Mathf.Max(0f, blackHoldExtra));

        // place the player safely while fully black
        yield return StartCoroutine(SafePlacePlayer(playerRoot));

        // fade back in
        yield return StartCoroutine(FadeTo(_black, 0f, blackFadeOut));

        _isRespawning = false;
    }

    IEnumerator SafePlacePlayer(Transform playerRoot)
    {
        var cc = playerRoot.GetComponentInChildren<CharacterController>();
        // Temporarily disable the controller so we can teleport cleanly
        if (cc) cc.enabled = false;

        // Zero any rigidbody velocities on the rig (prevents “launching”)
        foreach (var rb in playerRoot.GetComponentsInChildren<Rigidbody>())
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Compute a safe root position
        Vector3 newRootPos = respawnPoint.position;
        Quaternion newRootRot = Quaternion.Euler(0f, respawnPoint.eulerAngles.y, 0f);

        if (cc && respawnHallwayBounds)
        {
            // Clamp the capsule center inside the box, accounting for radius/height
            Vector3 desiredCenter = newRootPos + cc.center;
            Vector3 clampedCenter = ClampCapsuleCenterInside(respawnHallwayBounds, desiredCenter, cc.radius, cc.height, boundsMargin);

            // Optional: snap to floor under the clamped center
            Vector3 probeStart = clampedCenter + Vector3.up * 0.3f;
            if (Physics.Raycast(probeStart, Vector3.down, out var hit, 5f, ~0, QueryTriggerInteraction.Ignore))
            {
                float bottomToCenter = cc.height * 0.5f - cc.radius; // center above bottom
                clampedCenter.y = hit.point.y + bottomToCenter + cc.radius + 0.01f;
            }

            newRootPos = clampedCenter - cc.center;
        }

        // Teleport
        playerRoot.SetPositionAndRotation(newRootPos, newRootRot);

        // Let physics settle a couple of fixed frames while still black
        for (int i = 0; i < Mathf.Max(0, settleFixedFrames); i++)
            yield return new WaitForFixedUpdate();

        if (cc) cc.enabled = true;
    }

    // Clamp a capsule center inside a BoxCollider in local space, with margins
    Vector3 ClampCapsuleCenterInside(BoxCollider box, Vector3 capsuleCenterWorld, float radius, float height, float margin)
    {
        var t = box.transform;

        // Convert to the collider's local space, offset by its center
        Vector3 local = t.InverseTransformPoint(capsuleCenterWorld) - box.center;
        Vector3 half = box.size * 0.5f;

        // Effective half extents reduced by capsule extents + margin
        float halfHeight = Mathf.Max(radius, height * 0.5f);
        Vector3 reduce = new Vector3(radius + margin, halfHeight + margin, radius + margin);

        // Clamp inside
        local.x = Mathf.Clamp(local.x, -half.x + reduce.x, half.x - reduce.x);
        local.y = Mathf.Clamp(local.y, -half.y + reduce.y, half.y - reduce.y);
        local.z = Mathf.Clamp(local.z, -half.z + reduce.z, half.z - reduce.z);

        // Back to world
        return t.TransformPoint(local + box.center);
    }

    IEnumerator FadeTo(Image img, float targetA, float duration)
    {
        float startA = img.color.a;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startA, targetA, Mathf.SmoothStep(0f, 1f, t / duration));
            var c = img.color; c.a = a; img.color = c;
            yield return null;
        }
        var c2 = img.color; c2.a = targetA; img.color = c2;
    }

    IEnumerator Flash(Image img, float inDur, float holdDur, float outDur)
    {
        yield return FadeTo(img, 0.9f, inDur);
        if (holdDur > 0f) yield return new WaitForSeconds(holdDur);
        yield return FadeTo(img, 0f, outDur);
    }
}
