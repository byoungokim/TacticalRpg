using Tactics.Grid;
using Tactics.Units;
using UnityEngine;

namespace Tactics.Battle
{
    public struct HitResult
    {
        public int Amount;
        public bool IsHeal;
        public string Tag; // "Back", "Flank" or null
    }

    public static class DamageCalc
    {
        const float OffenseScale = 1.5f;
        const float DefenseScale = 0.75f;

        // userTile is passed separately so the AI can evaluate tiles it hasn't moved to yet.
        public static HitResult Compute(Unit user, Tile userTile, Unit target, Skill skill, bool randomize)
        {
            if (skill.Kind == SkillKind.Heal)
                return new HitResult { Amount = Mathf.RoundToInt(user.Stats.Mag * skill.Power * OffenseScale), IsHeal = true };

            float raw = skill.Kind == SkillKind.Physical
                ? user.Stats.Atk * skill.Power * OffenseScale - target.Stats.Def * DefenseScale
                : user.Stats.Mag * skill.Power * OffenseScale - target.Stats.Res * DefenseScale;

            // Higher ground: +10% per step, capped at +/-30%.
            float mult = 1f + Mathf.Clamp((userTile.Height - target.Tile.Height) * 0.1f, -0.3f, 0.3f);

            string tag = null;
            if (skill.Kind == SkillKind.Physical)
            {
                var from = userTile.Coord - target.Tile.Coord;
                var dir = Mathf.Abs(from.x) >= Mathf.Abs(from.y)
                    ? new Vector2Int((int)Mathf.Sign(from.x), 0)
                    : new Vector2Int(0, (int)Mathf.Sign(from.y));
                if (dir == -target.Facing) { mult += 0.3f; tag = "Back"; }
                else if (dir != target.Facing) { mult += 0.15f; tag = "Flank"; }
            }

            if (randomize) mult *= Random.Range(0.9f, 1.1f);
            return new HitResult { Amount = Mathf.Max(1, Mathf.RoundToInt(raw * mult)), Tag = tag };
        }
    }
}
