using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyAI : ScriptableObject
{
    public abstract void BuildActions(Enemy enemy, Unit player, Board board, BattleActionQueue queue, HashSet<Vector2Int> reservedCells);
}
