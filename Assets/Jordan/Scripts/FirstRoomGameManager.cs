using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

public class FirstRoomGameManager : MonoBehaviour
{
    public GameObject Dimension0;
    public GameObject Dimension1;
    public GameObject Bars;
    private XROrigin xrOrigin;
    public GameObject DoffHitbox;
    public SceneSwitcher sceneSwitcher;

    private Vector3 savedPosition;


    // Start is called before the first frame update
    void Start()
    {
        xrOrigin = FindObjectOfType<XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError("No XROrigin found in scene for TeleportPlayer.");
        }
        EventManager.OnHeadsetDon += HandleHeadsetDon;
        EventManager.OnHeadsetDoff += HandleHeadsetDoff;
    }

    private void HandleHeadsetDon(HMD headset)
    {
        if(headset.gameObject.name.Equals("Headset1"))
        {
            // Save XR origin position and rotation
            if (xrOrigin != null)
            {
                savedPosition = xrOrigin.transform.position;
            }

            Dimension1.SetActive(true);
            Dimension0.SetActive(false);
        }
        else if (headset.gameObject.name.Equals("EndHeadset"))
        {
            EventManager.RoomNumber = 1;
            sceneSwitcher.SwitchScene("MainScene");
        }
    }

    private void HandleHeadsetDoff(HMD headset)
    {
        if (headset.gameObject.name.Equals("Headset1"))
        {
            xrOrigin.transform.position = savedPosition;
            DoffHitbox.transform.position = Vector3.zero;
            Dimension1.SetActive(false);
            Dimension0.SetActive(true);
        }
    }

    public void ButtonPressed()
    {
        Bars.transform.position = new Vector3(0, 2.29f, 0);
    }
}
