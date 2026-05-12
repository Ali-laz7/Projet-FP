using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Raycastable : MonoBehaviour
{

    [SerializeField] UnityEvent onOver;
    [SerializeField] UnityEvent onOut;
    [SerializeField] UnityEvent onInteract;
    
    public void PerformOnOver()
    {
        onOver?.Invoke();
    }

    public void PerformOnOut()
    {
        onOut?.Invoke();
    }

    public void PerformOnInteract()
    {
        onInteract?.Invoke();
    }




}
