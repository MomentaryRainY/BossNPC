using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Linear Range Target Rule", menuName = "Card Target Rules/Linear Range")]
public class LinearRangeTargetRule : CardTargetingRule
{
    [SerializeField] private int MaxRange = 1;

    public int MaxRangeValue => MaxRange;

    public override bool RequireTarget => true;

    public override List<Vector2Int> GetSelectableCells(CardPlayContext context)
    {
        List<Vector2Int> cells = new();

        if (context.Caster == null || context.Board == null)
        {
            return cells;
        }

        Vector2Int origin = context.Caster.CurrentPos;

        for (int x = 0; x < context.Board.BoardWidth; x++)
        {
            for (int y = 0; y < context.Board.BoardHeight; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);

                if (IsCellInLinearRange(origin, cell))
                {
                    cells.Add(cell);
                }
            }
        }

        return cells;
    }

    public override bool IsValidTarget(CardPlayContext context)
    {
        if (context.Caster == null || context.Target == null)
        {
            return false;
        }

        return IsCellInLinearRange(context.Caster.CurrentPos, context.Target.CurrentPos);
    }

    private bool IsCellInLinearRange(Vector2Int origin, Vector2Int cell)
    {
        Vector2Int delta = cell - origin;

        int dx = Mathf.Abs(delta.x);
        int dy = Mathf.Abs(delta.y);

        if (dx == 0 && dy == 0)
        {
            return false;
        }

        bool sameRowOrColumn = dx == 0 || dy == 0;
        int distance = dx + dy;

        return sameRowOrColumn && distance <= MaxRange;
    }

    public override List<Vector2Int> GetImpactCells(CardPlayContext context)
    {
        List<Vector2Int> cells = new();

        if (context.Caster == null || context.Target == null || context.Board == null)
        {
            return cells;
        }

        Vector2Int origin = context.Caster.CurrentPos;
        Vector2Int target = context.Target.CurrentPos;

        Vector2Int direction = GetDirection(origin, target);

        if (direction == Vector2Int.zero)
        {
            return cells;
        }

        for (int i = 1; i <= MaxRange; i++)
        {
            Vector2Int cell = origin + direction * i;

            if (!context.Board.IsInside(cell))
            {
                break;
            }

            cells.Add(cell);
        }

        return cells;
    }

    private Vector2Int GetDirection(Vector2Int origin, Vector2Int target)
    {
        Vector2Int delta = target - origin;

        if (delta.x != 0 && delta.y != 0)
        {
            return Vector2Int.zero;
        }

        if (delta.x > 0) return Vector2Int.right;
        if (delta.x < 0) return Vector2Int.left;
        if (delta.y > 0) return Vector2Int.up;
        if (delta.y < 0) return Vector2Int.down;

        return Vector2Int.zero;
    }
}
