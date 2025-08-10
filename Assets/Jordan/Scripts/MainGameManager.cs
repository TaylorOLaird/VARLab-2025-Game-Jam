using UnityEngine;

public class MainGameManager : MonoBehaviour
{
    public SceneSwitcher SceneSwitcher;
    
    // Start is called before the first frame update
    void Start()
    {
        EventManager.OnHeadsetDon += HandleHeadsetDon;
    }

    void HandleHeadsetDon(HMD headset)
    {
        if(headset.gameObject.name.Equals("StartHeadset"))
        {
            StartHeadset();
        }
    }

    void StartHeadset()
    {
        SceneSwitcher.SwitchScene("FirstRoom");
    }
}
