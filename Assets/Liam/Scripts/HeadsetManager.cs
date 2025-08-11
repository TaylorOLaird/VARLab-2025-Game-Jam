using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class HeadsetManager : MonoBehaviour
{
    [SerializeField] SceneSwitcher sceneSwitcher;
    [SerializeField] private Volume redVolume;

    [SerializeField] private Volume blueVolume;

    [SerializeField] private Volume greenVolume;

    [SerializeField] GameObject redWalls;

    [SerializeField] GameObject blueWalls;

    [SerializeField] GameObject greenWalls;

    [SerializeField] GameObject redWallsTransparent;

    [SerializeField] GameObject blueWallsTransparent;

    [SerializeField] GameObject greenWallsTransparent;

    [SerializeField] GameObject allTransparentCollisions;


    List<int> headsetList;

    void Start()
    {
        EventManager.OnHeadsetDon += addHeadset;
        EventManager.OnHeadsetDoff += removeHeadset;
        headsetList = new List<int>();
        headsetList.Add(0);
    }

    void Update()
    {
        currentEffect();
    }

    public void addHeadset(HMD headset)
    {
        if (headset.gameObject.name == "HMD RED")
        {
            headsetList.Add(1);
        }
        else if (headset.gameObject.name == "HMD BLUE")
        {
            headsetList.Add(2);
        }
        else if (headset.gameObject.name == "HMD GREEN")
        {
            headsetList.Add(3);
        }
        else if (headset.gameObject.name == "HMD BLACK")
        {
            headsetList.Add(4);
        }
        else if (headset.gameObject.name == "HMD GOLD")
        {
            EventManager.RoomNumber = 4;
            sceneSwitcher.SwitchScene("MainScene");
        }
    }

    public void removeHeadset(HMD headset)
    {
        if (headset.gameObject.name == "HMD RED")
        {
            headsetList.Remove(1);
        }
        else if (headset.gameObject.name == "HMD BLUE")
        {
            headsetList.Remove(2);
        }
        else if (headset.gameObject.name == "HMD GREEN")
        {
            headsetList.Remove(3);
        }
        else if (headset.gameObject.name == "HMD BLACK")
        {
            headsetList.Remove(4);
        }
        else if (headset.gameObject.name == "HMD GOLD")
        {
            // Nothing
        }
    }

    void currentEffect()
    {
        if (headsetList[headsetList.Count - 1] == 0)
        {
            redWalls.SetActive(true);
            blueWalls.SetActive(true);
            greenWalls.SetActive(true);
            redWallsTransparent.SetActive(false);
            blueWallsTransparent.SetActive(false);
            greenWallsTransparent.SetActive(false);
            allTransparentCollisions.SetActive(false);
            redVolume.weight = 0;
            blueVolume.weight = 0;
            greenVolume.weight = 0;
        }
        else if (headsetList[headsetList.Count - 1] == 1)
        {
            redWalls.SetActive(false);
            blueWalls.SetActive(true);
            greenWalls.SetActive(true);
            redWallsTransparent.SetActive(true);
            blueWallsTransparent.SetActive(false);
            greenWallsTransparent.SetActive(false);
            allTransparentCollisions.SetActive(false);
            redVolume.weight = 1;
            blueVolume.weight = 0;
            greenVolume.weight = 0;
        }
        else if (headsetList[headsetList.Count - 1] == 2)
        {
            redWalls.SetActive(true);
            blueWalls.SetActive(false);
            greenWalls.SetActive(true);
            redWallsTransparent.SetActive(false);
            blueWallsTransparent.SetActive(true);
            greenWallsTransparent.SetActive(false);
            allTransparentCollisions.SetActive(false);
            redVolume.weight = 0;
            blueVolume.weight = 1;
            greenVolume.weight = 0;
        }
        else if (headsetList[headsetList.Count - 1] == 3)
        {
            redWalls.SetActive(true);
            blueWalls.SetActive(true);
            greenWalls.SetActive(false);
            redWallsTransparent.SetActive(false);
            blueWallsTransparent.SetActive(false);
            greenWallsTransparent.SetActive(true);
            allTransparentCollisions.SetActive(false);
            redVolume.weight = 0;
            blueVolume.weight = 0;
            greenVolume.weight = 1;
        }
        else if (headsetList[headsetList.Count - 1] == 4)
        {
            redWalls.SetActive(false);
            blueWalls.SetActive(false);
            greenWalls.SetActive(false);
            redWallsTransparent.SetActive(false);
            blueWallsTransparent.SetActive(false);
            greenWallsTransparent.SetActive(false);
            allTransparentCollisions.SetActive(true);
            redVolume.weight = 0;
            blueVolume.weight = 0;
            greenVolume.weight = 0;
        }
    }
}
