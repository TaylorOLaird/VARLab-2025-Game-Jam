using UnityEngine;

public class SkyboxToggler : MonoBehaviour
{
    [SerializeField] private Material defaultSkybox; 
    Material savedSkybox;


    void Start()
    {
        savedSkybox = RenderSettings.skybox;
    }
    [ContextMenu("Disable Skybox")]
    public void DisableSkybox()
    {
        RenderSettings.skybox = defaultSkybox;
        DynamicGI.UpdateEnvironment();
    }
    [ContextMenu("Enable Skybox")]
    public void EnableSkybox()
    {
        RenderSettings.skybox = savedSkybox;

        DynamicGI.UpdateEnvironment();

        foreach (var probe in FindObjectsOfType<ReflectionProbe>())
            if (probe.mode == UnityEngine.Rendering.ReflectionProbeMode.Realtime)
                probe.RenderProbe();
    }
}
