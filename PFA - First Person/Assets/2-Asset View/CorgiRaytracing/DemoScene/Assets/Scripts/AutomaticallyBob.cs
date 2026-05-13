using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-10)]
public class AutomaticallyBob : MonoBehaviour
{
    private Vector3 _startPosition;

    void Start()
    {
        _startPosition = transform.position;
    }

    void Update()
    {
        var speed = 1f;
        var magnitude = 1f;

        var bobAxis = Vector3.up;
        transform.position = _startPosition + bobAxis * Mathf.Sin(Time.time * speed) * magnitude;
    }
}
