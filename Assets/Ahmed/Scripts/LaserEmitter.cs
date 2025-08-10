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
    [Range(0.5f, 3.0f)] public float coreIntensity = 1.8f;
    [Range(0.5f, 3.0f)] public float glowIntensity = 2.0f;
    [Range(0.5f, 3.0f)] public float ringEmission = 1.4f;
    public float flicker = 0.05f;
    public float scrollSpeed = 3f;
    public float tilesPerMeter = 4f;

    [Header("Impact (optional)")]
    public Transform impactDecal;
    public ParticleSystem impactParticles;

    [Header("Lethal Beam Trigger")]
    public bool lethal = true;
    public float beamRadius = 0.06f;
    public string playerTag = "Player";

    [Header("Audio")]
    [Tooltip("Looping hum .wav (set to Loop in importer).")]
    public AudioClip humLoop;
    [Tooltip("If assigned, used as player head. Otherwise we auto-find (LevelManager.xrCamera, Camera.main, or the Player-tagged rig).")]
    public Transform playerHead;
    [Tooltip("Base volume far from the beam.")]
    [Range(0f, 1f)] public float humBaseVolume = 0.15f;
    [Tooltip("Volume when very close to the beam.")]
    [Range(0f, 1f)] public float humNearVolume = 0.9f;
    [Tooltip("Distance (m) considered 'very close'.")]
    public float humNearDistance = 0.6f;
    [Tooltip("Max distance (m) at which the hum is audible.")]
    public float humMaxDistance = 10f;
    [Tooltip("How snappy the audio source follows the closest point (0 = snap).")]
    [Range(0f, 1f)] public float humFollowSmoothing = 0.15f;
    public AudioClip deathSfx;
    [Range(0f, 1f)] public float deathSfxVolume = 1f;

    struct RingSlot { public Renderer r; public int matIndex; public MaterialPropertyBlock mpb; }

    MaterialPropertyBlock _mpbCore, _mpbGlow;
    List<RingSlot> _ringSlots = new();
    Vector3 _start, _end;
    LaserType _lastAppliedLaser;

    // physics + trigger
    Rigidbody _rb;
    Transform _beamTriggerTf;
    BoxCollider _beamTrigger;

    // audio
    Transform _beamAudioTf;
    AudioSource _beamAudio;

    void Reset()
    {
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    void OnEnable() { Ensure(); CacheRingSlots(); UpdateAll(true); }
    void OnValidate() { Ensure(); CacheRingSlots(); UpdateAll(true); }
    void OnDisable() { if (_beamAudio) _beamAudio.Stop(); }
    void Update() { UpdateAll(false); }

    void Ensure()
    {
        if (!_rb) _rb = GetComponent<Rigidbody>();
        if (_rb)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        _mpbCore ??= new MaterialPropertyBlock();
        _mpbGlow ??= new MaterialPropertyBlock();
        if (coreLR) coreLR.textureMode = LineTextureMode.Tile;
        if (glowLR) glowLR.textureMode = LineTextureMode.Tile;

        EnsureBeamTrigger();
        EnsureBeamAudio();
    }

    void EnsureBeamTrigger()
    {
        if (_beamTriggerTf == null)
        {
            var t = transform.Find("__BeamTrigger");
            if (!t)
            {
                var go = new GameObject("__BeamTrigger");
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);
                _beamTriggerTf = go.transform;
                _beamTrigger = go.AddComponent<BoxCollider>();
                _beamTrigger.isTrigger = true;

                var relay = go.AddComponent<BeamHitRelay>();
                relay.owner = this;
            }
            else
            {
                _beamTriggerTf = t;
                _beamTrigger = _beamTriggerTf.GetComponent<BoxCollider>() ?? _beamTriggerTf.gameObject.AddComponent<BoxCollider>();
                _beamTrigger.isTrigger = true;

                var relay = _beamTriggerTf.GetComponent<BeamHitRelay>() ?? _beamTriggerTf.gameObject.AddComponent<BeamHitRelay>();
                relay.owner = this;
            }
        }
    }

    // -------- NEW: create a follower AudioSource ----------
    void EnsureBeamAudio()
    {
        if (_beamAudioTf == null)
        {
            var t = transform.Find("__BeamAudio");
            if (!t)
            {
                var go = new GameObject("__BeamAudio");
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);
                _beamAudioTf = go.transform;
                _beamAudio = go.AddComponent<AudioSource>();
            }
            else
            {
                _beamAudioTf = t;
                _beamAudio = _beamAudioTf.GetComponent<AudioSource>() ?? _beamAudioTf.gameObject.AddComponent<AudioSource>();
            }

            // Configure once
            _beamAudio.playOnAwake = false;
            _beamAudio.loop = true;
            _beamAudio.spatialBlend = 1f;          // 3D
            _beamAudio.dopplerLevel = 0f;          // avoid pitch shift
            _beamAudio.spread = 0f;
            _beamAudio.priority = 160;             // mid priority
            _beamAudio.rolloffMode = AudioRolloffMode.Custom;
            // Flat custom rolloff (we control volume ourselves):
            var flat = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));
            _beamAudio.SetCustomCurve(AudioSourceCurveType.CustomRolloff, flat);
        }

        // Assign clip if needed
        if (_beamAudio && _beamAudio.clip != humLoop) _beamAudio.clip = humLoop;
    }
    // ------------------------------------------------------

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
                else if (!ringMaterialAsset && !string.IsNullOrEmpty(ringMaterialNameContains) && m.name.Contains(ringMaterialNameContains)) match = true;

                if (match)
                {
                    var slot = new RingSlot { r = r, matIndex = i, mpb = new MaterialPropertyBlock() };
                    _ringSlots.Add(slot);
                }
            }
        }
    }

    void UpdateAll(bool force)
    {
        _start = transform.position;
        var dir = transform.forward;

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
            LaserType.Blue => new Color(0.10f, 0.30f, 1.00f, 1f),
            _ => new Color(1.00f, 0.05f, 0.02f, 1f),
        };

        Color coreCol = Color.Lerp(baseCol, Color.white, 0.35f) * coreIntensity;
        Color glowCol = baseCol * glowIntensity;

        float length = Vector3.Distance(_start, _end);
        if (coreLR) { coreLR.GetPropertyBlock(_mpbCore); SetBeamMPB(_mpbCore, coreCol, length); coreLR.SetPropertyBlock(_mpbCore); }
        if (glowLR) { glowLR.GetPropertyBlock(_mpbGlow); SetBeamMPB(_mpbGlow, glowCol, length); glowLR.SetPropertyBlock(_mpbGlow); }

        if (force || _lastAppliedLaser != laser)
        {
            SetRingPerInstance(baseCol, ringEmission);
            _lastAppliedLaser = laser;
        }

        if (lethal) UpdateBeamTrigger(_start, _end, beamRadius);
        else if (_beamTrigger) _beamTrigger.enabled = false;

        // -------- NEW: update hum follower ----------
        UpdateHumAudio(_start, _end);
        // -------------------------------------------
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

    void SetBeamMPB(MaterialPropertyBlock mpb, Color c, float len)
    {
        mpb.SetColor("_BaseColor", c);
        float tiles = Mathf.Max(0.001f, len * tilesPerMeter);
        float offset = (Time.time * scrollSpeed) % 1f;
        mpb.SetVector("_BaseMap_ST", new Vector4(tiles, 1f, -offset, 0f));
        mpb.SetVector("_MainTex_ST", new Vector4(tiles, 1f, -offset, 0f));
    }

    void SetRingPerInstance(Color baseCol, float intensity)
    {
        if (_ringSlots.Count == 0) return;

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
        float len = Mathf.Max(0.001f, dir.magnitude);

        _beamTriggerTf.SetPositionAndRotation(mid, Quaternion.LookRotation(dir.normalized, Vector3.up));
        _beamTrigger.size = new Vector3(Mathf.Max(0.001f, radius * 2f), Mathf.Max(0.001f, radius * 2f), len);
        _beamTrigger.center = Vector3.zero;
    }

internal void OnBeamTriggerTouched(Collider other)
{
    if (!lethal || other == null) return;

    var root = other.transform.root;
    if (!root.CompareTag(playerTag)) return;

    var lm = LevelManager.Instance;
    if (lm != null)
    {
        // Only fire once per death
        if (!lm.IsRespawning)
        {
            if (deathSfx)
            {
                AudioSource.PlayClipAtPoint(deathSfx, this.playerHead.position, Mathf.Clamp01(deathSfxVolume));
            }

            lm.KillPlayer(root);
        }
    }
    else
    {
        Debug.LogWarning("[LaserEmitter] No LevelManager.Instance found.");
    }
}


    private class BeamHitRelay : MonoBehaviour
    {
        [HideInInspector] public LaserEmitter owner;
        void OnTriggerEnter(Collider other) => owner?.OnBeamTriggerTouched(other);
        void OnTriggerStay(Collider other) => owner?.OnBeamTriggerTouched(other);
    }

    public void SetLaser(LaserType t) { laser = t; UpdateAll(true); }

    // -------- NEW: HUM LOGIC --------
    void UpdateHumAudio(Vector3 a, Vector3 b)
    {
        EnsureBeamAudio();
        if (_beamAudio == null) return;

        // no audio in edit mode, avoid play-in-editor noise
        if (!Application.isPlaying) { if (_beamAudio.isPlaying) _beamAudio.Stop(); return; }

        // nothing to play?
        if (humLoop == null || humMaxDistance <= 0f)
        {
            if (_beamAudio.isPlaying) _beamAudio.Stop();
            return;
        }

        if (this.playerHead == null)
        {
            if (_beamAudio.isPlaying) _beamAudio.Stop();
            return;
        }

        // Closest point on beam segment to player head
        Vector3 ab = b - a;
        float abLenSq = Mathf.Max(1e-6f, ab.sqrMagnitude);
        float t = Vector3.Dot(this.playerHead.position - a, ab) / abLenSq;
        t = Mathf.Clamp01(t);
        Vector3 closest = a + ab * t;

        // Smooth follow
        if (humFollowSmoothing <= 0f) _beamAudioTf.position = closest;
        else _beamAudioTf.position = Vector3.Lerp(_beamAudioTf.position, closest, 1f - Mathf.Pow(1f - humFollowSmoothing, Application.isPlaying ? Time.deltaTime * 60f : 1f));

        // Our own distance-based loudness curve
        float d = Vector3.Distance(this.playerHead.position, closest);
        float k = Mathf.InverseLerp(humMaxDistance, humNearDistance, d); // 0 far, 1 near
        float vol = Mathf.Lerp(humBaseVolume, humNearVolume, k);
        _beamAudio.volume = vol;

        // ensure clip and play
        if (_beamAudio.clip != humLoop) _beamAudio.clip = humLoop;
        if (!_beamAudio.isPlaying) _beamAudio.Play();
    }
}