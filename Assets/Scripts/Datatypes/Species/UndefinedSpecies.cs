using System;

public class UndefinedSpecies : Species {
	public override String name { get { return "Undefined"; } }
	public override float defaultMass { get { return 10f; } }
	public override Species[] diets { get { return new Species[] { }; } }
}
