using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Expression : MonoBehaviour
{
    static public Expression Instance { get; private set; }
    private Material material;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        material = renderer.material;
        SetColor(Color.red);
    }

    public void SetColor(Color color)
    {
        material.SetColor("_EmissionColor", color);
    }
}
