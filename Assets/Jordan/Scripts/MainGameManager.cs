using Unity.XR.CoreUtils;
using UnityEngine;

public class MainGameManager : MonoBehaviour
{
    public SceneSwitcher SceneSwitcher;
    public GameObject StartHeadset;
    public GameObject HiddenPuzzle;
    public GameObject Anchors;
    public GameObject FinishColliderSpeakers;
    public GameObject HiddenPuzzleSpeakers;

    private XROrigin xrOrigin;

    // Start is called before the first frame update
    void Start()
    {
        xrOrigin = FindObjectOfType<XROrigin>();
        EventManager.OnHeadsetDon += HandleHeadsetDon;
        EventManager.OnHeadsetDoff += HandleHeadsetDoff;

        StartHeadset.SetActive(EventManager.RoomNumber > 0);
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
        }
        else
        {
            Debug.LogWarning("Anchors or xrOrigin is not assigned.");
        }

    }

    void HandleHeadsetDon(HMD headset)
    {
        if(headset.gameObject.name.Equals("StartHeadset"))
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
    }

    void HandleHeadsetDoff(HMD headset)
    {
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

    void FirstRoom()
    {
        SceneSwitcher.SwitchScene("FirstRoom");
    }
}
