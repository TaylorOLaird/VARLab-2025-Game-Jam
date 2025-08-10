using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Rigidbody))] // needed so the child trigger fires reliably
public class LaserEmitter : MonoBehaviour
{
    public enum LaserType { Red, Green, Blue }

    [Header("Laser")]
    public LaserType laser = LaserType.Red;
    [Range(0.1f, 200f)] public float maxDistance = 50f;
    public LayerMask hitMask = ~0;

    [Header("Beam Renderers (URP/Unlit Additive)")]
    public LineRenderer coreLR; // beam_core.png, Transparent+Additive
    public LineRenderer glowLR; // beam_glow.png, Transparent+Additive

    [Header("Ring Identification (no Renderer ref needed)")]
    [Tooltip("Assign the ring material asset used by the ring submesh in the FBX. Emission should be enabled on this asset.")]
    public Material ringMaterialAsset;
    [Tooltip("If ringMaterialAsset is not assigned, we'll match by material name containing this text.")]
    public string ringMaterialNameContains = "Ring";

    [Header("Look")]
    public float coreWidth = 0.02f;
    public float glowWidthMultiplier = 3f;

    [Tooltip("Beam intensities (keep modest to avoid ACES clipping to white).")]
    [Range(0.5f, 3.0f)] public float coreIntensity = 1.8f;
    [Range(0.5f, 3.0f)] public float glowIntensity = 2.0f;

    [Tooltip("Ring emission intensity (Lit/Emission).")]
    [Range(0.5f, 3.0f)] public float ringEmission = 1.4f;

    public float flicker = 0.05f;
    public float scrollSpeed = 3f;
    public float tilesPerMeter = 4f;

    [Header("Impact (optional)")]
    public Transform impactDecal;
    public ParticleSystem impactParticles;

    [Header("Lethal Beam Trigger")]
    public bool lethal = true;
    [Tooltip("Radius (thickness) of the hit trigger around the beam (meters).")]
    public float beamRadius = 0.06f;
    [Tooltip("Tag on the XR Rig/player root.")]
    public string playerTag = "Player";

    struct RingSlot
    {
        public Renderer r;
        public int matIndex;
        public MaterialPropertyBlock mpb;
    }

    MaterialPropertyBlock _mpbCore, _mpbGlow;
    List<RingSlot> _ringSlots = new();
    Vector3 _start, _end;
    LaserType _lastAppliedLaser;

    // physics + trigger
    Rigidbody _rb;
    Transform _beamTriggerTf;
    BoxCollider _beamTrigger;

    void Reset()
    {
        // Configure Rigidbody for triggers
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    void OnEnable()   { Ensure(); CacheRingSlots(); UpdateAll(true); }
    void OnValidate() { Ensure(); CacheRingSlots(); UpdateAll(true); }
    void Update()     { UpdateAll(false); }

    void Ensure()
    {
        // RB for trigger callbacks
        if (!_rb) _rb = GetComponent<Rigidbody>();
        if (_rb)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        // MPBs + LR settings
        _mpbCore ??= new MaterialPropertyBlock();
        _mpbGlow ??= new MaterialPropertyBlock();
        if (coreLR) coreLR.textureMode = LineTextureMode.Tile;
        if (glowLR) glowLR.textureMode = LineTextureMode.Tile;

        EnsureBeamTrigger();
    }

    void EnsureBeamTrigger()
    {
        // Create/find a child with a BoxCollider trigger that we align to the beam
        if (_beamTriggerTf == null)
        {
            var t = transform.Find("__BeamTrigger");
            if (!t)
            {
                var go = new GameObject("__BeamTrigger");
                go.layer = gameObject.layer;           // inherit layer
                go.transform.SetParent(transform, false);
                _beamTriggerTf = go.transform;
                _beamTrigger = go.AddComponent<BoxCollider>();
                _beamTrigger.isTrigger = true;

                // Relay to parent
                var relay = go.AddComponent<BeamHitRelay>();
                relay.owner = this;
            }
            else
            {
                _beamTriggerTf = t;
                _beamTrigger = _beamTriggerTf.GetComponent<BoxCollider>();
                if (!_beamTrigger) _beamTrigger = _beamTriggerTf.gameObject.AddComponent<BoxCollider>();
                _beamTrigger.isTrigger = true;

                var relay = _beamTriggerTf.GetComponent<BeamHitRelay>();
                if (!relay) relay = _beamTriggerTf.gameObject.AddComponent<BeamHitRelay>();
                relay.owner = this;
            }
        }
    }

    void CacheRingSlots()
    {
        _ringSlots.Clear();
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null) continue;

                bool match = false;
                if (ringMaterialAsset && m == ringMaterialAsset) match = true;
                else if (!ringMaterialAsset && !string.IsNullOrEmpty(ringMaterialNameContains) &&
                         (m.name.Contains(ringMaterialNameContains))) match = true;

                if (match)
                {
                    // IMPORTANT: The asset's Emission must be enabled once in the inspector.
                    var slot = new RingSlot { r = r, matIndex = i, mpb = new MaterialPropertyBlock() };
                    _ringSlots.Add(slot);
                }
            }
        }
        // Optional: warn if none found
        // if (_ringSlots.Count == 0) Debug.LogWarning($"[{name}] No ring slots found.");
    }

    void UpdateAll(bool force)
    {
        _start = transform.position;
        var dir = transform.forward;

        // Where does the beam end?
        if (Physics.Raycast(_start, dir, out var hit, maxDistance, hitMask, QueryTriggerInteraction.Ignore))
        {
            _end = hit.point;
            if (impactDecal) { impactDecal.gameObject.SetActive(true); impactDecal.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.normal)); }
            if (impactParticles)
            {
                impactParticles.transform.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.normal));
                if (!impactParticles.isPlaying) impactParticles.Play();
            }
        }
        else
        {
            _end = _start + dir * maxDistance;
            if (impactDecal) impactDecal.gameObject.SetActive(false);
            if (impactParticles && impactParticles.isPlaying) impactParticles.Stop();
        }

        // subtle width shimmer
        float f = 1f + ((flicker > 0f) ? (Mathf.PerlinNoise(Time.time * 3.1f, 0f) - 0.5f) * 2f * flicker : 0f);
        float coreW = Mathf.Max(0.0005f, coreWidth * f);
        float glowW = coreW * glowWidthMultiplier;
        ApplyLine(coreLR, coreW, _start, _end, true);
        ApplyLine(glowLR, glowW, _start, _end, false);

        // ACES-friendly primaries
        Color baseCol = laser switch
        {
            LaserType.Green => new Color(0.00f, 0.90f, 0.15f, 1f),
            LaserType.Blue  => new Color(0.10f, 0.30f, 1.00f, 1f),
            _               => new Color(1.00f, 0.05f, 0.02f, 1f),
        };

        // Beam tint (URP/Unlit Additive): keep intensities modest
        Color coreCol = Color.Lerp(baseCol, Color.white, 0.35f) * coreIntensity;
        Color glowCol = baseCol * glowIntensity;

        float length = Vector3.Distance(_start, _end);
        if (coreLR) { coreLR.GetPropertyBlock(_mpbCore);  SetBeamMPB(_mpbCore, coreCol, length);  coreLR.SetPropertyBlock(_mpbCore); }
        if (glowLR) { glowLR.GetPropertyBlock(_mpbGlow);  SetBeamMPB(_mpbGlow, glowCol, length);  glowLR.SetPropertyBlock(_mpbGlow); }

        // Update ring only when color changed or forced
        if (force || _lastAppliedLaser != laser)
        {
            SetRingPerInstance(baseCol, ringEmission);
            _lastAppliedLaser = laser;
        }

        // Update lethal beam trigger to match the visible segment
        if (lethal) UpdateBeamTrigger(_start, _end, beamRadius);
        else if (_beamTrigger) _beamTrigger.enabled = false;
    }

    void ApplyLine(LineRenderer lr, float width, Vector3 a, Vector3 b, bool caps)
    {
        if (!lr) return;
        lr.positionCount = 2;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.alignment = LineAlignment.View;
        lr.numCornerVertices = caps ? 2 : 0;
        lr.numCapVertices = caps ? 2 : 0;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.generateLightingData = false;
        lr.widthCurve = new AnimationCurve(new Keyframe(0, width), new Keyframe(1, width));
    }

    // URP/Unlit: drive ONLY _BaseColor (no emission needed for beam)
    void SetBeamMPB(MaterialPropertyBlock mpb, Color c, float len)
    {
        mpb.SetColor("_BaseColor", c);
        float tiles = Mathf.Max(0.001f, len * tilesPerMeter);
        float offset = (Time.time * scrollSpeed) % 1f;
        mpb.SetVector("_BaseMap_ST", new Vector4(tiles, 1f, -offset, 0f));
        mpb.SetVector("_MainTex_ST", new Vector4(tiles, 1f, -offset, 0f)); // harmless fallback
    }

    // Per-renderer override for ring using MPB (requires Emission enabled on the asset)
    void SetRingPerInstance(Color baseCol, float intensity)
    {
        if (_ringSlots.Count == 0) return;

        // Dark base; look via emission
        Color baseDark = new Color(0.02f, 0.02f, 0.02f, 1f);
        Color emissive = baseCol * intensity;

        for (int i = 0; i < _ringSlots.Count; i++)
        {
            var slot = _ringSlots[i];
            var mpb = slot.mpb ?? new MaterialPropertyBlock();

            mpb.SetColor("_BaseColor", baseDark);
            mpb.SetColor("_EmissionColor", emissive);

            slot.r.SetPropertyBlock(mpb, slot.matIndex);

            slot.mpb = mpb;
            _ringSlots[i] = slot;
        }
    }

    // --- LETHAL TRIGGER ---

    void UpdateBeamTrigger(Vector3 a, Vector3 b, float radius)
    {
        EnsureBeamTrigger();
        if (!_beamTriggerTf || !_beamTrigger) return;

        _beamTrigger.enabled = true;

        Vector3 mid = (a + b) * 0.5f;
        Vector3 dir = (b - a);
        float len = dir.magnitude;
        if (len < 0.001f) len = 0.001f;

        _beamTriggerTf.SetPositionAndRotation(mid, Quaternion.LookRotation(dir.normalized, Vector3.up));
        _beamTrigger.size = new Vector3(Mathf.Max(0.001f, radius * 2f), Mathf.Max(0.001f, radius * 2f), len);
        _beamTrigger.center = Vector3.zero; // already centered by transform
    }

    internal void OnBeamTriggerTouched(Collider other)
    {
        if (!lethal || other == null) return;

        var root = other.transform.root;
        if (!root.CompareTag(playerTag)) return;

        // Hand off to your level manager (guard if it's busy)
        var lm = LevelManager.Instance;
        if (lm != null)
        {
            if (!lm.IsRespawning) lm.KillPlayer(root);
        }
        else
        {
            Debug.LogWarning("[LaserEmitter] No LevelManager.Instance found.");
        }
    }

    // Child relay to forward trigger messages to the parent
    private class BeamHitRelay : MonoBehaviour
    {
        [HideInInspector] public LaserEmitter owner;
        void OnTriggerEnter(Collider other) => owner?.OnBeamTriggerTouched(other);
        void OnTriggerStay(Collider other)  => owner?.OnBeamTriggerTouched(other);
    }

    public void SetLaser(LaserType t) { laser = t; UpdateAll(true); }
}
