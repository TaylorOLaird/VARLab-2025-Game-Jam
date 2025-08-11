using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class WaterManager : MonoBehaviour
{
    public int goals;
    [SerializeField] private GameObject EndHeadset;
    [SerializeField] private Volume volume;
    [SerializeField] private GameObject currents;
    [SerializeField] private InputActionReference reset;
    [SerializeField] private GameObject movableCube;
    [SerializeField] private SceneSwitcher sceneSwitcher;
    [SerializeField] private SkyboxToggler skyboxToggler;
    private List<Transform> currentPivots;
    private Vector3 initialPos;

    private void Start()
    {
        EventManager.OnHeadsetDon += HandleHeadsetDon;
        EventManager.OnHeadsetDoff += HandleHeadsetDoff;
        reset.action.performed += OnButtonPressed;

        currentPivots = new List<Transform>();
        foreach (Transform child in currents.transform)
        {
            currentPivots.Add(child);
        }

        initialPos = movableCube.transform.position;
    }

    private void OnButtonPressed(InputAction.CallbackContext context)
    {
        movableCube.transform.position = initialPos;
    }

    [ContextMenu("EnableWater")]
    public void EnableWater()
    {
        skyboxToggler.DisableSkybox();
        volume.enabled = true;
        WaterSceneState.IsWaterEnabled = true;
    }

    [ContextMenu("DisableWater")]
    public void DisableWater()
    {
        skyboxToggler.EnableSkybox();
        volume.enabled = false;
        WaterSceneState.IsWaterEnabled = false;
    }

    [ContextMenu("ReverseCurrents")]
    public void ReverseCurrents()
    {
        foreach (Transform pivot in currentPivots)
        {    
            pivot.localEulerAngles = new Vector3(0f, 180f, 0f);     
        }
    }

    [ContextMenu("RestoreCurrents")]
    public void RestoreCurrents()
    {
        foreach (Transform pivot in currentPivots)
        {
            pivot.localEulerAngles = new Vector3(0f, 0f, 0f);
        }
    }

    public void EndPuzzle()
    {
        EndHeadset.SetActive(true);
    }

    public void HandleHeadsetDon(HMD headset)
    {
        if (headset.gameObject.name == "HMDWater")
        {
            EnableWater();
        }
        else if (headset.gameObject.name == "HMDCurrentFlow")
        {
            ReverseCurrents();
        }
        if (headset.gameObject.name == "HMDEnd")
        {
            EventManager.RoomNumber = 3;
            sceneSwitcher.SwitchScene("MainScene");
        }
    }

    public void HandleHeadsetDoff(HMD headset)
    {
        if (headset.gameObject.name == "HMDWater")
        {
            DisableWater();
        }
        else if (headset.gameObject.name == "HMDCurrentFlow")
        {
            RestoreCurrents();
        }
    }
}
