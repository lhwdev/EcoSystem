using System;

public class PlantSpecies : Species {
	public override String name { get { return "Plant"; } }
	public override float defaultMass { get { return 7f; } }
	public override Species[] diets { get { return new Species[] { }; } }
}
