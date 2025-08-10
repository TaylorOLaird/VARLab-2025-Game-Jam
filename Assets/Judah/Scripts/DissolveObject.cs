using UnityEngine;

public class DissolveObject : MonoBehaviour
{
    private float dissolveSpeed = 0.5f;
    private float dissolveValue = 0f;
    private bool dissolving = false;
    private bool rematerializing = false;
    private Material mat;

    private void Start()
    {
        mat = GetComponent<Renderer>().material;   
    }

    private void OnEnable()
    {
        WaterSceneState.OnWaterEnabledChanged += HandleWaterChange;
    }

    private void OnDisable()
    {
        WaterSceneState.OnWaterEnabledChanged += HandleWaterChange;
    }
    void Update()
    {
        if (dissolving)
        {
            dissolveValue += Time.deltaTime * dissolveSpeed;
            dissolveValue = Mathf.Clamp01(dissolveValue);
            mat.SetFloat("_Dissolve", dissolveValue);
            if (dissolveValue == 1)
            {
                gameObject.SetActive(false);
                dissolving = false;
            }
        }
        else if (rematerializing)
        {
            dissolveValue -= Time.deltaTime * dissolveSpeed;
            dissolveValue = Mathf.Clamp01(dissolveValue); 
            mat.SetFloat("_Dissolve", dissolveValue);
            if (dissolveValue == 0)
            {
                rematerializing = false;
            }
        } 
    }

    public void StartDissolve()
    {
        rematerializing = false;
        dissolving = true;
    }

    public void StartRematerialize()
    {
        dissolving = false;
        gameObject.SetActive(true);
        rematerializing = true;
    }

    private void HandleWaterChange(bool isEnabled)
    {
        if (isEnabled)
        {
            StartDissolve();
        }
        else 
        {
            StartRematerialize();
        }
    }
}
