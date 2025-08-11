using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorManager : MonoBehaviour
{
    [SerializeField] private GameObject openDoorParent;
    [SerializeField] private GameObject closedDoorParent;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        GameObject testTriggerBlock2 = GameObject.Find("Broken Door");
        if (testTriggerBlock2 != null)
        {
            openDoorParent.SetActive(true);
            closedDoorParent.SetActive(false);
        }
        else
        {
            openDoorParent.SetActive(false);
            closedDoorParent.SetActive(true);
        }
    }
}
