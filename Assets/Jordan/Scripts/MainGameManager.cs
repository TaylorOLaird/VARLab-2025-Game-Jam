using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;

public class MainGameManager : MonoBehaviour
{
    public SceneSwitcher SceneSwitcher;
    public EndSwitcher EndSwitcher;
    public GameObject Headsets;
    public GameObject HiddenPuzzle;
    public GameObject Anchors;
    public GameObject FinishColliderSpeakers;
    public GameObject HiddenPuzzleSpeakers;
    public List<string> HeadsetNames;

    public List<string> board1 = new List<string> { "WaterHeadset", "BallHeadset", "DimensionHeadset" };
    public List<string> board2 = new List<string> { "LaserHeadset", "CameraHeadset", "BallHeadset" };
    public List<string> board3 = new List<string> { "DimensionHeadset", "WaterHeadset", "LaserHeadset" };

    public Image board1Panel;
    public Image board2Panel;
    public Image board3Panel;

    public bool board1Solved = false;
    public bool board2Solved = false;
    public bool board3Solved = false;

    public GameObject Door;

    private XROrigin xrOrigin;

    // Start is called before the first frame update
    void Start()
    {
        xrOrigin = FindObjectOfType<XROrigin>();
        EventManager.OnHeadsetDon += HandleHeadsetDon;
        EventManager.OnHeadsetDoff += HandleHeadsetDoff;

        if (Anchors != null && xrOrigin != null)
        {
            int roomIndex = EventManager.RoomNumber;
            if (roomIndex >= 0 && roomIndex < Anchors.transform.childCount)
            {
                Transform anchor = Anchors.transform.GetChild(roomIndex);
                xrOrigin.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
                FinishColliderSpeakers.transform.GetChild(roomIndex).gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("Room index out of range for Anchors children.");
            }
            for (int i = 0; i <= roomIndex; i++)
            {
                Headsets.transform.GetChild(i).gameObject.SetActive(true);
                if(roomIndex == 0) Headsets.transform.GetChild(i).gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning("Anchors or xrOrigin is not assigned.");
        }

    }

    void HandleHeadsetDon(HMD headset)
    {
        string headsetName = headset.gameObject.name;
        HeadsetNames.Add(headsetName);

        int board1index = 0;
        int board2index = 0;
        int board3index = 0;
        for (int i = 0; i < HeadsetNames.Count; i++)
        {
            if (board1[board1index].Equals(HeadsetNames[i]))
            {
                board1index++;
            }
            if (board2[board2index].Equals(HeadsetNames[i]))
            {
                board2index++;
            }
            if (board3[board3index].Equals(HeadsetNames[i]))
            {
                board3index++;
            }
        }
        if(board1index == board1.Count && !board1Solved)
        {
            board1Solved = true;
            board1Panel.color = Color.green;
        }
        if (board2index == board2.Count && !board2Solved)
        {
            board2Solved = true;
            board2Panel.color = Color.green;
        }
        if (board3index == board3.Count && !board3Solved)
        {
            board3Solved = true;
            board3Panel.color = Color.green;
        }
        if (board1Solved && board2Solved && board3Solved)
        {
            Door.SetActive(false);
            return;
        }
        if (headsetName.Equals("LastttHeadset"))
        {
            EndSwitcher.End();
        }
        if (headsetName.Equals("StartHeadset"))
        {
            if(EventManager.RoomNumber > 0)
            {
                if(HiddenPuzzle == null || HiddenPuzzleSpeakers == null)
                {
                    Debug.LogError("HiddenPuzzle or HiddenPuzzleSpeakers is not assigned in the inspector.");
                    return;
                }
                HiddenPuzzle.SetActive(true);
                int roomIndex = EventManager.RoomNumber - 1;
                HiddenPuzzleSpeakers.transform.GetChild(roomIndex).gameObject.SetActive(true);
            }
            else
            {
                FirstRoom();
            }
                
        }
        else if (headsetName.Equals("WaterHeadset"))
        {
            SceneSwitcher.SwitchScene("WaterScene");
        }
        else if (headsetName.Equals("LaserHeadset"))
        {
            SceneSwitcher.SwitchScene("AhmedTestScene");
        }
        else if(headsetName.Equals("CameraHeadset"))
        {
            SceneSwitcher.SwitchScene("Taylor");
        }
        else if (headsetName.Equals("BallHeadset"))
        {
            SceneSwitcher.SwitchScene("ChessScene");
        }
        else if (headsetName.Equals("DimensionHeadset"))
        {
            SceneSwitcher.SwitchScene("BerkMainScene");
        }
    }

    void HandleHeadsetDoff(HMD headset)
    {
        HeadsetNames.RemoveAt(HeadsetNames.Count - 1);
        if (headset.gameObject.name.Equals("StartHeadset"))
        {
            if (EventManager.RoomNumber > 0)
            {
                if (HiddenPuzzle == null || HiddenPuzzleSpeakers == null)
                {
                    Debug.LogError("HiddenPuzzle or HiddenPuzzleSpeakers is not assigned in the inspector.");
                    return;
                }
                HiddenPuzzle.SetActive(false);
            }
        }
    }

    private void OnDestroy()
    {
        EventManager.OnHeadsetDon -= HandleHeadsetDon;
        EventManager.OnHeadsetDoff -= HandleHeadsetDoff;
    }

    void FirstRoom()
    {
        SceneSwitcher.SwitchScene("FirstRoom");
    }
}
