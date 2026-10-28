# Embers of the Lattice (working title)

An original 3D tactical RPG built with Unity 6 (developed on 6000.6.7f1).

## Running it

1. Unity Hub → **Add → Add project from disk** → pick this folder.
2. Open the project, open any scene (the default empty one is fine), press **Play**.

The battle is built entirely from code by `GameBootstrap`, so no scene setup is needed.

## Controls

| Input | Action |
|---|---|
| Left click | Advance dialogue, move to a blue tile, pick a target |
| Right click / Esc | Cancel targeting |
| Q / E | Rotate camera 90° |
| WASD / arrows | Pan camera |
| Mouse wheel | Zoom |

## Rules in this slice

- **Turn order** is speed-based: each unit gains CT equal to its SPD every tick and acts at 100.
- On its turn a unit can **move once** and **act once**, in either order, then the turn ends.
- **Height**: climbing is limited by JMP; attacking from higher ground gives +10% damage per step (max ±30%).
- **Facing**: physical attacks from the side get +15%, from behind +30%. Units face the way they last moved or attacked.
- Melee can't hit targets more than 2 steps above or below.

## Code layout

```
Assets/Scripts/
  Core/    GameBootstrap (entry point), InputShim (old/new input)
  Grid/    GridMap, Tile, Pathfinding
  Units/   Unit, UnitDefinition
  Battle/  BattleManager (state machine), TurnOrder, DamageCalc, Skill, EnemyAI
  View/    CameraRig
  UI/      BattleHUD (IMGUI)
  Story/   DialogueLine
  Data/    BattleSetup, Chapter1 (map, units, dialogue)
```

See [Docs/GameDesign.md](Docs/GameDesign.md) for the world and cast.
