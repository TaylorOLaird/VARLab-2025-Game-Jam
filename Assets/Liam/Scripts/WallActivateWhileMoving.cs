using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallActivateWhileMoving : MonoBehaviour
{
    BallBehavior ballScript;
    void Start()
    {
        ballScript = transform.parent.GetComponent<BallBehavior>();
    }
    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Wall"))
        {
            ballScript.ballReset();
        }
    }
}
