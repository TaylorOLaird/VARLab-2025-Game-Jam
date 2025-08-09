using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Menu : MonoBehaviour
{
    [SerializeField] private InputActionReference openMenuButton;
    [SerializeField] private GameObject floor;
    [SerializeField] private Material floorMaterial;
    private bool isMenuOpen = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnEnable()
    {
        openMenuButton.action.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        if (openMenuButton.action.triggered)
        {
            toggleMenu();
        }
    }

    private void toggleMenu()
    {
        if (!isMenuOpen)
        {
            floorMaterial.color = floorMaterial.color;
            isMenuOpen = true;
        }
        else
        {
            floorMaterial.color = Color.red;
            isMenuOpen = false;
        }
    }
}
