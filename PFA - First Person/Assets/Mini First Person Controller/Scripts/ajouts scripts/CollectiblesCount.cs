using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UIElements;

public class CollectiblesCount : MonoBehaviour
{

    [SerializeField] UnityEvent onComplete;
    int count =0;
    [SerializeField] int countMax;
    [SerializeField] TMP_Text textCount;

    // liste vide du nombre d'elt récupérés
    //public List <string> eltRecup = new List<string>();


    void Start()
    {
        countMax = transform.childCount;
        textCount.text = "" + count + "/" + countMax;
    }

    public void Compteur(int countElt)
    {
        count += countElt;
        textCount.text = "" + count + "/" + countMax;
        if (count == countMax)
        {
            onComplete?.Invoke ();
        }


        /*countElt = eltRecup.Count;
        

        if (count == countMax)
        {
            
        }
        else {

            // pour chaque elt désactivé (== recupéré) 
            count += 1;
            if (textCount != null)
            {
                textCount.text = "" + count + "/" + countMax;
            }
        }*/


    }
}
