using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageResolver
{
    public float Resolve(CardPlayContext context, float baseDamage)
    {
        if (Pause.OneHitEnabled && context.Caster is Player)
        {
            return 999999f;
        }

        int maxStamina = context.BattleState.MaxStamina;

        int currentStamina = context.BattleState.CurrentStamina;

        float correction = currentStamina / (float)maxStamina;

        if (correction == 1) return baseDamage * 1.2f;
        else if (correction >= .75f) return baseDamage * 1.1f;
        else if (correction >= .5f) return baseDamage;
        else if (correction >= .25f) return baseDamage * .9f;
        else return baseDamage * .8f;
    }
}
