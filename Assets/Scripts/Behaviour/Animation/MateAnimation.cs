using System;
using UnityEngine;

public class MateAnimation : EntityAnimation {
	public static float animationDuration = 5f;
	public static float animationSpeed = 1f;
	public static float jumpHeight = 1.5f;

	Animal target;
	float baseY;

	public MateAnimation(Animal target) : base(isCritical: true) {
		this.target = target;
		baseY = target.transform.position.y;
	}


	protected override void Update() {
		if (time >= animationDuration) {
			var last = target.transform.position;
			target.transform.position.Set(last.x, baseY, last.z);
			FinishAnimation();
		} else {
			Debug.Log("Animating!!!");
			var height = Mathf.Abs(Mathf.Sin(animationSpeed * time)) * jumpHeight;
			var last = target.transform.position;
			target.transform.position.Set(last.x, baseY + height, last.z);
		}
	}
}
