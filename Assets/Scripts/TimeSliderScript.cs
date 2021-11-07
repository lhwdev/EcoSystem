using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class TimeSliderScript : MonoBehaviour {
	Slider slider;
	Text sliderText;
	Environment environment;

	public void Start() {
		slider = GetComponentInChildren<Slider>();
		sliderText = GetComponentInChildren<Text>();
		environment = GameObject.Find("Environment").GetComponent<Environment>();
    OnValueChange(environment.timeScale);
		slider.onValueChanged.AddListener(OnValueChange);
	}

	public void OnValueChange(float value) {
		environment.timeScale = value;
		sliderText.text = "" + (Mathf.Round(value * 100) / 100);
	}
}
