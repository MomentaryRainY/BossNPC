using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Enemy AI/Chicken")]
public class ChickenAI : EnemyAI
{
    public override void BuildActions(Enemy enemy, Unit player, Board board, BattleActionQueue queue, HashSet<Vector2Int> reservedCells)
    {
        // scared chicken run randomly
        Vector2Int target = new Vector2Int(Random.Range(0, board.BoardWidth), Random.Range(0, board.BoardHeight));
        while(board.IsOccupied(target))
        {
            target = new Vector2Int(Random.Range(0, board.BoardWidth), Random.Range(0, board.BoardHeight));
        }

        queue.Enqueue(new FaceTargetAction(enemy, target));
        queue.Enqueue(new MoveAction(board, enemy, target));

    }
}
