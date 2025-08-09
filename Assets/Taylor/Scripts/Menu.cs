using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Menu : MonoBehaviour
{
    [SerializeField] private InputActionReference openMenuButton;
    private bool isMenuOpen = false;

    [SerializeField] private GameObject menuUI;
    [SerializeField] private GameObject curve;
    
    [SerializeField] private List<GameObject> headsetImages;
    private int headsetIndex = 0;

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

    public void addHeadset()
    {
        if (headsetIndex < headsetImages.Count)
        {
            headsetImages[headsetIndex].SetActive(true);
            headsetIndex++;
        }
    }

    public void removeHeadset()
    {
        if (headsetIndex > 0)
        {
            headsetIndex--;
            headsetImages[headsetIndex].SetActive(false);
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
