using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class DeathBarrier : MonoBehaviour
{
    [Tooltip("Tag on your XR Rig root (the object to teleport).")]
    public string playerTag = "Player";

    Rigidbody _rb;
    BoxCollider _col;

    void Reset()
    {
        _col = GetComponent<BoxCollider>();
        _col.isTrigger = true;

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
    }

    void OnTriggerEnter(Collider other) => TryKill(other);
    void OnTriggerStay(Collider other)  => TryKill(other);

    void TryKill(Collider other)
    {
        if (other == null) return;

        // 🔒 Only kill the player-tagged rig
        var root = other.transform.root;
        if (!root.CompareTag(playerTag)) return;

        var lm = LevelManager.Instance;
        if (lm != null && !lm.IsRespawning)
        {
            lm.KillPlayer(root); // LevelManager will re-validate this root too
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
        var bc = GetComponent<BoxCollider>();
        if (!bc) return;
        var m = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(bc.center, bc.size);
        Gizmos.matrix = m;
    }
}
