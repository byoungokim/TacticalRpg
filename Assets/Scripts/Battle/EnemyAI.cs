using System.Collections.Generic;
using Tactics.Grid;
using Tactics.Units;
using UnityEngine;

namespace Tactics.Battle
{
    public class AiPlan
    {
        public Tile MoveTo;
        public Skill Skill;   // null = just move
        public Tile Target;
    }

    public static class EnemyAI
    {
        // Tries every reachable tile x skill x target tile and keeps the best-scoring action.
        // If nothing is in range, walks toward the closest opponent.
        public static AiPlan Decide(GridMap map, Unit self, List<Unit> units)
        {
            var reach = Pathfinding.Reachable(map, self);
            AiPlan best = null;
            float bestScore = 0f;

            foreach (var dest in reach.Destinations)
            foreach (var skill in self.Def.Skills)
            {
                if (skill.MpCost > self.Mp) continue;
                foreach (var center in map.AllTiles)
                {
                    if (!skill.InRange(dest, center)) continue;
                    float score = Score(self, dest, skill, center, units);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = new AiPlan { MoveTo = dest, Skill = skill, Target = center };
                    }
                }
            }

            return best ?? new AiPlan { MoveTo = ApproachTile(map, self, units, reach) };
        }

        static float Score(Unit self, Tile dest, Skill skill, Tile center, List<Unit> units)
        {
            float score = 0f;
            foreach (var u in units)
            {
                if (!skill.AffectsUnit(self, u)) continue;
                var uTile = u == self ? dest : u.Tile;
                if (GridMap.Distance(uTile, center) > skill.Radius) continue;

                var hit = DamageCalc.Compute(self, dest, u, skill, false);
                if (hit.IsHeal)
                {
                    int missing = u.Stats.MaxHp - u.Hp;
                    if (missing > u.Stats.MaxHp * 0.3f) score += Mathf.Min(hit.Amount, missing);
                }
                else
                {
                    score += hit.Amount + (hit.Amount >= u.Hp ? 30f : 0f);
                }
            }
            return score > 0f ? score - skill.MpCost * 0.2f : 0f;
        }

        static Tile ApproachTile(GridMap map, Unit self, List<Unit> units, ReachResult reach)
        {
            var full = Pathfinding.Reachable(map, self, 999);
            List<Tile> bestPath = null;

            foreach (var u in units)
            {
                if (!u.IsAlive || u.Team == self.Team) continue;
                foreach (var n in map.Neighbors(u.Tile))
                {
                    if (!full.Destinations.Contains(n)) continue;
                    var path = full.PathTo(n);
                    if (bestPath == null || path.Count < bestPath.Count) bestPath = path;
                }
            }
            if (bestPath == null) return self.Tile;

            // Walk as far along the path as this turn's movement allows.
            for (int i = Mathf.Min(self.Stats.Move, bestPath.Count - 1); i > 0; i--)
                if (reach.Destinations.Contains(bestPath[i])) return bestPath[i];
            return self.Tile;
        }
    }
}
