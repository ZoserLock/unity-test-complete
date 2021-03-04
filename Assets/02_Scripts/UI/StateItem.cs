using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Class that represent a state item in a shovel report.
public class StateItem : MonoBehaviour
{
    [SerializeField]
    private Image _mainImage = null;

    [SerializeField]
    private Text _mainText = null;

    public void SetState(ShovelState state)
    {
        _mainText.text   = state.Name.ToUpper();
        _mainImage.color = HexToColor(state.Color);
    }

    // Function to parse hex color to unity color.
    public static Color HexToColor(string hex)
    {
        try
        {
            if (hex != null)
            {
                byte r = byte.Parse(hex.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(hex.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

                return new Color32(r, g, b, 255);
            }
        }
        catch(Exception e)
        {
            Debug.LogError(e.Message);
        }

        return Color.white;
    }
}
