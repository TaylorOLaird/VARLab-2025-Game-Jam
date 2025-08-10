using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileCheck : MonoBehaviour
{
    bool tileActive;
    // Start is called before the first frame update
    void Start()
    {
        tileActive = false;
    }

    public void setTileActive()
    {
        GetComponent<Renderer>().material.color = Color.blue;
        tileActive = true;
    }
    public bool getTileActive()
    {
        return tileActive;  
    }
}
