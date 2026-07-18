using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyAI : ScriptableObject
{
    public abstract void BuildActions(Enemy enemy, Unit player, Board board, BattleActionQueue queue, HashSet<Vector2Int> reservedCells);

    public PathSearchResult GetReachableDistances(Board board, Vector2Int start, int range)
    {
        Dictionary<Vector2Int, int> dist = new();
        Dictionary<Vector2Int, Vector2Int> previous = new();
        Queue<Vector2Int> queue = new();

        dist[start] = 0;
        queue.Enqueue(start);

        Vector2Int[] dirs = {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            if (dist[current] >= range)
            {
                continue;
            }

            foreach (Vector2Int dir in dirs)
            {
                Vector2Int next = current + dir;
                if (dist.ContainsKey(next))
                {
                    continue;
                }

                if (!board.IsWalkable(next))
                {
                    continue;
                }

                if (board.IsOccupied(next) && next != start)
                {
                    continue;
                }

                dist[next] = dist[current] + 1;
                previous[next] = current;
                queue.Enqueue(next);
            }
        }

        return new PathSearchResult(dist, previous);
    }
}

public class PathSearchResult
{
    public Dictionary<Vector2Int, int> Distance { get; }
    public Dictionary<Vector2Int, Vector2Int> Previous { get; }

    public PathSearchResult(Dictionary<Vector2Int, int> dist, Dictionary<Vector2Int, Vector2Int> previous)
    {
        Distance = dist;
        Previous = previous;
    }
}