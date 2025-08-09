using UnityEngine;

public class WaterManager : MonoBehaviour
{

    [SerializeField] private GameObject waterPlane;   

    public void EnableWater()
    {
        waterPlane.SetActive(true);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.0f, 0.4f, 0.7f);
        RenderSettings.fogDensity = 0.05f;
        WaterSceneState.IsWaterEnabled = true;
    }

    public void DisableWater()
    {
        waterPlane.SetActive(false);
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
