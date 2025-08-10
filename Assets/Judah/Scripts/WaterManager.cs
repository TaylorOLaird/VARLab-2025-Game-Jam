using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class WaterManager : MonoBehaviour
{
    [SerializeField] private GameObject moveNormal;
    [SerializeField] private GameObject moveFlying;
    [SerializeField] private Volume volume;
    [SerializeField] private Color color;
    [SerializeField] private float fogDensity;
    [SerializeField] private GameObject currents;
    private List<Transform> currentPivots;

    private void Start()
    {
        EventManager.OnHeadsetDon += HandleHeadsetDon;
        EventManager.OnHeadsetDoff += HandleHeadsetDoff;

        currentPivots = new List<Transform>();
        foreach (Transform child in currents.transform)
        {
            currentPivots.Add(child);
        }
    }
    private void Update()
    {
        RenderSettings.fogColor = color;
        RenderSettings.fogDensity = fogDensity;
    }
    [ContextMenu("EnableWater")]
    public void EnableWater()
    {
        moveNormal.SetActive(false);
        moveFlying.SetActive(true);
        volume.enabled = true;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = color;
        RenderSettings.fogDensity = fogDensity;
        WaterSceneState.IsWaterEnabled = true;
    }
    [ContextMenu("DisableWater")]
    public void DisableWater()
    {
        moveNormal.SetActive(true);
        moveFlying.SetActive(false);
        volume.enabled = false;
        RenderSettings.fog = false;
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
