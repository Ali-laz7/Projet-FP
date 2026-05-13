using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-11)]
public class AutomaticallyRotate : MonoBehaviour
{
    private void Update()
    {
        transform.Rotate(transform.up * (45 * Time.deltaTime));
    }
}
