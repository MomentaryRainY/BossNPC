using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Square Damage Effect", menuName = "Card Effects/Square Damage")]
public class SquareDamageEffect : CardEffect
{
    [SerializeField] private float Damage = 10f;

    public float CardDamage => Damage;
    public override void BuildActions(CardPlayContext context, BattleActionQueue queue)
    {
        Vector2Int center = context.Target.CurrentPos;

        queue.Enqueue(new FaceTargetAction(context.Caster, context.Target.CurrentPos));

        queue.Enqueue(new PlayAnimationAction(context.Caster, UnitAnimationType.Attack));

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int cell = center + new Vector2Int(x, y);

                if (!context.Board.IsInside(cell))
                {
                    continue;
                }

                Unit unit = context.Board.GetOccupant(cell);

                if (unit == null || unit == context.Caster)
                {
                    continue;
                }

                float finalDamage = context.DMGResolver.Resolve(context, Damage);
                queue.Enqueue(new DamageAction(unit, context.Caster, finalDamage));
            }
        }

        queue.Enqueue(new WaitAnimationEndAction(context.Caster));
    }
}
