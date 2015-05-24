using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
