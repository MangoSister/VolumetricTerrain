using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Text))]
public class SliderShowValue : MonoBehaviour 
{
    public Slider _slider;
    public void ShowValue()
    {
        GetComponent<Text>().text = _slider.value.ToString();
    }
}
