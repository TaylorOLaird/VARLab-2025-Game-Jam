using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [SerializeField] float rows;

    [SerializeField] float tilesPerRow;

    [SerializeField] TextMeshProUGUI remainingSpacesText;

    [SerializeField] AudioSource winSound;

    public float maxTileCount;

    public float currentTileCount;

    [SerializeField] GameObject lockedDoor;
    // Start is called before the first frame update
    void Start()
    {
        maxTileCount = rows * tilesPerRow;
        remainingSpacesText.text = "Spaces Remaining \n" + maxTileCount;
    }

    // Update is called once per frame
    void Update()
    {
        // Win condition
        if (currentTileCount == maxTileCount)
        {
            lockedDoor.SetActive(false);
            winSound.Play();
            currentTileCount++;
        }
    }

    public void addTile()
    {
        ++currentTileCount;
        remainingSpacesText.text = "Spaces Remaining \n" + (maxTileCount - currentTileCount);
    }
    public void Win()
    {
        lockedDoor.SetActive(false);
        winSound.Play();
    }
}
