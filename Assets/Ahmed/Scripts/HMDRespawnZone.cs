using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HMDRespawnZone : MonoBehaviour
{
    void Reset()
    {
        // Make sure the collider is a trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        TryRespawn(other);
    }

    void OnTriggerStay(Collider other)
    {
        // In case of continuous contact (slow physics), still recover the HMD
        TryRespawn(other);
    }

    void TryRespawn(Component c)
    {
        // Find an HMD on this object or its parents
        var hmd = c.GetComponentInParent<HMD>();
        if (hmd == null) return;

        // Let the manager handle any bookkeeping, then reset the HMD
        if (HMDManagerLaser.Instance != null)
            HMDManagerLaser.Instance.RespawnHMD(hmd);
        else
            hmd.ResetToSpawn(); // fallback
    }
}
