using Tactics.Grid;
using Tactics.Units;
using UnityEngine;

namespace Tactics.Battle
{
    public enum SkillKind { Physical, Magic, Heal }

    public class Skill
    {
        public string Name;
        public string Description;
        public SkillKind Kind;
        public int MpCost;
        public int MinRange = 1;
        public int MaxRange = 1;
        public int Radius;          // 0 = single tile, 1 = plus-shaped area, ...
        public float Power = 1f;
        public bool CutIn;          // play the camera close-up when used

        public bool TargetsAllies => Kind == SkillKind.Heal;

        public bool AffectsUnit(Unit user, Unit target) =>
            target != null && target.IsAlive &&
            (TargetsAllies ? target.Team == user.Team : target.Team != user.Team);

        public bool InRange(Tile from, Tile to)
        {
            if (to.Terrain == TerrainType.Wall) return false;
            int d = GridMap.Distance(from, to);
            if (d < MinRange || d > MaxRange) return false;
            // Melee can't reach up or down tall cliffs.
            return MaxRange > 1 || Mathf.Abs(from.Height - to.Height) <= 2;
        }
    }
}
