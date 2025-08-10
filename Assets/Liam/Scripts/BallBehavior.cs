using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using static UnityEngine.ParticleSystem;

public class BallBehavior : MonoBehaviour
{
    [SerializeField] BoardManager manager;

    [SerializeField] GameObject meshObject;

    [SerializeField] GameObject trail;
    [SerializeField] ParticleSystem particleTrail1;
    [SerializeField] ParticleSystem particleTrail2;

    EmissionModule emissionTrail1;

    EmissionModule emissionTrail2;

    Animator animator;

    TileCheck tileCheck;

    Rigidbody ballRigidbody;

    Vector3 currentPosition;

    Vector3 restPosition;

    Vector3 startPosition;

    [Range(0f, 1.5f)]
    [SerializeField] float maxMoveSpeed;

    [Range(0f, 5f)]
    [SerializeField] float maxRotateSpeed;

    // Change back to private when done debugging
    [SerializeField] float moveDirection;

    float emissionCount;

    string jumpBoolString;

    bool isMoving;

    bool isInitialized;
    // Start is called before the first frame update
    void Start()
    {
        ballRigidbody = GetComponent<Rigidbody>();
        animator = meshObject.GetComponent<Animator>();
        currentPosition = transform.position;
        isInitialized = false;


        emissionTrail1 = particleTrail1.emission;
        emissionTrail2 = particleTrail2.emission;

        emissionCount = emissionTrail1.rateOverTime.constant;

        emissionTrail1.rateOverTime = 0;
        emissionTrail2.rateOverTime = 0;

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
            jumpBoolString = "JumpFront"; // temp
            trail.transform.rotation = Quaternion.Euler(0f, 0f, 0f); // temp
            emissionTrail1.rateOverTime = emissionCount; // temp
            emissionTrail2.rateOverTime = emissionCount; // temp
            isMoving = true; // temp

            float movementSpeed = -maxMoveSpeed / 10;
            float rotationSpeed = maxRotateSpeed * 10;

            moveBall(movementSpeed, 0);
            rotateBall(0f, rotationSpeed);
        }
        // Backwards
        else if (moveDirection == 2)
        {
            jumpBoolString = "JumpBack"; // temp
            trail.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // temp
            emissionTrail1.rateOverTime = emissionCount; // temp
            emissionTrail2.rateOverTime = emissionCount; // temp
            isMoving = true; // temp

            float movementSpeed = maxMoveSpeed / 10;
            float rotationSpeed = -maxRotateSpeed * 10;

            moveBall(movementSpeed, 0);
            rotateBall(0f, rotationSpeed);
        }
        // Right
        else if (moveDirection == 3)
        {
            jumpBoolString = "JumpRight"; // temp
            trail.transform.rotation = Quaternion.Euler(0f, 270f, 0f); // temp
            emissionTrail1.rateOverTime = emissionCount; // temp
            emissionTrail2.rateOverTime = emissionCount; // temp
            isMoving = true; // temp

            float movementSpeed = maxMoveSpeed / 10;
            float rotationSpeed = maxRotateSpeed * 10;

            moveBall(0, movementSpeed);
            rotateBall(rotationSpeed, 0f);
        }
        // Left
        else if (moveDirection == 4)
        {
            jumpBoolString = "JumpLeft"; // temp
            trail.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // temp
            emissionTrail1.rateOverTime = emissionCount; // temp
            emissionTrail2.rateOverTime = emissionCount; // temp
            isMoving = true; // temp

            float movementSpeed = -maxMoveSpeed / 10;
            float rotationSpeed = -maxRotateSpeed * 10;

            moveBall(0, movementSpeed);
            rotateBall(rotationSpeed, 0f);
        }
    }

    public void moveForward()
    {
        if (!isMoving)
        {
            movement(1, 0f, "JumpFront");
        }
    }
    public void moveBackward()
    {
        if (!isMoving)
        {
            movement(2, 180f, "JumpBack");
        }
    }

    public void moveRight()
    {
        if (!isMoving)
        {
            movement(3, 90f, "JumpRight");
        }
    }

    public void moveLeft()
    {
        if (!isMoving)
        {
            movement(4, 270f, "JumpLeft");
        }
    }

    void movement(int moveDirection, float rotation, string jumpBoolString)
    {
        this.moveDirection = moveDirection;

        trail.transform.rotation = Quaternion.Euler(0f, rotation, 0f);

        this.jumpBoolString = jumpBoolString;

        emissionTrail1.rateOverTime = emissionCount;
        emissionTrail2.rateOverTime = emissionCount;

        isMoving = true;
    }


    // Used for the edge case where a wall is activated on top of the ball while it is moving
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Wall"))
        {
            moveDirection = 0;

            transform.position = restPosition;
            if (isMoving)
            {
                animator.SetTrigger(jumpBoolString);
            }

            meshObject.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);

            emissionTrail1.rateOverTime = 0;
            emissionTrail2.rateOverTime = 0;

            isMoving = false;
        }
        if (other.CompareTag("Floor"))
        {
            restPosition = new Vector3(other.transform.position.x, transform.position.y, other.transform.position.z);
            if (!isInitialized)
            {
                transform.position = restPosition;
                startPosition = restPosition;
                isInitialized = true;
            }
            tileCheck = other.GetComponentInParent<TileCheck>();

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
    void rotateBall(float maxRotateSpeedX, float maxRotateSpeedZ)
    {
        Vector3 rotation = new Vector3(maxRotateSpeedX, 0f, maxRotateSpeedZ);
        meshObject.transform.Rotate(rotation);
    }

    public void ballReset()
    {
        ballRigidbody.MovePosition(startPosition);
    }
    
}
