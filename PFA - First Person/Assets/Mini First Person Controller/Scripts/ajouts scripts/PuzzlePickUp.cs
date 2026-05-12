using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzlePickUp : MonoBehaviour
{
    
    public void PerformPickUp(string flagName)
    {
        if (Inventory.flagList.Contains(flagName)== false)
        {
            Inventory.flagList.Add(flagName);
        }
    }
}
