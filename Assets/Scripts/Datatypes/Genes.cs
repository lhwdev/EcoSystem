﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public struct InheritContext {
	public System.Random random;
	public float variationRate;


	private bool NextBool() {
		return random.Next(0, 1) == 1;
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

	public float VariationTraitFloat(float min, float max) {
		return Mathf.Lerp(min, max, (float)random.NextDouble());
	}
}


public abstract class Trait {
	public abstract TraitInfo info { get; }

	public abstract Trait InheritWith(InheritContext context, Trait other);
}

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
			if(traitInfo.isDominanceTrait) {
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
	public float min;
	public float max;
	public float defaultValue;


	public ValueTraitInfo(String name, float defaultValue, float min, float max) : base() {
		this.name = name;
		this.defaultValue = defaultValue;
		this.min = min;
		this.max = max;
	}

	public override Trait Default(InheritContext context) {
		if (float.IsNaN(defaultValue) || context.IsVariation(this)) {
			return new ValueTrait(this, context.VariationTraitFloat(min, max));
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
			return new ValueTrait(info, context.VariationTraitFloat(info.min, info.max));
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


public class Genes {
	private Dictionary<TraitInfo, Trait> traitMap;
	public Trait[] traits;

	public Genes(Trait[] traits) {
		this.traits = traits;
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
}
