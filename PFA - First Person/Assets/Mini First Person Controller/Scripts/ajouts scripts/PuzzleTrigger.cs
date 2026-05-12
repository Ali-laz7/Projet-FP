using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PuzzleTrigger : MonoBehaviour
{

    [SerializeField] string tagAndNameToCheck;
    [SerializeField] UnityEvent onEnter;
    [SerializeField] UnityEvent onExit;

    void Start()
    {
        
    }

    
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if ( other.tag == tagAndNameToCheck || other.name == tagAndNameToCheck)
        {
            onEnter?.Invoke();

        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == tagAndNameToCheck || other.name == tagAndNameToCheck)
        {
            onExit?.Invoke();

        }
    }
}
