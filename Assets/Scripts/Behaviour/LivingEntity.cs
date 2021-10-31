using UnityEngine;

public abstract class LivingEntity : MonoBehaviour {

	public int colourMaterialIndex;
	
	public abstract Species species { get; }
	public Material material;

	public Coord coord;
	
	[HideInInspector]
	public int mapIndex;
	[HideInInspector]
	public Coord mapCoord;

	public float mass;

	[HideInInspector]
	public Environment environment;
	protected bool dead;

	public Genes genes;


	public static TraitInfo maxAgeTrait = new ValueTraitInfo("maxAge", defaultValue: 300f, min: 300f, max: 300f);

	public static TraitInfo[] LivingEntityDefaultTraitInfos = {
		maxAgeTrait,
	};


	public float maxAge => genes.Get<ValueTrait>(maxAgeTrait).value;


	public virtual TraitInfo[] CreateTraitInfos() {
		return LivingEntityDefaultTraitInfos;
	}

	public virtual void Init(Coord coord, Environment environment) {
		this.coord = coord;
		this.environment = environment;
		this.mass = species.defaultMass * Random.Range(0.9f, 1.1f);

		genes = new Genes(
			inheritContext: environment.inheritContext,
			infos: CreateTraitInfos()
		);

		transform.position = environment.tileCentres[coord.x, coord.y];

		// Set material to the instance material
		var meshRenderer = transform.GetComponentInChildren<MeshRenderer>();
		for (int i = 0; i < meshRenderer.sharedMaterials.Length; i++) {
			if (meshRenderer.sharedMaterials[i] is Material) {
				material = meshRenderer.materials[i];
				break;
			}
		}
	}

	protected virtual void Die(CauseOfDeath cause) {
		if (!dead) {
			dead = true;
			environment.RegisterDeath(this, cause);
			Destroy(gameObject);
		}
	}
}
