using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallActivateWhileMoving : MonoBehaviour
{
    BallBehavior ballScript;

    bool isMoving;
    void Start()
    {
        ballScript = transform.parent.GetComponent<BallBehavior>();
    }

    void Update()
    {
        isMoving = ballScript.getMoving();
    }
    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Wall") && isMoving)
        {
            ballScript.ballReset();
        }
    }

    // public void setMoving(bool isMoving)
    // {
    //     StartCoroutine(movingDelay(isMoving));
    // }
    // IEnumerator movingDelay(bool isMoving)
    // {
    //     yield return new WaitForSeconds(0.2f);
    //     this.isMoving = isMoving;
    // }
}
