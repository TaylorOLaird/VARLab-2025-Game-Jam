using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PushCollider : MonoBehaviour
{
    [SerializeField]Vector3 position;

    BallBehavior ballScript;
    float pushDirection;
    // Start is called before the first frame update
    void Start()
    {
        position = transform.localPosition;

        ballScript = transform.parent.parent.GetComponent<BallBehavior>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("GameController"))
        {
            // Front collider - Ball goes backwards
            if (position.x == 0 && position.z > 0)
            {
                // Debug.Log("FrontCollider");
                ballScript.moveBackward();
            }
            // Back collider - Ball goes forwards
            else if (position.x == 0 && position.z < 0)
            {
                // Debug.Log("BackCollider");
                ballScript.moveForward();
            }
            // Right collider - Ball goes left
            else if (position.x > 0 && position.z == 0)
            {
                // Debug.Log("RightCollider");
                ballScript.moveLeft();
            }
            // Left collider - Ball goes right
            else if (position.x < 0 && position.z == 0)
            {
                // Debug.Log("LeftCollider");
                ballScript.moveRight();
            }
        }
    }
}
