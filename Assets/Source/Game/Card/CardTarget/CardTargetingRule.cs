using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CardTargetingRule : ScriptableObject
{
    public abstract bool RequireTarget { get; }

    public abstract List<Vector2Int> GetSelectableCells(CardPlayContext context);

    public abstract bool IsValidTarget(CardPlayContext context);

    public virtual List<Vector2Int> GetImpactCells(CardPlayContext context)
    {
        if (context.Target != null)
        {
            return new List<Vector2Int> { context.Target.CurrentPos };
        }
        /*
        if (context.TargetCell.HasValue)
        {
            return new List<Vector2Int> { context.TargetCell.Value };
        }*/

        return new List<Vector2Int>();
    }
}
