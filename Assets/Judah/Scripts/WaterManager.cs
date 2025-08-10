using UnityEngine;
using UnityEngine.Rendering;

public class WaterManager : MonoBehaviour
{
    [SerializeField] private GameObject moveNormal;
    [SerializeField] private GameObject moveFlying;
    [SerializeField] private Volume volume;
    [SerializeField] private Color color;
    [SerializeField] private float fogDensity;

    private void Update()
    {
        RenderSettings.fogColor = color;
        RenderSettings.fogDensity = fogDensity;
    }
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

    public void DisableWater()
    {
        moveNormal.SetActive(true);
        moveFlying.SetActive(false);
        volume.enabled = false;
        RenderSettings.fog = false;
        WaterSceneState.IsWaterEnabled = false;
    }

    private void OnEnable()
    {
        EnableWater();
    }

    private void OnDisable()
    {
        DisableWater();
    }
}
