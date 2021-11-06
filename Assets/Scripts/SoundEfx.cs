using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEfx : MonoBehaviour {
  public static SoundEfx instance => GameObject.Find("Sound Efx").GetComponent<SoundEfx>();

  public AudioSource pop;

	void Start() {
    pop = transform.Find("pop").GetComponent<AudioSource>();
	}
}
