using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Runtime.InteropServices;

public struct InheritContext {
	public System.Random random;
	public float variationRate;
	public float variationPrevention;


	private bool NextBool() {
		return random.Next(0, 2) == 1;
	}

	public bool IsVariation(TraitInfo traitInfo) {
		var isVariation = (float)random.NextDouble() < variationRate;
		if (isVariation) {
			Debug.Log("Variation happened for " + traitInfo.name);
		}
		return isVariation;
	}

	public (bool, bool) InheritTraitBare(bool fatherA, bool fatherB, bool motherA, bool motherB) {
		bool father = NextBool() ? fatherA : fatherB;
		bool mother = NextBool() ? motherA : motherB;
		return (father, mother);
	}

	public bool InheritTraitBare(bool father, bool mother) {
		return NextBool() ? father : mother;
	}

	public float InheritTraitBare(float mother, float father) {
		return Mathf.Lerp(mother, father, (float)random.NextDouble());
	}

	public (bool, bool) VariationTraitBoolDouble() {
		return (NextBool(), NextBool());
	}


	public float VariationTraitFloat(float min, float max, float center, float centerizeFraction) {
		var k = (center - min) / (max - min);
		var x = (float)random.NextDouble();
		var n = variationPrevention * centerizeFraction;

		// bias toward center; using quadratic function
		if (x < 0.5f) {
			x = (1 / Mathf.Pow(k, n - 1)) * Mathf.Pow(Mathf.Abs(x - k), n) + k;
		} else {
			x = (1 / Mathf.Pow(1 - k, n)) * Mathf.Pow(Mathf.Abs(x - k), n) + k;
		}
		return Mathf.Lerp(min, max, x);
	}
}


[Serializable]
public abstract class Trait {
	public abstract TraitInfo info { get; }

	public abstract Trait InheritWith(InheritContext context, Trait other);
}

[Serializable]
public abstract class TraitInfo {
	public String name;

	public abstract Trait Default(InheritContext context);
}

// 대립유전자
public class Pair {
	public bool a;
	public bool b;

	public Pair(bool a, bool b) {
		this.a = a;
		this.b = b;
	}

	public void Deconstruct(out bool a, out bool b) {
		a = this.a;
		b = this.b;
	}
}

public class BinaryTraitInfo : TraitInfo {
	public bool? defaultValue;


	public BinaryTraitInfo(String name, bool? defaultValue) : base() {
		this.name = name;
		this.defaultValue = defaultValue;
	}


	public override Trait Default(InheritContext context) {
		if (defaultValue == null || context.IsVariation(this)) {
			var (a, b) = context.VariationTraitBoolDouble();
			return new BinaryTrait(this, a);
		} else {
			return new BinaryTrait(this, (bool)defaultValue);
		}
	}
}

public class BinaryTrait : Trait {
	public static BinaryTrait Inherit(InheritContext context, BinaryTrait father, BinaryTrait mother) {
		if (father.traitInfo != mother.traitInfo) {
			throw new ArgumentException("father & mother information is different");
		}

		if (context.IsVariation(father.traitInfo)) {
			var (a, b) = context.VariationTraitBoolDouble();
			return new BinaryTrait(father.traitInfo, a);
		} else {
			return new BinaryTrait(father.traitInfo, context.InheritTraitBare(father.value, mother.value));
		}
	}


	public BinaryTraitInfo traitInfo;
	public bool value;

	public override TraitInfo info => traitInfo;


	public BinaryTrait(BinaryTraitInfo info, bool value) : base() {
		this.traitInfo = info;
		this.value = value;
	}

	public override Trait InheritWith(InheritContext context, Trait other) {
		return Inherit(context, this, other as BinaryTrait);
	}
}


public class AllericTraitInfo : TraitInfo {

	public bool isDominanceTrait;
	public Pair defaultValue;



	public AllericTraitInfo(String name, bool isDominanceTrait, Pair defaultValue) : base() {
		this.name = name;
		this.isDominanceTrait = isDominanceTrait;
		this.defaultValue = defaultValue;
	}

	public override Trait Default(InheritContext context) {
		if (defaultValue == null || context.IsVariation(this)) {
			var (a, b) = context.VariationTraitBoolDouble();
			return new AllericTrait(this, a, b);
		} else {
			return new AllericTrait(this, defaultValue.a, defaultValue.b);
		}
	}
}

public class AllericTrait : Trait {
	public static AllericTrait Inherit(InheritContext context, AllericTrait father, AllericTrait mother) {
		if (father.traitInfo != mother.traitInfo) {
			throw new ArgumentException("father & mother information is different");
		}

		if (context.IsVariation(father.traitInfo)) {
			var (a, b) = context.VariationTraitBoolDouble();
			return new AllericTrait(father.traitInfo, a, b);
		} else {
			var (a, b) = context.InheritTraitBare(father.a, father.b, mother.a, mother.b);

			return new AllericTrait(father.traitInfo, a, b);
		}
	}


	public AllericTraitInfo traitInfo;
	public bool a;
	public bool b;

	public bool hasTrait {
		get {
			var result = a && b;
			if (traitInfo.isDominanceTrait) {
				return result;
			} else {
				return !result;
			}
		}
	}

	public override TraitInfo info => traitInfo;


	public AllericTrait(AllericTraitInfo info, bool a, bool b) : base() {
		this.traitInfo = info;
		this.a = a;
		this.b = b;
	}

	public override Trait InheritWith(InheritContext context, Trait other) {
		return Inherit(context, this, other as AllericTrait);
	}
}


public class ValueTraitInfo : TraitInfo {
	public float defaultValue;
	public float min;
	public float max;
	public float defaultUrge;


	public ValueTraitInfo(String name, float defaultValue, float min, float max, float defaultUrge = 2f) : base() {
		this.name = name;
		this.defaultValue = defaultValue;
		this.min = min;
		this.max = max;
		this.defaultUrge = defaultUrge;
	}

	public override Trait Default(InheritContext context) {
		if (float.IsNaN(defaultValue) || context.IsVariation(this)) {
			return new ValueTrait(this, context.VariationTraitFloat(min, max, defaultValue, defaultUrge));
		} else {
			return new ValueTrait(this, defaultValue);
		}
	}
}

public class ValueTrait : Trait {
	public static ValueTrait Inherit(InheritContext context, ValueTrait father, ValueTrait mother) {
		var info = father.traitInfo;
		if (info != mother.traitInfo) {
			throw new ArgumentException("father & mother information is different");
		}

		if (context.IsVariation(info)) {
			return new ValueTrait(info, context.VariationTraitFloat(info.min, info.max, info.defaultValue, info.defaultUrge));
		} else {
			return new ValueTrait(info, context.InheritTraitBare(father.value, mother.value));
		}
	}


	public ValueTraitInfo traitInfo;
	public float value;

	public override TraitInfo info => traitInfo;


	public ValueTrait(ValueTraitInfo info, float value) : base() {
		this.traitInfo = info;
		this.value = value;
	}

	public override Trait InheritWith(InheritContext context, Trait other) {
		return Inherit(context, this, other as ValueTrait);
	}
}


[Serializable]
struct LightTrait {
	public enum TraitType { alleric, binary, value }

	[HideInInspector]
	public string name;

	public TraitType type;
	public float value1;
	public float value1min;
	public float value1max;
	public float value2;
}


[Serializable]
public class Genes {
	public static Genes InheritFrom(InheritContext inheritContext, Genes mother, Genes father) {
		var traits = new Trait[mother.traits.Length];	
		for(var i = 0; i < traits.Length; i++) {
			traits[i] = mother.traits[i].InheritWith(inheritContext, father.traits[i]);
		}

		return new Genes(traits);
	}


	private Dictionary<TraitInfo, Trait> traitMap;
	public Trait[] traits;

	[SerializeField]
	LightTrait[] lightTraits;
	[SerializeField]
	bool lightTraitsChanged;

	public Genes(Trait[] traits) {
		this.traits = traits;
		lightTraits = traits.Select(t => {
			switch (t) {
				case AllericTrait at:
					return new LightTrait {
						name = at.traitInfo.name,
						type = LightTrait.TraitType.alleric,
						value1 = at.a ? 1f : 0f,
						value1min = 0f,
						value1max = 1f,
						value2 = at.b ? 1f : 0f
					};

				case BinaryTrait bt:
					return new LightTrait {
						name = bt.traitInfo.name,
						type = LightTrait.TraitType.binary,
						value1 = bt.value ? 1f : 0f,
						value1min = 0f,
						value1max = 1f,
						value2 = bt.value ? 0f : 1f
					};

				case ValueTrait vt:
					return new LightTrait {
						name = vt.traitInfo.name,
						type = LightTrait.TraitType.value,
						value1 = vt.value,
						value1min = vt.traitInfo.min,
						value1max = vt.traitInfo.max,
						value2 = 0f
					};

				default: throw new NotImplementedException();
		}
		}).ToArray();
		traitMap = new Dictionary<TraitInfo, Trait>(capacity: traits.Length);

		foreach (var trait in traits) {
			traitMap[trait.info] = trait;
		}
	}

	public Genes(InheritContext inheritContext, TraitInfo[] infos) :
		this(infos.Select(info => info.Default(inheritContext)).ToArray()) { }


	public T Get<T>(TraitInfo info) where T : Trait {
		return traitMap[info] as T;
	}


	public void OnValidate() {
		if (lightTraitsChanged) {
			// change values in traits

			for (int i = 0; i < traits.Length; i++) {
				var trait = traits[i];
				var lightTrait = lightTraits[i];

				switch (trait) {
					case AllericTrait at:
						at.a = lightTrait.value1 > 0.2f;
						at.b = lightTrait.value2 > 0.2f;
						break;

					case BinaryTrait bt:
						bt.value = lightTrait.value1 > 0.2f;
						break;

					case ValueTrait vt:
						vt.value = lightTrait.value1;
						break;

					default: throw new NotImplementedException();
				}
			}
		}
		lightTraitsChanged = false;
	}
}
