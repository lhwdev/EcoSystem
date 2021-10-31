using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class TimeSliderScript : MonoBehaviour 
{
    Slider slider;
    Text sliderText;

    public void Start() {
        slider = GetComponentInChildren<Slider>();
        sliderText = GetComponentInChildren<Text>();
        slider.onValueChanged.AddListener(OnValueChange);
    }

    public void OnValueChange(float value) {
        Debug.Log("changed " + value);
        Time.timeScale = value;
        sliderText.text = "" + (Mathf.Round(value * 100) / 100);
    }
}
