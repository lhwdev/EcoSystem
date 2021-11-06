using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rabbit : Animal {
	public override Species species => Species.Bunny;


	public override void Init(Coord coord, Environment environment) {
		base.Init(coord, environment);
    }
}
