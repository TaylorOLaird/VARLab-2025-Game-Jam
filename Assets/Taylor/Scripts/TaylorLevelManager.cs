using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TaylorLevelManager : MonoBehaviour
{
    [SerializeField] private InputActionReference simulateDonHMDButton;
    [SerializeField] private InputActionReference simulateDofHMDButton;
    // Start is called before the first frame update
    void Start()
    {
        simulateDonHMDButton.action.Enable();
        simulateDofHMDButton.action.Enable();
        simulateDonHMDButton.action.performed += SimulateDonHMD;
        simulateDofHMDButton.action.performed += SimulateDofHMD;
    }

    private void SimulateDonHMD(InputAction.CallbackContext context)
    {
        Debug.Log("Simulating HMD Don");
    }

    private void SimulateDofHMD(InputAction.CallbackContext context)
    {
        Debug.Log("Simulating HMD Doff");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
