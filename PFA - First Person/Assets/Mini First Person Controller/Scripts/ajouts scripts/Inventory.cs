using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{

    public static List<string> flagList = new List<string>();
    //[SerializeField] List<string> flagList;
    public void ResetList()
    {
        flagList = new List<string>();
    }

}
