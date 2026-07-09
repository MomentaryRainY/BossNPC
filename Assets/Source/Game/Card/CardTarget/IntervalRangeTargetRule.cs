using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntervalRangeTargetRule : CardTargetingRule
{
    [SerializeField] private int Interval = 3;
    public int IntervalValue => Interval;
    public override bool RequireTarget => true;

    public override List<Vector2Int> GetSelectableCells(CardPlayContext context)
    {
        List<Vector2Int> targetCells = new List<Vector2Int>();
        Vector2Int origin = context.Caster.CurrentPos;

        if(origin.y + Interval < context.Board.BoardHeight)
            targetCells.Add(new Vector2Int(origin.x, origin.y + Interval));
        if(origin.x + Interval < context.Board.BoardWidth)
            targetCells.Add(new Vector2Int(origin.x + Interval, origin.y));
        if(origin.x - Interval >= 0)
            targetCells.Add(new Vector2Int(origin.x - Interval, origin.y));
        if(origin.y - Interval >= 0)
            targetCells.Add(new Vector2Int(origin.x, origin.y - Interval));

        return targetCells;
    }

    public override bool IsValidTarget(CardPlayContext context)
    {
        Vector2Int origin = context.Caster.CurrentPos;
        Vector2Int target = context.Target.CurrentPos;

        Vector2Int direction = target - origin;

        bool sameLine = direction.x == 0 || direction.y == 0;
        int distance = Mathf.Abs(direction.x) + Mathf.Abs(direction.y);

        return sameLine && distance == Interval;
    }

}
