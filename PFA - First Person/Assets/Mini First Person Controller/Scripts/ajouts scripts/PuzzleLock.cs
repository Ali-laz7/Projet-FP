using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PuzzleLock : MonoBehaviour
{
    [SerializeField] UnityEvent onLocked;
    [SerializeField] UnityEvent onUnLocked;


    public void PerformUnlock(string flagName)
    {
        if (Inventory.flagList.Contains(flagName) == true)
        {
            onUnLocked?.Invoke();
        }
        else
        {
            onLocked?.Invoke();
        }

    }


}
