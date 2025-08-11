using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Respawn")]
    public Transform respawnPoint;

    // Black screen timings
    public float blackFadeIn    = 0.35f;  // to black
    public float blackHold      = 0.10f;  // base hold
    public float blackHoldExtra = 1.50f;  // extra hold while placing
    public float blackFadeOut   = 0.40f;  // back to scene

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

    [Header("Respawn Safety (no bounds collider)")]
    [Tooltip("Layers considered solid ground when snapping the player to the floor at respawn.")]
    public LayerMask groundMask = ~0;
    [Tooltip("Start the ground probe this far above the spawn point.")]
    public float groundProbeUp = 1.5f;
    [Tooltip("Probe this far downward from the start height.")]
    public float groundProbeDown = 6f;
    [Tooltip("Small clearance above the floor so the capsule bottom doesn’t clip.")]
    public float groundClearance = 0.02f;
    [Tooltip("Extra settle time (physics frames) before we fade back in.")]
    public int settleFixedFrames = 2;

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

        FullRect("BlackFade", new Color(0, 0, 0, 0), out _black);
        FullRect("RedFlash",  new Color(1, 0, 0, 0), out _red);
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

        // fade to black
        yield return StartCoroutine(FadeTo(_black, 1f, blackFadeIn));
        // hold while we reset things
        yield return new WaitForSeconds(blackHold + Mathf.Max(0f, blackHoldExtra));

        // Reset puzzle items (headsets etc.)
        if (HMDManagerLaser.Instance) HMDManagerLaser.Instance.ResetHeadsetsToSpawn();

        // 🔄 Also clear any floating menu headset stacks
        ClearAllHeadsetUIStacks();

        // Hard-freeze ALL player rigidbodies while we reposition (prevents any speed carryover)
        var frozen = FreezeAllRigidbodies(playerRoot, freeze: true);

        // place player safely while fully black
        yield return StartCoroutine(SafePlacePlayer(playerRoot));

        // allow a couple physics ticks while still frozen & black
        for (int i = 0; i < Mathf.Max(0, settleFixedFrames); i++)
            yield return new WaitForFixedUpdate();

        // unfreeze before we fade back in
        FreezeAllRigidbodies(playerRoot, freeze: false, frozenStates: frozen);

        // fade back
        yield return StartCoroutine(FadeTo(_black, 0f, blackFadeOut));

        _isRespawning = false;
    }

    void ClearAllHeadsetUIStacks()
    {
        var menus = FindObjectsOfType<MenuLaser>(true);
        foreach (var m in menus)
        {
            if (!m) continue;
            try { m.ClearHeadsetUI(); } catch { /* ignore */ }
        }
    }

    IEnumerator SafePlacePlayer(Transform playerRoot)
    {
        var cc = playerRoot.GetComponentInChildren<CharacterController>();

        // Disable controller to teleport cleanly
        if (cc) cc.enabled = false;

        // Target yaw = respawn yaw
        Quaternion newRootRot = Quaternion.Euler(0f, respawnPoint.eulerAngles.y, 0f);

        // Compute a safe position at the spawn (no bounds collider required)
        Vector3 newRootPos = ComputeSafeRootAtSpawn(playerRoot, cc);

        // Teleport
        playerRoot.SetPositionAndRotation(newRootPos, newRootRot);

        // Re-enable controller after move
        if (cc) cc.enabled = true;

        yield break;
    }

    Vector3 ComputeSafeRootAtSpawn(Transform playerRoot, CharacterController cc)
    {
        // Start exactly at the respawn point
        Vector3 rootPos = respawnPoint.position;

        // Raycast down to find floor under/near the spawn
        float startY = respawnPoint.position.y + groundProbeUp;
        Vector3 probeStart = new Vector3(respawnPoint.position.x, startY, respawnPoint.position.z);
        float maxDist = groundProbeUp + groundProbeDown;

        float floorY;
        if (Physics.Raycast(probeStart, Vector3.down, out var hit, maxDist, groundMask, QueryTriggerInteraction.Ignore))
            floorY = hit.point.y;
        else
            floorY = respawnPoint.position.y; // fallback if no ground found

        if (cc)
        {
            // Place capsule bottom at floor + clearance
            float halfHeight = Mathf.Max(cc.height * 0.5f, cc.radius);
            float bottomToCenter = halfHeight - cc.radius;

            // desired world center Y
            float desiredCenterY = floorY + cc.radius + groundClearance + bottomToCenter;

            Vector3 centerWorld = rootPos + cc.center;
            centerWorld.y = desiredCenterY;
            rootPos = centerWorld - cc.center;

            // If overlapping here, nudge upward until clear
            const int steps = 12;
            const float stepUp = 0.04f; // ~0.5m max
            if (IsCapsuleOverlapping(rootPos, Quaternion.identity, cc, playerRoot))
            {
                for (int i = 0; i < steps; i++)
                {
                    Vector3 tryPos = rootPos + Vector3.up * (i + 1) * stepUp;
                    if (!IsCapsuleOverlapping(tryPos, Quaternion.identity, cc, playerRoot))
                    {
                        rootPos = tryPos;
                        break;
                    }
                }
            }
        }
        else
        {
            // No CC — just sit the root a hair above floor
            rootPos.y = floorY + groundClearance;
        }

        return rootPos;
    }

    bool IsCapsuleOverlapping(Vector3 rootPos, Quaternion rootRot, CharacterController cc, Transform playerRoot)
    {
        float halfHeight = Mathf.Max(cc.height * 0.5f, cc.radius);
        Vector3 up = rootRot * Vector3.up;

        Vector3 centerWorld = rootPos + cc.center;
        Vector3 p1 = centerWorld + up * (halfHeight - cc.radius);
        Vector3 p2 = centerWorld - up * (halfHeight - cc.radius);

        var hits = Physics.OverlapCapsule(p1, p2, cc.radius, ~0, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (!h || h.transform == null) continue;
            if (!h.transform.IsChildOf(playerRoot)) return true; // hit something that's not the rig
        }
        return false;
    }

    // --- Hard-freeze/unfreeze ALL rigidbodies under the player root during respawn ---
    struct RBState
    {
        public Rigidbody rb;
        public bool wasKinematic;
        public bool usedGravity;
        public RigidbodyConstraints constraints;
    }

    List<RBState> FreezeAllRigidbodies(Transform root, bool freeze, List<RBState> frozenStates = null)
    {
        if (freeze)
        {
            var list = new List<RBState>();
            foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
            {
                var state = new RBState
                {
                    rb = rb,
                    wasKinematic = rb.isKinematic,
                    usedGravity = rb.useGravity,
                    constraints = rb.constraints
                };

                // stop motion safely (only on dynamic bodies)
                if (!rb.isKinematic)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                rb.isKinematic = true;
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeAll;

                list.Add(state);
            }
            return list;
        }
        else
        {
            if (frozenStates != null)
            {
                foreach (var st in frozenStates)
                {
                    if (!st.rb) continue;
                    st.rb.constraints = st.constraints;
                    st.rb.useGravity  = st.usedGravity;
                    st.rb.isKinematic = st.wasKinematic;
                }
            }
            return null;
        }
    }

    // --- UI helpers ---
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
