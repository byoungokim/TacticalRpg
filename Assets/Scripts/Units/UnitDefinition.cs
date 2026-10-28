using System.Collections.Generic;
using Tactics.Battle;
using UnityEngine;

namespace Tactics.Units
{
    public enum Team { Player, Enemy }

    [System.Serializable]
    public struct UnitStats
    {
        public int MaxHp, MaxMp, Atk, Def, Mag, Res, Spd, Move, Jump;
    }

    public class UnitDefinition
    {
        public string Name;
        public string ClassName;
        public UnitStats Stats;
        public Color BodyColor;
        public List<Skill> Skills = new List<Skill>();
    }
}
