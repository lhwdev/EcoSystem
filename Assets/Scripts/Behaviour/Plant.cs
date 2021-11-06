using System.Linq;
using UnityEngine;

public class Plant : LivingEntity {
	const float consumeSpeed = 8;

	float reproduceTime;


	public override Species species => Species.Plant;


	public override void Init(Coord coord, Environment environment) {
		base.Init(coord, environment);
		UpdateReproduceTime();
	}

	void UpdateReproduceTime() {
		reproduceTime = environment.time + 40f + ((float)environment.prng.NextDouble() * 50f);
	}

	Coord FindEmptyTile() {
		var surrounding = environment.walkableNeighboursMap[coord.x, coord.y];

		foreach (var c in surrounding) {
			if (!environment.speciesMaps[Species.Plant].GetRegion(c).Any(entity => entity.coord == c)) {
				return c;
			}
		}

		return Coord.invalid;
	}

	void Reproduce() {
		if (mass > 3f) {
			var coord = FindEmptyTile();
			if (coord == Coord.invalid) {
				return;
			}
			var child = environment.BornEntityFrom(
        mother: this, father: this,
        mass: 2f,
        coord: coord
      );
			environment.SpawnEntity(child);
			mass -= 1.7f;
		}
	}

	void Update() {
		// Photosynthesis
		mass += environment.deltaTime / 150f;

		if (reproduceTime < environment.time) {
			Reproduce();
			UpdateReproduceTime();
		}

		// Update scale following to the mass
		var scale = Mathf.Sqrt(mass / species.defaultMass); // sqrt is used to make the scale approach to 1
		transform.localScale = Vector3.one * scale;
	}

	public float Consume(float mass) {
		float amountConsumed = Mathf.Max(0, Mathf.Min(this.mass, mass));
		this.mass -= mass * consumeSpeed;

		if (this.mass <= 0) {
			Die(CauseOfDeath.Eaten);
		}

		return amountConsumed;
	}
}
