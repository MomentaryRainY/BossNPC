using System.Collections.Generic;
using UnityEngine;

public sealed class BattleActionResult
{
    public Unit Source { get; }
    public Unit PrimaryTarget { get; }
    public CardInstance Card { get; }

    public float TotalDamageDealt { get; private set; }
    public bool IsPlayerAction { get; }

    private readonly Dictionary<Unit, float> damageByTarget = new();

    public BattleActionResult(
        Unit source,
        Unit primaryTarget,
        CardInstance card,
        bool isPlayerAction)
    {
        Source = source;
        PrimaryTarget = primaryTarget;
        Card = card;
        IsPlayerAction = isPlayerAction;
    }

    public void RecordDamage(Unit target, float hpBefore, float hpAfter)
    {
        if (target == null)
        {
            return;
        }

        float actualDamage = Mathf.Max(0f, hpBefore - Mathf.Max(0f, hpAfter));
        TotalDamageDealt += actualDamage;

        if (damageByTarget.ContainsKey(target))
        {
            damageByTarget[target] += actualDamage;
        }
        else
        {
            damageByTarget[target] = actualDamage;
        }
    }

    public float GetDamageDealtTo(Unit target)
    {
        return target != null && damageByTarget.TryGetValue(target, out float damage)
            ? damage
            : 0f;
    }
}
