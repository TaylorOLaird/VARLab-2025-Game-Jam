using UnityEngine;

public class ToggleGameObject : MonoBehaviour
{
    public void ToggleObject()
    {
        gameObject.SetActive(!gameObject.activeInHierarchy);
    }

    public void HideObject()
    {
        gameObject.SetActive(false);
    }

    public void ShowObject()
    {
       gameObject.SetActive(true);
    }
}
