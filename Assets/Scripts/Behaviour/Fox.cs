public class Fox : Animal {
	public override Species species => Species.Fox;

	public override void Init(Coord coord, Environment environment) {
		base.Init(coord, environment);
	}
}
