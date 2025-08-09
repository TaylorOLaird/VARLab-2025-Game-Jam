using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BallBehavior : MonoBehaviour
{
    Rigidbody ballRigidbody;

    Vector3 currentPosition;

    [Range(0f, 5f)]
    [SerializeField] float maxMoveSpeed;

    // Change back to private when done debugging
    [SerializeField] float moveDirection;

    bool isMoving;
    // Start is called before the first frame update
    void Start()
    {
        ballRigidbody = GetComponent<Rigidbody>();
        currentPosition = transform.position;

    }

    // Update is called once per frame
    void Update()
    {
        currentPosition = transform.position;
    }

    void FixedUpdate()
    {
        // Move direction 0 is to stay still
        
        // Forward
        if (moveDirection == 1)
        {
            Vector3 moveTowards = new Vector3(currentPosition.x - (maxMoveSpeed / 10), currentPosition.y, currentPosition.z);
            ballRigidbody.MovePosition(moveTowards);
        }
        // Backwards
        else if (moveDirection == 2)
        {
            Vector3 moveTowards = new Vector3(currentPosition.x + (maxMoveSpeed / 10), currentPosition.y, currentPosition.z);
            ballRigidbody.MovePosition(moveTowards);
        }
        // Right
        else if (moveDirection == 3)
        {
            Vector3 moveTowards = new Vector3(currentPosition.x, currentPosition.y, currentPosition.z + (maxMoveSpeed / 10));
            ballRigidbody.MovePosition(moveTowards);
        }
        // Left
        else if (moveDirection == 4)
        {
            Vector3 moveTowards = new Vector3(currentPosition.x, currentPosition.y, currentPosition.z - (maxMoveSpeed / 10));
            ballRigidbody.MovePosition(moveTowards);
        }
    }

    public void moveForward()
    {
        if (!isMoving)
        {
            moveDirection = 1;
            isMoving = true;
        }
    }
    public void moveBackwards()
    {
        if (!isMoving)
        {
            moveDirection = 2;
            isMoving = true;
        }
    }

    public void moveRight()
    {
        if (!isMoving)
        {
            moveDirection = 3;
            isMoving = true;
        }
    }

    public void moveLeft()
    {
        if (!isMoving)
        {
            moveDirection = 4;
            isMoving = true;
        }
    }


    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Wall"))
        {
            moveDirection = 0;
            isMoving = false; 
        }
    }
}
