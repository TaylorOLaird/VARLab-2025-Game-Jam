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

    // Update is called once per frame
    void Update()
    {

    }

    private void OnEnable()
    {
        openMenuButton.action.Enable();
        openMenuButton.action.performed += toggleMenu;
    }

    private void OnDisable()
    {
        openMenuButton.action.performed -= toggleMenu;
        openMenuButton.action.Disable();
    }

    private void toggleMenu(InputAction.CallbackContext context)
    {
        isMenuOpen = !isMenuOpen;
        if (isMenuOpen)
        {
            floor.GetComponent<Renderer>().material.color = Color.red;
        }
        else
        {
            floor.GetComponent<Renderer>().material.color = floorMaterial.color;
        }
    }
}
