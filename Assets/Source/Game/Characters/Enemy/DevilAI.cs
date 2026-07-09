using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Enemy AI/Devil")]
public class DevilAI : EnemyAI
{
    [SerializeField] private int PreferredDistance = 3;
    [SerializeField] private float Damage = 10f;

    public override void BuildActions(Enemy enemy, Unit player, Board board, BattleActionQueue queue, HashSet<Vector2Int> reservedCells)
    {
        if (enemy == null || player == null || board == null)
        {
            return;
        }

        if (CanAttack(enemy.CurrentPos, player.CurrentPos))
        {
            EnqueueAttack(enemy, player, queue);
            return;
        }

        Vector2Int bestCell = FindBestCell(enemy, player, board, reservedCells);

        if (bestCell != enemy.CurrentPos)
        {
            reservedCells.Remove(enemy.CurrentPos);
            reservedCells.Add(bestCell);

            queue.Enqueue(new MoveAction(board, enemy, bestCell));
            queue.Enqueue(new FaceTargetAction(enemy, player.CurrentPos));
        }

        if (CanAttack(bestCell, player.CurrentPos))
        {
            EnqueueAttack(enemy, player, queue);
        }
    }

    private void EnqueueAttack(Enemy enemy, Unit player, BattleActionQueue queue)
    {
        queue.Enqueue(new FaceTargetAction(enemy, player.CurrentPos));
        queue.Enqueue(new PlayAnimationAction(enemy, UnitAnimationType.Attack));
        queue.Enqueue(new DamageAction(player, enemy, Damage));
        queue.Enqueue(new WaitAnimationEndAction(enemy));
    }

    private bool CanAttack(Vector2Int from, Vector2Int target)
    {
        Vector2Int delta = target - from;

        bool sameLine = delta.x == 0 || delta.y == 0;
        int distance = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);

        return sameLine && distance == PreferredDistance;
    }

    private Vector2Int FindBestCell(Enemy enemy, Unit player, Board board, HashSet<Vector2Int> reservedCells)
    {
        Vector2Int bestCell = enemy.CurrentPos;
        int bestScore = int.MinValue;

        Vector2Int origin = enemy.CurrentPos;
        int moveRange = enemy.MoveRange;

        for (int x = 0; x < board.BoardWidth; x++)
        {
            for (int y = 0; y < board.BoardHeight; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);

                if (!CanStandOn(cell, enemy, board, reservedCells))
                {
                    continue;
                }

                int moveDistance = Mathf.Abs(cell.x - origin.x)
                                 + Mathf.Abs(cell.y - origin.y);

                if (moveDistance > moveRange)
                {
                    continue;
                }

                int distanceToPlayer = Mathf.Abs(cell.x - player.CurrentPos.x)
                                     + Mathf.Abs(cell.y - player.CurrentPos.y);

                bool canAttackFromCell = CanAttack(cell, player.CurrentPos);

                int score = 0;

                score -= Mathf.Abs(distanceToPlayer - PreferredDistance) * 20;

                if (canAttackFromCell)
                {
                    score += 100;
                }

                score -= moveDistance;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cell;
                }
            }
        }

        return bestCell;
    }

    private bool CanStandOn(Vector2Int cell, Enemy enemy, Board board, HashSet<Vector2Int> reservedCells)
    {
        if (!board.IsInside(cell))
        {
            return false;
        }

        if (cell == enemy.CurrentPos)
        {
            return true;
        }

        if (reservedCells.Contains(cell))
        {
            return false;
        }

        return !board.IsOccupied(cell);
    }
}
