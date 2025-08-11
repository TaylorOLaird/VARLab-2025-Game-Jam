using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraColider : MonoBehaviour
{
    // [SerializeField] private Collider targetCollider;
    [SerializeField] public List<Collider> targetColliders;
    public string currentColliderName;

    private void OnTriggerStay(Collider other)
    {
        if (targetColliders.Contains(other))
        {
            // GetComponent<Renderer>().material.color = Color.red;
            currentColliderName = other.name;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (targetColliders.Contains(other) && currentColliderName == other.name)
        {
            // GetComponent<Renderer>().material.color = Color.white;
            currentColliderName = string.Empty;
        }
    }
}
