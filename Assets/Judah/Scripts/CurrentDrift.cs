using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CurrentDrift : MonoBehaviour
{
    [SerializeField] private Collider contactCollider; 
    [SerializeField] private float driftSpeed = 0.5f;  
    [SerializeField] private Vector3 driftDirection = new Vector3(1, 0, 0); 

    private Rigidbody rb;
    private bool isDrifting;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void FixedUpdate()
    {
        if (isDrifting && contactCollider != null)
        {
            rb.MovePosition(rb.position + driftDirection.normalized * driftSpeed * Time.fixedDeltaTime);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other == contactCollider)
            isDrifting = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other == contactCollider)
            isDrifting = false;
    }
}