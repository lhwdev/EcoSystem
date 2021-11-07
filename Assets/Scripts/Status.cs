using UnityEngine;
using UnityEngine.UI;

public class Status : MonoBehaviour {
  Text text;
  Environment environment;

	void Start() {
    text = transform.Find("Text").GetComponent<Text>();
    environment = GameObject.Find("Environment").GetComponent<Environment>();
	}

	void Update() {
    text.text = $"Time: {Mathf.Round(environment.time)}";
	}
}
