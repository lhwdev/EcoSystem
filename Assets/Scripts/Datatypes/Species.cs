// public enum Species {
//     Undefined = (1 << 0),
//     Plant = (1 << 1),
//     Rabbit = (1 << 2),
//     Fox = (1 << 3)
// }


using System;


[System.Serializable]
public abstract class Species {
	public static Species Undefined = new UndefinedSpecies();

	public static Species Plant = new PlantSpecies();

	public static Species Bunny = new BunnySpecies();

	public static Species Fox = new FoxSpecies();

	public static Species[] Common = { Undefined, Plant, Bunny, Fox };


	public abstract String name { get; }

	public abstract float defaultMass { get; }

	public abstract Species[] diets { get; }
}

