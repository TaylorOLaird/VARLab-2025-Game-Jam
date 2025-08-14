using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuLaser : MonoBehaviour
{
    [SerializeField] private InputActionReference openMenuButton;

    private bool isMenuOpen = false;

    [SerializeField] private GameObject menuUI;
    [SerializeField] private GameObject curve;

    [SerializeField] private List<GameObject> headsetImages;
    private int headsetIndex = 0;

    [SerializeField] private HMD testHMD;

    void Start()
    {
        if (menuUI == null)
        {
            Debug.LogError("Menu UI is not assigned in the inspector.");
        }
        else
        {
            menuUI.SetActive(isMenuOpen);
            curve.SetActive(isMenuOpen);
        }

        for (int i = 0; i < headsetImages.Count; i++)
        {
            headsetImages[i].SetActive(false);
        }

        openMenuButton.action.Enable();
        openMenuButton.action.performed += toggleMenu;
        EventManager.OnHeadsetDon += addHeadset;
        EventManager.OnHeadsetDoff += removeHeadset;
    }

    void Update()
    {

    }

    public void addHeadset(HMD headset)
    {
        if (headsetIndex < headsetImages.Count)
        {
            if (headsetImages[headsetIndex] == null)
            {
                Debug.LogError("Headset image at index " + headsetIndex + " is not assigned.");
                return;
            }
            headsetImages[headsetIndex].SetActive(true);
            HMD hmdScript = headset.GetComponent<HMD>();
            headsetImages[headsetIndex].GetComponent<SpriteRenderer>().sprite = hmdScript.headsetSprite;
            headsetIndex++;
        }
        else
        {
            Debug.Log("Maximum number of headsets reached.");
        }

    }

    public void removeHeadset(HMD headset)
    {
        if (headsetIndex > 0)
        {
            if (headsetImages[headsetIndex] == null)
            {
                Debug.LogError("Headset image at index " + headsetIndex + " is not assigned.");
                return;
            }
            headsetIndex--;
            headsetImages[headsetIndex].SetActive(false);
        }
        else
        {
            Debug.Log("No headsets to remove.");
        }
    }

    private void toggleMenu(InputAction.CallbackContext context)
    {
        isMenuOpen = !isMenuOpen;
        if (isMenuOpen)
        {
            if (!menuUI) return;
            menuUI.SetActive(true);
            curve.SetActive(true);
        }
        else
        {
            menuUI.SetActive(false);
            curve.SetActive(false);
        }
    }
    
    public void ClearHeadsetUI()
{
    // hide all icons and reset index
    for (int i = 0; i < headsetImages.Count; i++)
        if (headsetImages[i]) headsetImages[i].SetActive(false);
    headsetIndex = 0;
}
}
