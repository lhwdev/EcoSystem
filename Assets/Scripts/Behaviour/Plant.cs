using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plant : LivingEntity {
    public static ValueTraitInfo initialAmountTrait = new ValueTraitInfo("initialAmount", defaultValue: 1f, min: 0.3f, max: 1.7f);

    public static TraitInfo[] PlantDefaultTraitInfos = LivingEntity.LivingEntityDefaultTraitInfos.ConcatArray(new TraitInfo[] {
        initialAmountTrait
    });


    float amountRemaining = 1;
    const float consumeSpeed = 8;


    public override Species species => Species.Plant;



    public override TraitInfo[] CreateTraitInfos() {
        return PlantDefaultTraitInfos;
    }

    public override void Init(Coord coord, Environment environment) {
        base.Init(coord, environment);
    }

    public float Consume (float amount) {
        float amountConsumed = Mathf.Max (0, Mathf.Min (amountRemaining, amount));
        amountRemaining -= amount * consumeSpeed;

        transform.localScale = Vector3.one * amountRemaining;

        if (amountRemaining <= 0) {
            Die (CauseOfDeath.Eaten);
        }

        return amountConsumed;
    }

    public float AmountRemaining {
        get {
            return amountRemaining;
        }
    }
}
