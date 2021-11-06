using UnityEngine;
using UnityEditor;

public abstract class LivingEntity : MonoBehaviour {
	public string id;

	public int colourMaterialIndex;

	public LivingEntity prefab;
	public abstract Species species { get; }

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
	
	public float bornAt;
	public float age {
		get => environment.time - bornAt;
		set => bornAt = environment.time + value;
	}


	public static TraitInfo maxAgeTrait = new ValueTraitInfo("maxAge", defaultValue: 300f, min: 300f, max: 300f);

	public static TraitInfo[] LivingEntityDefaultTraitInfos = {
		maxAgeTrait,
	};


	public float maxAge => genes.Get<ValueTrait>(maxAgeTrait).value;


	public virtual TraitInfo[] CreateTraitInfos() {
		return LivingEntityDefaultTraitInfos;
	}

	public virtual void Init(Coord coord, Environment environment) {
		id = environment.NextEntityId();
		name = species.name + " #" + id;

		this.coord = coord;
		this.environment = environment;

		transform.position = environment.tileCentres[coord.x, coord.y];

		bornAt = environment.time;
	}

	public virtual void InitNew() {
		this.mass = species.defaultMass * Random.Range(0.9f, 1.1f);
	}

	public virtual void InitInherit(LivingEntity mother, LivingEntity father) { }

	public virtual void PostInit() { }

	void OnValidate() {
		genes.OnValidate();
	}

	protected virtual void Die(CauseOfDeath cause) {
		if (!dead) {
			dead = true;
			environment.RegisterDeath(this, cause);
			Destroy(gameObject);
		}
	}
}
