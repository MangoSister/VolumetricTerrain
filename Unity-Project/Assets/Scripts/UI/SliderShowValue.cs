//PGRTerrain: Procedural Generation and Rendering of Terrain
//DH2323 Course Project in KTH
//SliderShowValue.cs
//Yang Zhou: yanzho@kth.se
//Yanbo Huang: yanboh@kth.se
//Huiting Wang: huitingw@kth.se
//2015.5

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// A misc script that shows the value of slider
/// </summary>
[RequireComponent(typeof(Text))]
public class SliderShowValue : MonoBehaviour 
{
    public Slider _slider;
    public void ShowValue()
    {
        GetComponent<Text>().text = _slider.value.ToString();
    }
}
