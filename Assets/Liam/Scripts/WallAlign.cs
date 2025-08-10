using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallAlign : MonoBehaviour
{
    bool isAligned;
    // Start is called before the first frame update
    void Start()
    {
        isAligned = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isAligned)
        {
            if (other.CompareTag("Floor"))
            {
            transform.position = new Vector3(other.transform.position.x, transform.position.y, other.transform.position.z);
            isAligned = true;
            }
        }

    }
}
