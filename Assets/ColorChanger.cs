using System;
using UnityEngine;

public class ColorChanger : MonoBehaviour
{
    private void SetColor(GameObject transform, Color color)
    {
        transform.GetComponent<Renderer>().material.color = color;
    }

    public void UpdateColor(string[] values)
    {
        var colorString = values[0];
        Debug.Log("Color string: " + colorString);
        var shapeString = values[1];
        Debug.Log("Shape string: " + shapeString);

        if (ColorUtility.TryParseHtmlString(colorString, out var color))
        {
            Debug.Log(color);
            if (!string.IsNullOrEmpty(shapeString))
            {
                var shape = GameObject.Find(shapeString);
                if (shape) SetColor(shape, color);
            }
        }
    }
}
