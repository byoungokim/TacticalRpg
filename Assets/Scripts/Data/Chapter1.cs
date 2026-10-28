using System.Collections.Generic;
using Tactics.Battle;
using Tactics.Story;
using Tactics.Units;
using UnityEngine;

namespace Tactics.Data
{
    // Prologue battle: the party holds a river crossing against a Synod patrol.
    public static class Chapter1
    {
        public static BattleSetup Create()
        {
            var setup = new BattleSetup
            {
                Title = "Prologue: The Ashford Crossing",
                // Row 0 is the far (enemy) side. See GridMap.Build for the legend.
                MapRows = new[]
                {
                    "332110012333",
                    "321100001233",
                    "211000000122",
                    "1100#00#0011",
                    "~~~~~BB~~~0~",
                    "~~~~~BB~~~0~",
                    "001100000011",
                    "012210000110",
                    "0123#1001210",
                    "001221001100",
                    "000110000000",
                    "000000000000",
                },
            };

            var up = Vector2Int.up;
            var down = Vector2Int.down;
            Spawn(setup, Rhen(), Team.Player, 6, 1, up);
            Spawn(setup, Isolde(), Team.Player, 5, 0, up);
            Spawn(setup, Tobin(), Team.Player, 3, 1, up);
            Spawn(setup, Warden("Synod Warden A"), Team.Enemy, 5, 9, down);
            Spawn(setup, Warden("Synod Warden B"), Team.Enemy, 7, 9, down);
            Spawn(setup, Arbalist(), Team.Enemy, 2, 10, down);
            Spawn(setup, Cantor(), Team.Enemy, 9, 10, down);

            setup.Intro.AddRange(new[]
            {
                new DialogueLine("", "The Iron Synod outlawed the old-world engines a century ago. Yet in the canyons of Veyra, their ember cores still burn."),
                new DialogueLine("Tobin", "Bridge ahead. And company. Synod colors, four of them."),
                new DialogueLine("Isolde", "They're tracking the core's resonance. We can't outrun them while it hums like this."),
                new DialogueLine("Rhen", "Then we don't run. Take the crossing, break their line, and we're through by nightfall."),
                new DialogueLine("Synod Warden A", "By decree of the Synod, surrender the relic and kneel!"),
            });
            setup.Victory.AddRange(new[]
            {
                new DialogueLine("Rhen", "That's the last of them. Isolde, the core?"),
                new DialogueLine("Isolde", "Still stable. But their cantor sang a signal before she fell. More will come."),
                new DialogueLine("Tobin", "Then let's not be standing here when they do."),
                new DialogueLine("", "End of Prologue."),
            });
            setup.Defeat.AddRange(new[]
            {
                new DialogueLine("", "The core is taken, and the last embers of the old world go dark..."),
            });
            return setup;
        }

        static void Spawn(BattleSetup setup, UnitDefinition def, Team team, int x, int z, Vector2Int facing) =>
            setup.Spawns.Add(new UnitSpawn { Def = def, Team = team, Coord = new Vector2Int(x, z), Facing = facing });

        // ---------------------------------------------------------------- skills

        static Skill Strike(string name = "Attack", float power = 1f) => new Skill
        {
            Name = name, Kind = SkillKind.Physical, Power = power,
            Description = "A basic melee attack. Strike from the side or back for bonus damage.",
        };

        static Skill Shot(string name, float power, int maxRange) => new Skill
        {
            Name = name, Kind = SkillKind.Physical, Power = power, MinRange = 2, MaxRange = maxRange,
            Description = "A ranged attack. Can't hit adjacent enemies.",
        };

        // ---------------------------------------------------------------- party

        static UnitDefinition Rhen() => new UnitDefinition
        {
            Name = "Rhen", ClassName = "Lattice Knight", BodyColor = new Color(0.35f, 0.5f, 0.85f),
            Stats = new UnitStats { MaxHp = 62, MaxMp = 20, Atk = 16, Def = 11, Mag = 6, Res = 7, Spd = 9, Move = 4, Jump = 2 },
            Skills =
            {
                Strike(),
                new Skill
                {
                    Name = "Rift Cleave", Kind = SkillKind.Physical, MpCost = 8, Power = 1.3f, Radius = 1, CutIn = true,
                    Description = "An engine-charged sweep that hits the target and every enemy next to it.",
                },
            },
        };

        static UnitDefinition Isolde() => new UnitDefinition
        {
            Name = "Isolde", ClassName = "Resonant", BodyColor = new Color(0.85f, 0.85f, 0.95f),
            Stats = new UnitStats { MaxHp = 44, MaxMp = 40, Atk = 7, Def = 6, Mag = 17, Res = 12, Spd = 10, Move = 4, Jump = 1 },
            Skills =
            {
                Strike("Staff", 0.7f),
                new Skill
                {
                    Name = "Ember Lance", Kind = SkillKind.Magic, MpCost = 10, Power = 1.1f, MinRange = 2, MaxRange = 4, Radius = 1, CutIn = true,
                    Description = "Hurls a lance of core fire that bursts over a small area. Ignores facing.",
                },
                new Skill
                {
                    Name = "Mend", Kind = SkillKind.Heal, MpCost = 6, Power = 1f, MinRange = 0, MaxRange = 3,
                    Description = "Restores HP to one ally (or herself).",
                },
            },
        };

        static UnitDefinition Tobin() => new UnitDefinition
        {
            Name = "Tobin", ClassName = "Coilgunner", BodyColor = new Color(0.75f, 0.55f, 0.3f),
            Stats = new UnitStats { MaxHp = 50, MaxMp = 18, Atk = 14, Def = 8, Mag = 5, Res = 6, Spd = 11, Move = 5, Jump = 3 },
            Skills =
            {
                Shot("Coil Shot", 0.9f, 4),
                new Skill
                {
                    Name = "Piercing Round", Kind = SkillKind.Physical, MpCost = 6, Power = 1.4f, MinRange = 2, MaxRange = 5, CutIn = true,
                    Description = "An overcharged round with extra reach. Shoot from high ground for more damage.",
                },
            },
        };

        // ---------------------------------------------------------------- Synod

        static UnitDefinition Warden(string name) => new UnitDefinition
        {
            Name = name, ClassName = "Synod Warden", BodyColor = new Color(0.55f, 0.55f, 0.58f),
            Stats = new UnitStats { MaxHp = 55, MaxMp = 12, Atk = 14, Def = 10, Mag = 3, Res = 5, Spd = 8, Move = 4, Jump = 2 },
            Skills =
            {
                Strike(),
                new Skill { Name = "Shield Bash", Kind = SkillKind.Physical, MpCost = 4, Power = 1.2f, Description = "A heavy blow with a tower shield." },
            },
        };

        static UnitDefinition Arbalist() => new UnitDefinition
        {
            Name = "Synod Arbalist", ClassName = "Synod Arbalist", BodyColor = new Color(0.45f, 0.4f, 0.35f),
            Stats = new UnitStats { MaxHp = 42, MaxMp = 0, Atk = 13, Def = 6, Mag = 3, Res = 6, Spd = 10, Move = 4, Jump = 2 },
            Skills = { Shot("Bolt", 0.9f, 4) },
        };

        static UnitDefinition Cantor() => new UnitDefinition
        {
            Name = "Synod Cantor", ClassName = "Synod Cantor", BodyColor = new Color(0.5f, 0.3f, 0.55f),
            Stats = new UnitStats { MaxHp = 40, MaxMp = 30, Atk = 6, Def = 5, Mag = 15, Res = 11, Spd = 9, Move = 3, Jump = 1 },
            Skills =
            {
                Strike("Rod", 0.7f),
                new Skill
                {
                    Name = "Static Hymn", Kind = SkillKind.Magic, MpCost = 8, Power = 0.9f, MinRange = 2, MaxRange = 4, Radius = 1, CutIn = true,
                    Description = "A droning chant that crackles over an area.",
                },
            },
        };
    }
}
