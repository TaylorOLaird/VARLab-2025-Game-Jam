using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Menu : MonoBehaviour
{
    [SerializeField] private InputActionReference openMenuButton;
    // [SerializeField] private InputActionReference testDONButton;
    // [SerializeField] private InputActionReference testDOFFButton;
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
        // testDONButton.action.Enable();
        // testDONButton.action.performed += tmpAddHeadset;
        // testDOFFButton.action.Enable();
        // testDOFFButton.action.performed += tmpRemoveHeadset;
        EventManager.OnHeadsetDon += addHeadset;
        EventManager.OnHeadsetDoff += removeHeadset;
    }

    void Update()
    {

    }

    // public void tmpAddHeadset(InputAction.CallbackContext context)
    // {
    //     addHeadset(testHMD);
    // }
    // public void tmpRemoveHeadset(InputAction.CallbackContext context)
    // {
    //     removeHeadset(testHMD);
    // }

    public void addHeadset(HMD headset)
    {
        if (headsetIndex < headsetImages.Count)
        {
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
            menuUI.SetActive(true);
            curve.SetActive(true);
        }
        else
        {
            menuUI.SetActive(false);
            curve.SetActive(false);
        }
    }
}
