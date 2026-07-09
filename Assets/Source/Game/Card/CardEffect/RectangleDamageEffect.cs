using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Rectangle Damage Effect", menuName = "Card Effects/Rectangle Damage")]
public class RectangleDamageEffect : CardEffect
{
    [SerializeField] private float Damage = 10f;

    public float CardDamage => Damage;

    public override void BuildActions(CardPlayContext context, BattleActionQueue queue)
    {
        if (context.Card.Data.TargetingRule == null) return;

        List<Vector2Int> impactCells = GetRectangleCells(context);

        queue.Enqueue(new FaceTargetAction(context.Caster, context.Target.CurrentPos));

        queue.Enqueue(new PlayAnimationAction(context.Caster, UnitAnimationType.Attack));

        foreach (Vector2Int cell in impactCells)
        {
            Unit unit = context.Board.GetOccupant(cell);

            if (unit == null) continue;

            float finalDamage = context.DMGResolver.Resolve(context, Damage);

            queue.Enqueue(new DamageAction(unit, context.Caster, finalDamage));
        }

        queue.Enqueue(new WaitAnimationEndAction(context.Caster));
    }
    private List<Vector2Int> GetRectangleCells(CardPlayContext context)
    {
        List<Vector2Int> cells = new();

        Vector2Int origin = context.Caster.CurrentPos;
        Vector2Int target = context.Target.CurrentPos;

        Vector2Int direction = target - origin;

        if (Mathf.Abs(direction.x) + Mathf.Abs(direction.y) != 1)
        {
            return cells;
        }

        Vector2Int side = new Vector2Int(-direction.y, direction.x);

        int depth = 2;

        for (int forward = 1; forward <= depth; forward++)
        {
            for (int sideOffset = -1; sideOffset <= 1; sideOffset++)
            {
                Vector2Int cell = origin + direction * forward + side * sideOffset;
                if (!context.Board.IsInside(cell))
                {
                    continue;
                }

                cells.Add(cell);
            }
        }

        return cells;
    }
}
