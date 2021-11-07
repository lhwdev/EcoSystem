using System;
using System.Collections.Generic;
using System.Linq;
using TerrainGeneration;
using UnityEditor;
using UnityEngine;


public class Environment : MonoBehaviour {
	const int mapRegionSize = 10;

	public int seed;

	[Header("Trees")]
	public MeshRenderer treePrefab;
	[Range(0, 1)]
	public float treeProbability;

	[Header("Populations")]
	public PopulationInfo[] currentPopuations;
	public Population[] initialPopulations;
	public GameObject tombstone;

	[Header("Debug")]
	public bool showState;
	public bool showStateForSelected;
	public bool debugStateChange;
	public bool debugStateChangeForSelected;
	public bool debugMate;
	public bool debugGene;
	public bool debugColor;
	public GameObject showStatePrefab;
	public float showStateScale = 1f;
	public bool showMapDebug;
	public Transform mapCoordTransform;
	public float mapViewDst;

	// Cached data:
	[HideInInspector]
	public Vector3[,] tileCentres;
	[HideInInspector]
	public bool[,] walkable;
	int size;
	[HideInInspector]
	public Coord[,][] walkableNeighboursMap;
	List<Coord> walkableCoords;

	Dictionary<Species, List<Species>> preyBySpecies;
	Dictionary<Species, List<Species>> predatorsBySpecies;

	// array of visible tiles from any tile; value is Coord.invalid if no visible water tile
	Coord[,] closestVisibleWaterMap;

	[HideInInspector]
	public System.Random prng;
	TerrainGenerator.TerrainData terrainData;

	[HideInInspector]
	public Dictionary<Species, Map> speciesMaps;
	GameObject entities;

	public InheritContext inheritContext;

	public float timeScale = 1f;

	[HideInInspector]
	public float deltaTime;
	[HideInInspector]
	public float time;

	// debugs
	[Range(0, 50)]
	public float hungerSpeed = 1f;
	[Range(0, 50)]
	public float thirstSpeed = 1f;
	[Range(0, 50)]
	public float mateUrgeSpeed = 1f;


#if UNITY_EDITOR
	public Dictionary<TraitInfo, bool> disabledTraits;
	static TraitInfo[] traitsToDisable = Animal.AnimalDefaultTraitInfos;
#endif

	void Start() {
		prng = new System.Random();
		inheritContext.random = prng;
		inheritContext.variationRate = 0.005f;
		inheritContext.variationPrevention = 1f;

		entities = GameObject.Find("Entities");
		time = Time.time;

		// TODO: run only once on initializing
#if UNITY_EDITOR
		disabledTraits = new Dictionary<TraitInfo, bool>();
		foreach (var trait in traitsToDisable) {
			disabledTraits[trait] = false;
		}
#endif

		Init();
		SpawnInitialPopulations();

	}

	void Update() {
		deltaTime = Time.deltaTime * timeScale;
		time += deltaTime;
	}

	void OnValidate() {
		inheritContext.debug = debugGene;
	}

	void OnDrawGizmos() {

		/* 
        if (showMapDebug) {
            if (preyMap != null && mapCoordTransform != null) {
                Coord coord = new Coord ((int) mapCoordTransform.position.x, (int) mapCoordTransform.position.z);
                preyMap.DrawDebugGizmos (coord, mapViewDst);
            }
        }
        */
	}

	int lastEntityId = 1;
	public string NextEntityId() {
		return "" + (lastEntityId++);
	}

	public void RegisterMove(LivingEntity entity, Coord from, Coord to) {
		speciesMaps[entity.species].Move(entity, from, to);
	}

	public void RegisterDeath(LivingEntity entity, CauseOfDeath cause) {
		speciesMaps[entity.species].Remove(entity, entity.coord);
		Debug.Log("[Environment] " + entity.species + " died because of " + cause);

		if (entity is Animal) {
			var animal = entity as Animal;
			var tombstone = Instantiate(this.tombstone);
			tombstone.transform.SetPositionAndRotation(animal.transform.position + Vector3.up * 2, animal.transform.rotation);

			var meshRenderer = tombstone.transform.GetComponentInChildren<MeshRenderer>();
			meshRenderer.materials[0].color = animal.material.color;
		}

		UpdateCurrentPopulations();
	}

	public Coord SenseWater(Animal self) {
		var coord = self.coord;
		var closestWaterCoord = closestVisibleWaterMap[coord.x, coord.y];
		if (closestWaterCoord != Coord.invalid) {
			float sqrDst = (tileCentres[coord.x, coord.y] - tileCentres[closestWaterCoord.x, closestWaterCoord.y]).sqrMagnitude;
			if (sqrDst <= self.maxViewDistance * self.maxViewDistance) {
				return closestWaterCoord;
			}
		}
		return Coord.invalid;
	}

	// public IEnumerable<LivingEntity> SenseTile(Coord coord, float maxViewDistance, Species kind, System.Func<LivingEntity, float> sortFunc) {
	// 	var speciesMap = speciesMaps[kind];
	// 	var sources = speciesMap.GetEntities(coord, maxViewDistance);

	// 	sources.Sort((a, b) => sortFunc(a).CompareTo(sortFunc(b)));

	// 	return sources.Where(entity => {
	// 		var targetCoord = entity.coord;
	// 		return EnvironmentUtility.TileIsVisibile(this, coord.x, coord.y, targetCoord.x, targetCoord.y);
	// 	});
	// }

	public LivingEntity SenseFood(Animal self, System.Func<LivingEntity, LivingEntity, float> foodPreference) {
		var coord = self.coord;
		var foodSources = new List<LivingEntity>();

		List<Species> prey = preyBySpecies[self.species];
		for (int i = 0; i < prey.Count; i++) {

			Map speciesMap = speciesMaps[prey[i]];

			foodSources.AddRange(speciesMap.GetEntities(coord, self.maxViewDistance));
		}

		// Sort food sources based on preference function
		foodSources.Sort((a, b) => foodPreference(self, b).CompareTo(foodPreference(self, a)));

		// Return first visible food source
		for (int i = 0; i < foodSources.Count; i++) {
			Coord targetCoord = foodSources[i].coord;
			if (EnvironmentUtility.TileIsVisibile(this, coord.x, coord.y, targetCoord.x, targetCoord.y)) {
				return foodSources[i];
			}
		}

		return null;
	}

	// Return list of animals of the same species, with the opposite gender, who are also searching for a mate
	public List<Animal> SensePotentialMates(Animal self) {
		Map speciesMap = speciesMaps[self.species];
		List<LivingEntity> visibleEntities = speciesMap.GetEntities(self.coord, self.maxViewDistance);
		var potentialMates = new List<Animal>();

		for (int i = 0; i < visibleEntities.Count; i++) {
			var visibleAnimal = (Animal)visibleEntities[i];
			if (visibleAnimal != self && visibleAnimal.sex != self.sex) {
				if (visibleAnimal.WantsMate(with: self) && !self.rejectedMateTargets.ContainsKey(visibleAnimal)) {
					potentialMates.Add(visibleAnimal);
				}
			}
		}

		return potentialMates;
	}

	public Surroundings Sense(Animal self) {
		var closestPlant = speciesMaps[self.species.diets[0]].ClosestEntity(self.coord, self.maxViewDistance);
		var surroundings = new Surroundings();
		surroundings.nearestFoodSource = closestPlant;
		surroundings.nearestWaterTile = closestVisibleWaterMap[self.coord.x, self.coord.y];

		return surroundings;
	}

	public Coord GetNextTileRandom(Coord current) {
		var neighbours = walkableNeighboursMap[current.x, current.y];
		if (neighbours.Length == 0) {
			return current;
		}
		return neighbours[prng.Next(neighbours.Length)];
	}

	/// Get random neighbour tile, weighted towards those in similar direction as currently facing
	public Coord GetNextTileWeighted(Coord current, Coord previous, double forwardProbability = 0.2, int weightingIterations = 3) {

		if (current == previous) {

			return GetNextTileRandom(current);
		}

		Coord forwardOffset = (current - previous);
		// Random chance of returning foward tile (if walkable)
		if (prng.NextDouble() < forwardProbability) {
			Coord forwardCoord = current + forwardOffset;

			if (forwardCoord.x >= 0 && forwardCoord.x < size && forwardCoord.y >= 0 && forwardCoord.y < size) {
				if (walkable[forwardCoord.x, forwardCoord.y]) {
					return forwardCoord;
				}
			}
		}

		// Get walkable neighbours
		var neighbours = walkableNeighboursMap[current.x, current.y];
		if (neighbours.Length == 0) {
			return current;
		}

		// From n random tiles, pick the one that is most aligned with the forward direction:
		Vector2 forwardDir = new Vector2(forwardOffset.x, forwardOffset.y).normalized;
		float bestScore = float.MinValue;
		Coord bestNeighbour = current;

		for (int i = 0; i < weightingIterations; i++) {
			Coord neighbour = neighbours[prng.Next(neighbours.Length)];
			Vector2 offset = neighbour - current;
			float score = Vector2.Dot(offset.normalized, forwardDir);
			if (score > bestScore) {
				bestScore = score;
				bestNeighbour = neighbour;
			}
		}

		return bestNeighbour;
	}

	// Call terrain generator and cache useful info
	void Init() {
		var sw = System.Diagnostics.Stopwatch.StartNew();

		var terrainGenerator = FindObjectOfType<TerrainGenerator>();
		terrainData = terrainGenerator.Generate();

		tileCentres = terrainData.tileCentres;
		walkable = terrainData.walkable;
		size = terrainData.size;

		preyBySpecies = new Dictionary<Species, List<Species>>();
		predatorsBySpecies = new Dictionary<Species, List<Species>>();

		// Init species maps
		speciesMaps = new Dictionary<Species, Map>();
		foreach (var species in Species.Common) {
			speciesMaps.Add(species, new Map(this, size, mapRegionSize));

			preyBySpecies.Add(species, new List<Species>());
			predatorsBySpecies.Add(species, new List<Species>());
		}

		// Store predator/prey relationships for all species
		for (int i = 0; i < initialPopulations.Length; i++) {

			if (initialPopulations[i].prefab is Animal) {
				Animal hunter = initialPopulations[i].prefab as Animal;
				Species[] diets = hunter.species.diets;

				foreach (var diet in diets) {
					preyBySpecies[hunter.species].Add(diet);
					predatorsBySpecies[diet].Add(hunter.species);
				}
			}
		}

		//LogPredatorPreyRelationships ();

		SpawnTrees();

		walkableNeighboursMap = new Coord[size, size][];

		// Find and store all walkable neighbours for each walkable tile on the map
		for (int y = 0; y < terrainData.size; y++) {
			for (int x = 0; x < terrainData.size; x++) {
				if (walkable[x, y]) {
					List<Coord> walkableNeighbours = new List<Coord>();
					for (int offsetY = -1; offsetY <= 1; offsetY++) {
						for (int offsetX = -1; offsetX <= 1; offsetX++) {
							if (offsetX != 0 || offsetY != 0) {
								int neighbourX = x + offsetX;
								int neighbourY = y + offsetY;
								if (neighbourX >= 0 && neighbourX < size && neighbourY >= 0 && neighbourY < size) {
									if (walkable[neighbourX, neighbourY]) {
										walkableNeighbours.Add(new Coord(neighbourX, neighbourY));
									}
								}
							}
						}
					}
					walkableNeighboursMap[x, y] = walkableNeighbours.ToArray();
				}
			}
		}

		// Generate offsets within max view distance, sorted by distance ascending
		// Used to speed up per-tile search for closest water tile
		List<Coord> viewOffsets = new List<Coord>();
		int viewRadius = ((int)Animal.maxViewDistanceTrait.defaultValue);
		int sqrViewRadius = viewRadius * viewRadius;
		for (int offsetY = -viewRadius; offsetY <= viewRadius; offsetY++) {
			for (int offsetX = -viewRadius; offsetX <= viewRadius; offsetX++) {
				int sqrOffsetDst = offsetX * offsetX + offsetY * offsetY;
				if ((offsetX != 0 || offsetY != 0) && sqrOffsetDst <= sqrViewRadius) {
					viewOffsets.Add(new Coord(offsetX, offsetY));
				}
			}
		}
		viewOffsets.Sort((a, b) => (a.x * a.x + a.y * a.y).CompareTo(b.x * b.x + b.y * b.y));
		Coord[] viewOffsetsArr = viewOffsets.ToArray();

		// Find closest accessible water tile for each tile on the map:
		closestVisibleWaterMap = new Coord[size, size];
		for (int y = 0; y < terrainData.size; y++) {
			for (int x = 0; x < terrainData.size; x++) {
				bool foundWater = false;
				if (walkable[x, y]) {
					for (int i = 0; i < viewOffsets.Count; i++) {
						int targetX = x + viewOffsetsArr[i].x;
						int targetY = y + viewOffsetsArr[i].y;
						if (targetX >= 0 && targetX < size && targetY >= 0 && targetY < size) {
							if (terrainData.shore[targetX, targetY]) {
								if (EnvironmentUtility.TileIsVisibile(this, x, y, targetX, targetY)) {
									closestVisibleWaterMap[x, y] = new Coord(targetX, targetY);
									foundWater = true;
									break;
								}
							}
						}
					}
				}
				if (!foundWater) {
					closestVisibleWaterMap[x, y] = Coord.invalid;
				}
			}
		}
		Debug.Log("Init time: " + sw.ElapsedMilliseconds);
	}

	void SpawnTrees() {
		// Settings:
		float maxRot = 4;
		float maxScaleDeviation = .2f;
		float colVariationFactor = 0.15f;
		float minCol = .8f;

		var spawnPrng = new System.Random(seed);
		var treeHolder = new GameObject("Tree holder").transform;
		walkableCoords = new List<Coord>();

		for (int y = 0; y < terrainData.size; y++) {
			for (int x = 0; x < terrainData.size; x++) {
				if (walkable[x, y]) {
					if (prng.NextDouble() < treeProbability) {
						// Randomize rot/scale
						float rotX = Mathf.Lerp(-maxRot, maxRot, (float)spawnPrng.NextDouble());
						float rotZ = Mathf.Lerp(-maxRot, maxRot, (float)spawnPrng.NextDouble());
						float rotY = (float)spawnPrng.NextDouble() * 360f;
						Quaternion rot = Quaternion.Euler(rotX, rotY, rotZ);
						float scale = 1 + ((float)spawnPrng.NextDouble() * 2 - 1) * maxScaleDeviation;

						// Randomize colour
						float col = Mathf.Lerp(minCol, 1, (float)spawnPrng.NextDouble());
						float r = col + ((float)spawnPrng.NextDouble() * 2 - 1) * colVariationFactor;
						float g = col + ((float)spawnPrng.NextDouble() * 2 - 1) * colVariationFactor;
						float b = col + ((float)spawnPrng.NextDouble() * 2 - 1) * colVariationFactor;

						// Spawn
						MeshRenderer tree = Instantiate(treePrefab, tileCentres[x, y], rot);
						tree.transform.parent = treeHolder;
						tree.transform.localScale = Vector3.one * scale;
						tree.material.color = new Color(r, g, b);

						// Mark tile unwalkable
						walkable[x, y] = false;
					} else {
						walkableCoords.Add(new Coord(x, y));
					}
				}
			}
		}
	}

	void UpdateCurrentPopulations() {
		// var entities = speciesMaps.Values.SelectMany(map => map.allEntities);
		var populations = new List<PopulationInfo>();

		foreach (var entry in speciesMaps) {
			var species = entry.Key;
			var map = entry.Value;

			populations.Add(new PopulationInfo(species.name, map.allEntities.Count));
		}

		currentPopuations = populations.ToArray();
	}

	public LivingEntity NewEntity(LivingEntity prefab, Coord coord) {
		var entity = Instantiate(prefab);
		entity.prefab = prefab;

		entity.Init(coord, this);
		entity.InitNew();
		inheritContext.debugCurrentEntity = entity;
		entity.genes = new Genes(
			inheritContext: inheritContext,
			infos: entity.CreateTraitInfos()
		);

		entity.age = entity.maxAge * (0.2f + 0.2f * (float)prng.NextDouble()); // defaults to around 30% of whole lifespan

		inheritContext.debugCurrentEntity = null;

		entity.PostInit();

		return entity;
	}

	public LivingEntity BornEntityFrom(LivingEntity mother, LivingEntity father, float mass, Coord coord) {
		var entity = Instantiate(mother.prefab);
		entity.prefab = mother.prefab;

		entity.Init(coord, this);
		entity.InitInherit(mother, father);
		entity.mass = mass;

		inheritContext.debugCurrentEntity = entity;
		entity.genes = Genes.InheritFrom(
			inheritContext: inheritContext,
			mother: mother.genes,
			father: father.genes
		);
		inheritContext.debugCurrentEntity = null;

		entity.PostInit();

		return entity;
	}

	Transform EntitiesObjectFor(Species species) {
		var found = entities.transform.Find(species.name);
		if (found != null) {
			return found;
		}

		var created = new GameObject();
		created.name = species.name;
		created.transform.parent = entities.transform;
		return created.transform;
	}

	public void SpawnEntity(LivingEntity entity) {
		speciesMaps[entity.species].Add(entity, entity.coord);
		entity.transform.SetParent(EntitiesObjectFor(entity.species));
	}

	public void ClearPopulations() {
		foreach (var species in speciesMaps.Keys) {
			var map = speciesMaps[species];
			foreach (var entity in map.allEntities) {
				UnityEngine.Object.Destroy(entity);
			}
			map.RemoveAll();
		}
	}

	public void SpawnInitialPopulations() {

		var spawnPrng = new System.Random(seed);
		var spawnCoords = new List<Coord>(walkableCoords);

		foreach (var pop in initialPopulations) {
			for (int i = 0; i < pop.count; i++) {
				if (spawnCoords.Count == 0) {
					Debug.Log("Ran out of empty tiles to spawn initial population");
					break;
				}
				int spawnCoordIndex = spawnPrng.Next(0, spawnCoords.Count);
				Coord coord = spawnCoords[spawnCoordIndex];
				spawnCoords.RemoveAt(spawnCoordIndex);
				var entity = NewEntity(pop.prefab, coord);
				SpawnEntity(entity);
			}
		}

		UpdateCurrentPopulations();
	}

	void LogPredatorPreyRelationships() {
		foreach (var species in Species.Common) {
			string s = "(" + species.name + ") ";
			var prey = preyBySpecies[species];
			var predators = predatorsBySpecies[species];

			s += "Prey: " + ((prey.Count == 0) ? "None" : "");
			for (int j = 0; j < prey.Count; j++) {
				s += prey[j];
				if (j != prey.Count - 1) {
					s += ", ";
				}
			}

			s += " | Predators: " + ((predators.Count == 0) ? "None" : "");
			for (int j = 0; j < predators.Count; j++) {
				s += predators[j];
				if (j != predators.Count - 1) {
					s += ", ";
				}
			}
			print(s);
		}
	}



	List<Animal> SensePredator(Animal self, Species predatorSpecies) {
		Map speciesMap = speciesMaps[predatorSpecies];
		List<LivingEntity> visibleEntities = speciesMap.GetEntities(self.coord, Math.Min(self.maxViewDistance, self.fleeDetectDistance));
		var predators = new List<Animal>();

		for (int i = 0; i < visibleEntities.Count; i++) {
			var visibleAnimal = (Animal)visibleEntities[i];
			predators.Add(visibleAnimal);
		}

		return predators;
	}

	public List<Animal> SensePredators(Animal self) {
		var predators = new List<Animal>();
		foreach (var predator in predatorsBySpecies[self.species]) {
			predators.AddRange(SensePredator(self, predator));
		}
		return predators;
	}

	public Vector2 FleeDirection(Animal self, List<Animal> predators) {
		var vector = Vector2.zero;

		foreach (var predator in predators) {
			var delta = self.coord - predator.coord;
			vector += new Vector2(delta.x, delta.y) * (0.5f + predator.moveSpeed * 1f);
		}

		return vector.normalized;
	}


	List<Animal> SensePrey(Animal self, Species preySpecies) {
		Map speciesMap = speciesMaps[preySpecies];
		List<LivingEntity> visibleEntities = speciesMap.GetEntities(self.coord, self.maxViewDistance);
		var preys = new List<Animal>();

		for (int i = 0; i < visibleEntities.Count; i++) {
			var visibleAnimal = (Animal)visibleEntities[i];
			preys.Add(visibleAnimal);
		}

		return preys;
	}

	public List<Animal> SensePreys(Animal self) {
		var preys = new List<Animal>();
		foreach (var preyKind in preyBySpecies[self.species]) {
			preys.AddRange(SensePrey(self, preyKind));
		}
		return preys;
	}

	public Animal SenseBestPrey(Animal self) {
		var preys = SensePreys(self);
		return preys.OrderBy(p => p.mass / (1 + Coord.Distance(p.coord, self.coord))).Last();
	}


	private int Coerce(int number, int max) {
		if (number >= 0) {
			if (number <= max) {
				return number;
			} else {
				return max;
			}
		} else {
			return 0;
		}
	}
	public Coord CoerceCoord(Coord coord) {
		return new Coord(Coerce(coord.x, walkable.GetLength(0) - 1), Coerce(coord.y, walkable.GetLength(1) - 1));
	}

	public bool Walkable(int x, int y) {
		if (x >= 0 && x < walkable.GetLength(0) && y >= 0 && y < walkable.GetLength(1)) {
			return walkable[x, y];
		} else {
			return false;
		}
	}

	[System.Serializable]
	public struct Population {
		public LivingEntity prefab;
		public int count;
	}

	[System.Serializable]
	public struct PopulationInfo {
		public String species;
		public int count;

		public PopulationInfo(String species, int count) {
			this.species = species;
			this.count = count;
		}
	}


	// Debug
	public bool DebugStateChange(LivingEntity self) {
		if (debugStateChange) {
			if (debugStateChangeForSelected) {
				return Selection.Contains(self);
			} else {
				return true;
			}
		} else {
			return false;
		}
	}
}
