using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerPushCollider : MonoBehaviour
{
    [SerializeField] InputActionReference trigger;

    SphereCollider controllerCollider;

    // Start is called before the first frame update
    void Start()
    {
        controllerCollider = GetComponent<SphereCollider>();
        trigger.action.started += pushPrimed;
        trigger.action.canceled += pushStopped;

        controllerCollider.enabled = false;

    }

    void pushPrimed(InputAction.CallbackContext context)
    {
        controllerCollider.enabled = true;
    }
    void pushStopped(InputAction.CallbackContext context)
    {
        controllerCollider.enabled = false;
    }
}
