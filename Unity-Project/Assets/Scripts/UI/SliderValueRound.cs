//PGRTerrain: Procedural Generation and Rendering of Terrain
//DH2323 Course Project in KTH
//SliderValueRound.cs
//Yang Zhou: yanzho@kth.se
//Yanbo Huang: yanboh@kth.se
//Huiting Wang: huitingw@kth.se
//2015.5

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// A misc script that rounds the value of slider
/// </summary>
[RequireComponent(typeof(Slider))]
public class SliderValueRound : MonoBehaviour
{

    public enum RoundType
    { EqualDistance, Exponential };

    public RoundType type;

    public float _param;
    public void RoundValue()
    {
        var curr = GetComponent<Slider>().value;
        if (type == RoundType.EqualDistance) 
            GetComponent<Slider>().value = Mathf.RoundToInt((float)curr / (float)_param) * _param;
        else
        {
            var exp = Mathf.Log((float)curr, (float)_param);
            GetComponent<Slider>().value = Mathf.Pow(_param, Mathf.RoundToInt(exp));
        }
        GetComponent<Slider>().value = Mathf.Clamp(GetComponent<Slider>().value,
                                                GetComponent<Slider>().minValue,
                                                GetComponent<Slider>().maxValue);
    }
}
