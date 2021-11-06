using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;

public class AnimalState : MonoBehaviour
{
    [HideInInspector]
    public Animal animal;

	Scrollbar hunger;
	Scrollbar hungerMax;
	Scrollbar thirst;
	Scrollbar thirstMax;
    RectTransform rectTransform;


    void Start() {
        var canvas = transform.GetChild(0);
		hunger = canvas.Find("hunger").GetComponent<Scrollbar>();
		hungerMax = canvas.Find("hunger_max").GetComponent<Scrollbar>();
		thirst = canvas.Find("thirst").GetComponent<Scrollbar>();
		thirstMax = canvas.Find("thirst_max").GetComponent<Scrollbar>();
        rectTransform = GetComponentInChildren<RectTransform>();
    }


    void Update() {
        float hungerScale = animal.environment.showStateScale * 40f;
        hunger.size = animal.hunger / hungerScale;
        hungerMax.value = animal.mass / hungerScale;

        float thirstScale = animal.environment.showStateScale * 40f;
		thirst.size = animal.thirst / thirstScale;
        thirstMax.value = animal.mass / thirstScale;

        rectTransform.LookAt(Camera.main.transform.position);
    }
}
