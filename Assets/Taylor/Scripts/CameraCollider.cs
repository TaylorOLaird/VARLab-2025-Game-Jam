using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraColider : MonoBehaviour
{
    [SerializeField] private Collider targetCollider;
    public string currentColliderName;

    private void OnTriggerStay(Collider other)
    {
        if (other == targetCollider)
        {
            // GetComponent<Renderer>().material.color = Color.red;
            currentColliderName = other.name;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == targetCollider)
        {
            // GetComponent<Renderer>().material.color = Color.white;
            currentColliderName = string.Empty;
        }
    }
}
