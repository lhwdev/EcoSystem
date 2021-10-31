using System;

public class FoxSpecies : Species {
	public override String name { get { return "Fox"; } }
	public override float defaultMass { get { return 37f; } }
	public override Species[] diets { get { return new Species[] { Bunny }; } }
}
