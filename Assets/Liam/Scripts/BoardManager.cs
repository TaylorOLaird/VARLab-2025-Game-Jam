using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [SerializeField] float rows;

    [SerializeField] float tilesPerRow;

    public float maxTileCount;

    public float currentTileCount;

    [SerializeField] GameObject lockedDoor;
    // Start is called before the first frame update
    void Start()
    {
        maxTileCount = rows * tilesPerRow;
    }

    // Update is called once per frame
    void Update()
    {
        // Win condition
        if (currentTileCount == maxTileCount)
        {
            lockedDoor.SetActive(false);
            currentTileCount++;
        }
    }

    public void addTile()
    {
        ++currentTileCount;
    }
}
