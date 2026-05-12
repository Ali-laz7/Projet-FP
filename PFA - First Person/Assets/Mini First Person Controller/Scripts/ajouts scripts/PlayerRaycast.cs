using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRaycast : MonoBehaviour
{
    Ray screenRay;
    RaycastHit hit;
    Raycastable refRaycastable;
    Raycastable currentRaycastable;

    [SerializeField] float distance = 100;
    [SerializeField] LayerMask raycastableMask;

    General controles;


    void Start()
    {
        controles = new General();
        controles.Enable();

        controles.Controles.Interact.started += OnInteract;
    }


    void OnInteract(InputAction.CallbackContext ctx)
    {
        if (currentRaycastable != null)
        {
            currentRaycastable.PerformOnInteract();
        }
    }
    
    void Update()
    {

        screenRay = Camera.main.ScreenPointToRay(new Vector3 ( Screen.width/2, Screen.height/2, distance));

        if (Physics.Raycast(screenRay, out hit, distance, raycastableMask))
        {
            refRaycastable = hit.collider.gameObject.GetComponent<Raycastable>();
            if (refRaycastable != null)
            {
                if (refRaycastable != currentRaycastable)
                {
                    CheckCurrentRaycastable();

                    refRaycastable.PerformOnOver();
                    currentRaycastable = refRaycastable;
                }
            }
            else
            {
                CheckCurrentRaycastable();
            }
        }
        else
        {
            CheckCurrentRaycastable();
        }
    }
    void CheckCurrentRaycastable()
    {
        if (currentRaycastable != null)
        {
            currentRaycastable.PerformOnOut();
            currentRaycastable = null;
        }
    }
}


