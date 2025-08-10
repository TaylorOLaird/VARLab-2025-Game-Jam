using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderManager : MonoBehaviour
{
    bool isMoving;

    LayerMask layerMask;

    [SerializeField] GameObject frontCollider;
    [SerializeField] GameObject backCollider;
    [SerializeField] GameObject rightCollider;
    [SerializeField] GameObject leftCollider;

    [Range(0f,1f)]
    [SerializeField] float range = 0.6f;


    // Start is called before the first frame update
    void Start()
    {
        layerMask = LayerMask.GetMask("Wall");
    }

    // Update is called once per frame
    void Update()
    {
        if (!isMoving)
        {
            RaycastHit hit;

            // If you want to adjust the range value do it here
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * range, Color.yellow);
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.back) * range, Color.yellow);
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.right) * range, Color.yellow);
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.left) * range, Color.yellow);

            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, range, layerMask))
            {
                frontCollider.SetActive(false);
            }
            else
            {
                frontCollider.SetActive(true);
            }

            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.back), out hit, range, layerMask))
            {
                backCollider.SetActive(false);
            }
            else
            {
                backCollider.SetActive(true);
            }

            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.right), out hit, range, layerMask))
            {
                rightCollider.SetActive(false);
            }
            else
            {
                rightCollider.SetActive(true);
            }

            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.left), out hit, range, layerMask))
            {
                leftCollider.SetActive(false);
            }
            else
            {
                leftCollider.SetActive(true);
            }

            // if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, 2f, layerMask))
            // {
            // }
        }
    }
}
