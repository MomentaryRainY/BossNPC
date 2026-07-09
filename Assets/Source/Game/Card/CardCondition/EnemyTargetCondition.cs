using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Enemy Target Condition", menuName = "Card Conditions/Enemy Target")]
public class EnemyTargetCondition : CardCondition
{
    public override bool IsSatisfied(CardPlayContext context)
    {
        return context.Target is Enemy;
    }
}