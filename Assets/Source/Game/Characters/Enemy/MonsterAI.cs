using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Enemy AI/Monster")]
public class MonsterAI : EnemyAI
{
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

        List<Vector2Int> bestpath = FindBestPath(enemy, player, board, reservedCells);

        Vector2Int finalCell = bestpath.Count > 0 ? bestpath[0] : enemy.CurrentPos;

        if (finalCell != enemy.CurrentPos)
        {
            reservedCells.Remove(enemy.CurrentPos);
            reservedCells.Add(finalCell);

            queue.Enqueue(new MoveAction(board, enemy, bestpath));
            queue.Enqueue(new FaceTargetAction(enemy, player.CurrentPos));
        }

        if (CanAttack(finalCell, player.CurrentPos))
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
        int distance = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);

        return distance == 1;
    }

    private List<Vector2Int> FindBestPath(Enemy enemy, Unit player, Board board, HashSet<Vector2Int> reservedCells)
    {
        Vector2Int bestCell = enemy.CurrentPos;
        int bestScore = int.MinValue;

        PathSearchResult res = GetReachableDistances(board, enemy.CurrentPos, enemy.MoveRange);

        foreach (var pair in res.Distance)
        {
            Vector2Int cell = pair.Key;
            int moveCost = pair.Value;

            if (cell == enemy.CurrentPos)
            {
                continue;
            }

            if (!CanStandOn(cell, enemy, board, reservedCells))
            {
                continue;
            }

            int distanceToPlayer =
                Mathf.Abs(cell.x - player.CurrentPos.x) +
                Mathf.Abs(cell.y - player.CurrentPos.y);

            int score = 0;

            if (distanceToPlayer == 1)
            {
                score += 100;
            }

            score -= distanceToPlayer * 10;
            score -= moveCost;

            if (score > bestScore)
            {
                bestScore = score;
                bestCell = cell;
            }
        }

        List<Vector2Int> path = new();
        Vector2Int t = new Vector2Int(bestCell.x, bestCell.y);
        while (!(t.x == enemy.CurrentPos.x && t.y == enemy.CurrentPos.y))
        {
            path.Add(t);
            t = res.Previous[t];
        }

        return path;
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

        if (!board.IsWalkable(cell)) return false;

        if (reservedCells.Contains(cell))
        {
            return false;
        }

        return !board.IsOccupied(cell);
    }
}
