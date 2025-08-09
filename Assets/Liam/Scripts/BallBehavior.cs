using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class BallBehavior : MonoBehaviour
{
    [SerializeField]BoardManager manager;

    TileCheck tileCheck;

    Rigidbody ballRigidbody;

    Vector3 currentPosition;

    Vector3 restPosition;

    [Range(0f, 1.5f)]
    [SerializeField] float maxMoveSpeed;

    // Change back to private when done debugging
    [SerializeField] float moveDirection;

    bool isMoving;

    bool isInitialized;
    // Start is called before the first frame update
    void Start()
    {
        ballRigidbody = GetComponent<Rigidbody>();
        currentPosition = transform.position;
        isInitialized = false;

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
            float movementSpeed = -maxMoveSpeed / 10;
            moveBall(movementSpeed, 0);
        }
        // Backwards
        else if (moveDirection == 2)
        {
            float movementSpeed = maxMoveSpeed / 10;
            moveBall(movementSpeed, 0);
        }
        // Right
        else if (moveDirection == 3)
        {
            float movementSpeed = maxMoveSpeed / 10;
            moveBall(0, movementSpeed);
        }
        // Left
        else if (moveDirection == 4)
        {
            float movementSpeed = -maxMoveSpeed / 10;
            moveBall(0, movementSpeed);
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
            transform.position = restPosition;
            isMoving = false;
        }
        if (other.CompareTag("Floor"))
        {
            restPosition = new Vector3(other.transform.position.x, transform.position.y, other.transform.position.z);
            if (!isInitialized)
            {
                transform.position = restPosition;
                isInitialized = true;
            }
            tileCheck = other.GetComponent<TileCheck>();

            if (tileCheck.getTileActive() == false)
            {
                manager.addTile();
                tileCheck.setTileActive();
            }
        }
    }

    void moveBall(float movementSpeedX, float movementSpeedZ)
    {
        Vector3 moveTowards = new Vector3(currentPosition.x + movementSpeedX, currentPosition.y, currentPosition.z + movementSpeedZ);
        ballRigidbody.MovePosition(moveTowards);
    }
}
