using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Range Target Rule", menuName = "Card Target Rules/Range")]
public class RangeTargetRule : CardTargetingRule
{
    [SerializeField] private int MaxRange = 1;

    public int MaxRangeValue => MaxRange;

    public override bool RequireTarget => true;

    public override List<Vector2Int> GetSelectableCells(CardPlayContext context)
    {
        List<Vector2Int> cells = new();

        Vector2Int origin = context.Caster.CurrentPos;

        for (int x = 0; x < context.Board.BoardWidth; x++)
        {
            for (int y = 0; y < context.Board.BoardHeight; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);

                int distance = Mathf.Abs(cell.x - origin.x)
                             + Mathf.Abs(cell.y - origin.y);

                if (distance <= MaxRange)
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

        int distance = Mathf.Abs(context.Caster.CurrentPos.x - context.Target.CurrentPos.x)
                     + Mathf.Abs(context.Caster.CurrentPos.y - context.Target.CurrentPos.y);

        return distance <= MaxRange;
    }
}