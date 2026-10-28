using System.Collections.Generic;
using Tactics.Units;
using UnityEngine;

namespace Tactics.Grid
{
    public class ReachResult
    {
        public readonly Dictionary<Tile, Tile> Prev = new Dictionary<Tile, Tile>();
        public readonly List<Tile> Destinations = new List<Tile>();

        // Path from the start tile to dest, inclusive of both.
        public List<Tile> PathTo(Tile dest)
        {
            var path = new List<Tile>();
            for (var t = dest; t != null; t = Prev[t]) path.Add(t);
            path.Reverse();
            return path;
        }
    }

    public static class Pathfinding
    {
        public static ReachResult Reachable(GridMap map, Unit unit) =>
            Reachable(map, unit, unit.Stats.Move);

        // Breadth-first search: every step costs 1, climbing is limited by Jump,
        // units can pass through allies but not stop on them, and enemies block.
        public static ReachResult Reachable(GridMap map, Unit unit, int move)
        {
            var result = new ReachResult();
            var start = unit.Tile;
            var dist = new Dictionary<Tile, int> { [start] = 0 };
            result.Prev[start] = null;
            var queue = new Queue<Tile>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (cur.Occupant == null || cur.Occupant == unit) result.Destinations.Add(cur);
                if (dist[cur] >= move) continue;

                foreach (var n in map.Neighbors(cur))
                {
                    if (dist.ContainsKey(n) || !n.Walkable) continue;
                    if (Mathf.Abs(n.Height - cur.Height) > unit.Stats.Jump) continue;
                    if (n.Occupant != null && n.Occupant.Team != unit.Team) continue;
                    dist[n] = dist[cur] + 1;
                    result.Prev[n] = cur;
                    queue.Enqueue(n);
                }
            }
            return result;
        }
    }
}
