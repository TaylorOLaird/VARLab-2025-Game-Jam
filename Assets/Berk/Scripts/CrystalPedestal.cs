using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Collider))]
public class CrystalPedestal : MonoBehaviour
{
    [Header("Matching")]
    [Tooltip("If set, pedestal accepts crystals with this tag. If empty, expectedPersistentId is used.")]
    public string expectedTag;

    [Tooltip("If expectedTag is empty, you can set the persistent object's id here (GUID).")]
    public string expectedPersistentId;

    [Header("Placement")]
    [Tooltip("Optional transform where the crystal will be snapped/parented when placed.")]
    public Transform acceptPoint;

    [Tooltip("If true the pedestal will keep the crystal as a child (and then disable it).")]
    public bool parentBeforeDisable = true;

    [Header("Effects")]
    [Tooltip("Effect GameObject to enable when crystal placed (particles, lights).")]
    public GameObject effectObject;

    [Tooltip("Optional sound to play on accept (AudioSource on pedestal will be used).")]
    public AudioClip acceptSfx;

    [Header("Events")]
    public UnityEvent onAccepted;

    bool _accepted = false;

    // expose accepted state so managers can check
    public bool Accepted => _accepted;


    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        if (effectObject != null)
            effectObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_accepted) return;

        // find persistent object in parent's chain
        var p = other.GetComponentInParent<PersistentObject>();
        if (p == null) return;

        // quick match by tag or id
        bool matches = false;
        if (!string.IsNullOrEmpty(expectedTag))
        {
            // match via GameObject tag on the root/persistent object
            if (p.gameObject.CompareTag(expectedTag)) matches = true;
        }
        else if (!string.IsNullOrEmpty(expectedPersistentId))
        {
            if (p.id == expectedPersistentId) matches = true;
        }
        else
        {
            // no expectations set -> accept any persistent object
            matches = true;
        }

        if (!matches)
        {
            // feedback for wrong crystal (optional)
            StartCoroutine(RejectedPulse());
            return;
        }

        // find XRGrabInteractable if present so we can wait until release
        var grab = other.GetComponentInParent<XRGrabInteractable>();
        // start coroutine to wait until not selected then accept
        StartCoroutine(WaitForReleaseAndAccept(p.gameObject, grab));
    }

    IEnumerator WaitForReleaseAndAccept(GameObject go, XRGrabInteractable grab)
    {
        // if no grab or not selected, proceed immediately
        if (grab != null)
        {
            // wait until interactable is no longer selected (released)
            while (grab.isSelected)
                yield return null;

            // one extra frame to ensure physics settle
            yield return null;
        }

        // still inside trigger? (cheap check: distance to pedestal)
        if (!IsObjectCloseEnough(go))
        {
            // object moved away while still selected — ignore
            yield break;
        }

        Accept(go);
    }

    bool IsObjectCloseEnough(GameObject go)
    {
        if (go == null) return false;
        // check distance to acceptPoint or pedestal
        Vector3 targetPos = acceptPoint ? acceptPoint.position : transform.position;
        float sqrDist = (go.transform.position - targetPos).sqrMagnitude;
        return sqrDist < 2.5f * 2.5f; // within 2.5m by default; tweak if needed
    }

    void Accept(GameObject go)
    {
        if (_accepted || go == null) return;
        _accepted = true;

        // Move / parent the object to the acceptPoint or pedestal
        if (acceptPoint != null)
        {
            go.transform.position = acceptPoint.position;
            go.transform.rotation = acceptPoint.rotation;
            if (parentBeforeDisable)
                go.transform.SetParent(acceptPoint, worldPositionStays: false);
        }
        else
        {
            // parent to pedestal root
            if (parentBeforeDisable)
                go.transform.SetParent(transform, worldPositionStays: false);
            // snap to local origin
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
        }

        // // Ensure object is part of active (main) scene
        // var main = SceneManager.GetActiveScene();
        // if (go.scene != main && main.IsValid())
        //     SceneManager.MoveGameObjectToScene(go, main);

        // // Persist that this object is in the main scene and record its transform
        // PersistentSceneStateManager.MarkObjectInSceneAndSave(go, main.name);

        // disable the physical crystal so only effect remains
        go.SetActive(false);

        // enable/play effect object
        if (effectObject != null)
        {
            effectObject.SetActive(true);
            // play ParticleSystems if any
            var ps = effectObject.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var p in ps) p.Play(true);
        }

        // optional sfx
        if (acceptSfx != null)
        {
            var src = GetComponent<AudioSource>();
            if (src == null) src = gameObject.AddComponent<AudioSource>();
            src.PlayOneShot(acceptSfx);
        }

        // callback
        onAccepted?.Invoke();
        Debug.Log($"[CrystalPedestal] Accepted crystal '{go.name}' on pedestal '{name}'.");
    }

    IEnumerator RejectedPulse()
    {
        // brief visual feedback for wrong crystal: flash pedestal light (if present)
        var rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            var mat = rend.material;
            Color original = mat.HasProperty("_Color") ? mat.color : Color.white;
            float t = 0f;
            while (t < 0.4f)
            {
                float f = Mathf.PingPong(t * 5f, 1f);
                if (mat.HasProperty("_Color"))
                    mat.color = Color.Lerp(original, Color.red, f);
                t += Time.deltaTime;
                yield return null;
            }
            if (mat.HasProperty("_Color"))
                mat.color = original;
        }
    }

    // editor helper: reset pedestal to accept new crystal (call from inspector/button or PuzzleManager)
    public void ResetPedestal()
    {
        _accepted = false;
        if (effectObject != null) effectObject.SetActive(false);
    }
}
