using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WinFreeButton : MonoBehaviour
{
    [SerializeField] BoardManager manager;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("GameController"))
        {
            manager.Win();
        }
    }
}
