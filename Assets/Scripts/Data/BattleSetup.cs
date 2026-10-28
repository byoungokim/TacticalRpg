using System.Collections.Generic;
using Tactics.Story;
using Tactics.Units;
using UnityEngine;

namespace Tactics.Data
{
    public class UnitSpawn
    {
        public UnitDefinition Def;
        public Team Team;
        public Vector2Int Coord;
        public Vector2Int Facing;
    }

    public class BattleSetup
    {
        public string Title;
        public string[] MapRows;
        public List<UnitSpawn> Spawns = new List<UnitSpawn>();
        public List<DialogueLine> Intro = new List<DialogueLine>();
        public List<DialogueLine> Victory = new List<DialogueLine>();
        public List<DialogueLine> Defeat = new List<DialogueLine>();
    }
}
