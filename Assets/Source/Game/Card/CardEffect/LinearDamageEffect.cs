using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Linear Damage Effect", menuName = "Card Effects/Linear Damage")]
public class LinearDamageEffect : CardEffect
{
    [SerializeField] private float Damage = 10f;

    public float CardDamage => Damage;

    public override void BuildActions(CardPlayContext context, BattleActionQueue queue)
    {
        if (context.Card == null || context.Card.Data == null)
        {
            return;
        }

        CardTargetingRule targetingRule = context.Card.Data.TargetingRule;

        if (targetingRule == null)
        {
            return;
        }

        List<Vector2Int> impactCells = targetingRule.GetImpactCells(context);

        queue.Enqueue(new FaceTargetAction(context.Caster, context.Target.CurrentPos));

        queue.Enqueue(new PlayAnimationAction(context.Caster, UnitAnimationType.Attack));

        foreach (Vector2Int cell in impactCells)
        {
            Unit unit = context.Board.GetOccupant(cell);

            if (unit == null || unit == context.Caster)
            {
                continue;
            }

            float finalDamage = context.DMGResolver.Resolve(context, Damage);
            queue.Enqueue(new DamageAction(unit, context.Caster, finalDamage));
        }

        queue.Enqueue(new WaitAnimationEndAction(context.Caster));
    }

}
