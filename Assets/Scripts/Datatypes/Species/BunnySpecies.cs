using System;

public class BunnySpecies : Species {
	public override String name { get { return "Bunny"; } }
	public override float defaultMass { get { return 13f; } }
	public override Species[] diets { get { return new Species[] { Species.Plant }; } }
}
