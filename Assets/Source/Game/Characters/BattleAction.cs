using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BattleAction
{
    public abstract IEnumerator Execute();
}

public class BattleActionQueue
{
    private readonly Queue<BattleAction> Actions = new Queue<BattleAction>();

    public bool HasActions => Actions.Count > 0;

    public void Enqueue(BattleAction action)
    {
        Actions.Enqueue(action);
    }

    public IEnumerator Execute()
    {
        while (Actions.Count > 0)
        {
            yield return Actions.Dequeue().Execute();
        }
    }
}

public class MoveAction : BattleAction
{
    private Unit Unit;
    private Vector2Int TargetCell;
    private Board Board;

    public MoveAction(Board board, Unit unit, Vector2Int targetCell)
    {
        this.Unit = unit;
        this.TargetCell = targetCell;
        this.Board = board;
    }

    public override IEnumerator Execute()
    {
        yield return Unit.MoveTo(Board, TargetCell);
    }
}

public class DamageAction : BattleAction
{
    private Unit target;
    private Unit source;
    private float damage;

    public DamageAction(Unit target, Unit source, float damage)
    {
        this.target = target;
        this.source = source;
        this.damage = damage;
    }

    public override IEnumerator Execute()
    {
        if (target == null)
        {
            yield break;
        }

        if (source != null)
        {
            yield return target.FaceCell(source.CurrentPos);
        }

        yield return target.TakeDamage(damage);
    }
}

public class PlayAnimationAction : BattleAction
{
    private Unit unit;
    private UnitAnimationType animationType;

    public PlayAnimationAction(Unit unit, UnitAnimationType animationType)
    {
        this.unit = unit;
        this.animationType = animationType;
    }

    public override IEnumerator Execute()
    {
        switch (animationType)
        {
            case UnitAnimationType.Attack:
                yield return unit.PlayAttackAnimation();
                break;
        }
    }
}

public class WaitAnimationEndAction : BattleAction
{
    private Unit unit;

    public WaitAnimationEndAction(Unit unit)
    {
        this.unit = unit;
    }

    public override IEnumerator Execute()
    {
        yield return unit.WaitAttackEnd();
    }
}

public class FaceTargetAction : BattleAction
{
    private Unit unit;
    private Vector2Int target;

    public FaceTargetAction(Unit unit, Vector2Int targetCell)
    {
        this.unit = unit;
        this.target = targetCell;
    }

    public override IEnumerator Execute()
    {
        if (unit == null)
        {
            yield break;
        }

        yield return unit.FaceCell(target);
    }
}

public enum UnitAnimationType
{
    Attack
}