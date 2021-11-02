using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;



public enum Sex {
	Male,
	Female
}


public abstract class Animal : LivingEntity {
	public CreatureAction currentAction;
	public CreatureActionReason currentActionReason;
	public Color maleColour;
	public Color femaleColour;

	// Settings:
	public float timeBetweenActionChoices = 1;
	public float timeToDeathByHunger = 100;
	public float timeToDeathByThirst = 90;
	public float hungryPointBase = .1f;
	public float thirstyPointBase = .8f;
	public float matePointBase = .4f;


	public float criticalPercent = 0.7f;

	// Visual settings:
	float moveArcHeight = .2f;

	// State:
	[Header("State")]
	public float hunger;
	public float thirst;

	protected LivingEntity foodTarget;
	protected Coord waterTarget;

	// Move data:
	public bool animatingMovement;
	public bool animatingCritical;
	public float animationDuration;
	public int remainingRestWalkCount = 0;
	float restWalkAroundStartTime = float.MinValue;
	public Coord moveFromCoord;
	public Coord moveTargetCoord;
	public Vector3 moveStartPos;
	public Vector3 moveTargetPos;
	public float moveTime;
	float moveSpeedFactor;
	float moveArcHeightFactor;
	Coord[] path;
	int pathIndex;

	// Other
	float lastActionChooseTime;
	const float sqrtTwo = 1.4142f;
	const float oneOverSqrtTwo = 1 / sqrtTwo;



	public static BinaryTraitInfo sexTrait = new BinaryTraitInfo("sex", defaultValue: null);
	public static ValueTraitInfo moveSpeedTrait = new ValueTraitInfo("moveSpeed", defaultValue: 1.5f, min: 0.5f, max: 5f);
	public static ValueTraitInfo drinkDurationTrait = new ValueTraitInfo("drinkDuration", defaultValue: 6f, min: 1f, max: 100f);
	public static ValueTraitInfo eatDurationTrait = new ValueTraitInfo("eatDuration", defaultValue: 10f, min: 1f, max: 100f);
	public static ValueTraitInfo mateDesireTrait = new ValueTraitInfo("mateDesire", defaultValue: .4f, min: 0.0f, max: 1.0f);
	public static ValueTraitInfo maxViewDistanceTrait = new ValueTraitInfo("maxViewDistance", defaultValue: 10f, min: 5f, max: 16f);

	private static TraitInfo[] AnimalDefaultTraitInfos = LivingEntity.LivingEntityDefaultTraitInfos.ConcatArray(new TraitInfo[] {
		moveSpeedTrait,
		drinkDurationTrait,
		eatDurationTrait,
		mateDesireTrait,
		maxViewDistanceTrait,
		sexTrait,
	});

	public float moveSpeed => genes.Get<ValueTrait>(moveSpeedTrait).value;
	public float drinkDuration => genes.Get<ValueTrait>(drinkDurationTrait).value;
	public float eatDuration => genes.Get<ValueTrait>(eatDurationTrait).value;
	public float mateDesire => genes.Get<ValueTrait>(mateDesireTrait).value;
	public float maxViewDistance => genes.Get<ValueTrait>(maxViewDistanceTrait).value;
	public Sex sex => genes.Get<BinaryTrait>(sexTrait).value ? Sex.Male : Sex.Female;

	public float currentMateDesire = 0f;

	protected GameObject debugPanel => transform.Find("AnimalState")?.gameObject;
	protected bool showState => environment.showState && (environment.showStateForSelected ? Selection.activeGameObject == gameObject : true);


	public override TraitInfo[] CreateTraitInfos() {
		return AnimalDefaultTraitInfos;
	}


	public override void Init(Coord coord, Environment environment) {
		base.Init(coord, environment);

		moveFromCoord = coord;

		material.color = sex == Sex.Male ? maleColour : femaleColour;

		ChooseNextAction(true);
	}


	protected virtual void Update() {
		// Update debug panel
		UpdateDebugPanel();

		// Increase hunger and thirst over time
		var sqrtMass = Mathf.Sqrt(mass);
		hunger += environment.deltaTime * 1 / timeToDeathByHunger * sqrtMass;
		thirst += environment.deltaTime * 1 / timeToDeathByThirst * sqrtMass;
		currentMateDesire += environment.deltaTime * mateDesire / 30;

		// Animate movement. After moving a single tile, the animal will be able to choose its next action
		if (animatingMovement) {
			if (animatingCritical) {
				AnimateMove();
			} else {
				var changed = ChooseNextAction(false);
				if (!changed) {
					AnimateMove();
				}
			}
		} else {
			// Handle interactions with external things, like food, water, mates
			HandleInteractions();
			float timeSinceLastActionChoice = Time.time - lastActionChooseTime;
			if (timeSinceLastActionChoice > timeBetweenActionChoices) {
				ChooseNextAction(true);
			}
		}

		if (hunger >= mass) {
			Die(CauseOfDeath.Hunger);
		} else if (thirst >= mass) {
			Die(CauseOfDeath.Thirst);
		}
	}

	// Animals choose their next action after each movement step (1 tile),
	// or, when not moving (e.g interacting with food etc), at a fixed time interval
	protected virtual bool ChooseNextAction(bool required) {
		lastActionChooseTime = Time.time;
		// Get info about surroundings

		// Decide next action:
		// Eat if (more hungry than thirsty) or (currently eating and not critically thirsty)
		bool currentlyEating = currentAction == CreatureAction.Eating && foodTarget && hunger > 0;
		var hungryPoint = hungryPointBase * mass;
		var thirstyPoint = thirstyPointBase * mass;
		if (hunger >= hungryPoint) {
			if (thirst >= thirstyPoint) {
				if (hunger >= thirst || currentlyEating && hunger < criticalPercent * mass) {
					FindFood();
				} else { // More thirsty than hungry
					FindWater();
				}
			} else {
				FindFood();
			}
		} else {
			if (thirst >= thirstyPoint) {
				FindWater();
			} else {
				if (currentMateDesire > matePointBase) {
					FindMate();
				} else {
					if (!required) return false;
					Rest();
				}
			}
		}


		Act();
		return true;
	}

	private void UpdateDebugPanel() {
		var panel = debugPanel;
		var show = showState;

		if (show && panel == null) {
			panel = Instantiate(environment.showStatePrefab);
			panel.name = "AnimalState";
			var stateScript = panel.GetComponent<AnimalState>();
			stateScript.animal = this;
			panel.transform.SetParent(transform);
			panel.transform.localPosition = new Vector3(0f, 1f, 0f);
			panel.transform.localRotation = Quaternion.identity;
		} else if (!show && panel != null) {
			Destroy(panel);
		}
	}


	float NextFloat(float minValue, float maxValue) {
		return ((float)environment.prng.NextDouble()) * (maxValue - minValue) + minValue;
	}

	protected virtual void Rest() {
		var current = Time.fixedTime;
		currentActionReason = CreatureActionReason.Rest;
		if (remainingRestWalkCount > 0) {
			currentAction = CreatureAction.RestingWalking;
			remainingRestWalkCount--;
		} else if (restWalkAroundStartTime > current) {
			currentAction = CreatureAction.RestingIdle;
		} else {
			// start random walking
			currentAction = CreatureAction.RestingWalking;

			restWalkAroundStartTime = current + (((float)environment.prng.NextDouble()) * 20f);
			remainingRestWalkCount = environment.prng.Next(8);
		}

		if (currentAction == CreatureAction.RestingWalking) {
			if (!animatingMovement) {
				StartMoveToCoord(environment.GetNextTileWeighted(coord, moveFromCoord), NextFloat(0.9f, 2.3f), false);
			}
		}
	}

	protected virtual void FindFood() {
		currentActionReason = CreatureActionReason.Hungry;
		LivingEntity foodSource = environment.SenseFood(this, FoodPreference);

		if (foodSource) {
			currentAction = CreatureAction.GoingToFood;
			foodTarget = foodSource;
			CreatePath(foodTarget.coord);

		} else {
			currentAction = CreatureAction.Exploring;
		}
	}

	protected virtual void FindWater() {
		currentActionReason = CreatureActionReason.Thirsty;
		Coord waterTile = environment.SenseWater(this);

		if (waterTile != Coord.invalid) {
			currentAction = CreatureAction.GoingToWater;
			waterTarget = waterTile;
			CreatePath(waterTarget);

		} else {
			currentAction = CreatureAction.Exploring;
		}
	}

	protected virtual void FindMate() {
		currentActionReason = CreatureActionReason.Mate;
		environment.SensePotentialMates(this);


	}

	// When choosing from multiple food sources, the one with the lowest penalty + highest visibility will be selected
	protected virtual float FoodPreference(LivingEntity self, LivingEntity food) {
		return food.mass / Coord.SqrDistance(self.coord, food.coord);
	}

	protected void Act() {
		switch (currentAction) {
			case CreatureAction.Exploring:
				StartMoveToCoord(environment.GetNextTileWeighted(coord, moveFromCoord), 1f, true);
				break;
			case CreatureAction.GoingToFood:
				if (Coord.AreNeighbours(coord, foodTarget.coord)) {
					LookAt(foodTarget.coord);
					currentAction = CreatureAction.Eating;
				} else {
					StartMoveToCoord(path[pathIndex], 1f, true);
					pathIndex++;
				}
				break;
			case CreatureAction.GoingToWater:
				if (Coord.AreNeighbours(coord, waterTarget)) {
					LookAt(waterTarget);
					currentAction = CreatureAction.Drinking;
				} else {
					StartMoveToCoord(path[pathIndex], 1f, true);
					pathIndex++;
				}
				break;
		}
	}

	protected void CreatePath(Coord target) {
		// Create new path if current is not already going to target
		if (
			path == null ||
			path.Length == 0 ||
			pathIndex >= path.Length ||
			pathIndex > 0 && (path[path.Length - 1] != target || path[pathIndex - 1] != moveTargetCoord)
		) {
			path = EnvironmentUtility.GetPath(environment, coord.x, coord.y, target.x, target.y);
			pathIndex = 0;
		}
	}

	protected void StartMoveToCoord(Coord target, float duration, bool critical) {
		moveFromCoord = coord;
		moveTargetCoord = target;
		moveStartPos = transform.position;
		moveTargetPos = environment.tileCentres[moveTargetCoord.x, moveTargetCoord.y];
		animatingMovement = true;
		animationDuration = duration;
		animatingCritical = critical;

		bool diagonalMove = Coord.SqrDistance(moveFromCoord, moveTargetCoord) > 1;
		moveArcHeightFactor = (diagonalMove) ? sqrtTwo : 1;
		moveSpeedFactor = (diagonalMove) ? oneOverSqrtTwo : 1;

		LookAt(moveTargetCoord);
	}

	protected void LookAt(Coord target) {
		if (target != coord) {
			Coord offset = target - coord;
			transform.eulerAngles = Vector3.up * Mathf.Atan2(offset.x, offset.y) * Mathf.Rad2Deg;
		}
	}

	void HandleInteractions() {
		if (currentAction == CreatureAction.Eating) {
			if (foodTarget && hunger > 0) {
				float eatAmount = Mathf.Min(hunger, environment.deltaTime * 15 / eatDuration);
				if (foodTarget is Plant) {
					eatAmount = ((Plant)foodTarget).Consume(eatAmount);
					hunger -= eatAmount;
				}
			}
		} else if (currentAction == CreatureAction.Drinking) {
			if (thirst > 0) {
				thirst -= environment.deltaTime * 23 / drinkDuration; // thirst is easier to resolve than hunger
				thirst = Mathf.Clamp01(thirst);
			}
		}
	}

	void AnimateMove() {
		// Move in an arc from start to end tile
		moveTime = Mathf.Min(1, moveTime + environment.deltaTime * moveSpeed * moveSpeedFactor);
		float height = (1 - 4 * (moveTime - .5f) * (moveTime - .5f)) * moveArcHeight * moveArcHeightFactor;
		transform.position = Vector3.Lerp(moveStartPos, moveTargetPos, moveTime / animationDuration) + Vector3.up * height;

		// Finished moving
		if (moveTime >= 1) {
			environment.RegisterMove(this, moveFromCoord, moveTargetCoord);
			coord = moveTargetCoord;
			hunger += (moveStartPos - moveTargetPos).sqrMagnitude * mass / 1500 / timeToDeathByHunger;

			animatingMovement = false;
			moveTime = 0;
			ChooseNextAction(true);
		}
	}

	void OnDrawGizmosSelected() {
		if (Application.isPlaying) {
			var surroundings = environment.Sense(this);
			Gizmos.color = Color.white;
			if (surroundings.nearestFoodSource != null) {
				Gizmos.DrawLine(transform.position, surroundings.nearestFoodSource.transform.position);
			}
			if (surroundings.nearestWaterTile != Coord.invalid) {
				Gizmos.DrawLine(transform.position, environment.tileCentres[surroundings.nearestWaterTile.x, surroundings.nearestWaterTile.y]);
			}

			if (currentAction == CreatureAction.GoingToFood) {
				var path = EnvironmentUtility.GetPath(environment, coord.x, coord.y, foodTarget.coord.x, foodTarget.coord.y);
				if (path != null) {
					Gizmos.color = Color.black;
					for (int i = 0; i < path.Length; i++) {
						Gizmos.DrawSphere(environment.tileCentres[path[i].x, path[i].y], .2f);
					}
				}
			}
		}
	}

}
