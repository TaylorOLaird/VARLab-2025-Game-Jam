using UnityEngine;

public class WaterDrift : MonoBehaviour
{
    [SerializeField] private float driftSpeed = 0.01f;
    [SerializeField] private Vector3 driftDirection = new Vector3(1, 0, 0);
    private Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (WaterSceneState.IsWaterEnabled)
        {
            rb.MovePosition(rb.position + driftDirection * driftSpeed * Time.fixedDeltaTime);
        }
    }
}
