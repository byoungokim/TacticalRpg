using System.Collections;
using System.Collections.Generic;
using Tactics.Grid;
using UnityEngine;

namespace Tactics.Units
{
    public class Unit : MonoBehaviour
    {
        const float StepDuration = 0.18f;

        public UnitDefinition Def { get; private set; }
        public Team Team { get; private set; }
        public int Hp { get; private set; }
        public int Mp { get; private set; }
        public Tile Tile { get; private set; }
        public Vector2Int Facing { get; private set; } = Vector2Int.up;
        public float Ct; // charge time; the unit acts when it reaches TurnOrder.Threshold

        public string Name => Def.Name;
        public UnitStats Stats => Def.Stats;
        public bool IsAlive => Hp > 0;

        Transform _body;

        public static Unit Create(UnitDefinition def, Team team, Tile tile, Vector2Int facing, Transform parent)
        {
            var root = new GameObject(def.Name);
            root.transform.SetParent(parent, false);
            var unit = root.AddComponent<Unit>();
            unit.Def = def;
            unit.Team = team;
            unit.Hp = def.Stats.MaxHp;
            unit.Mp = def.Stats.MaxMp;
            unit.BuildView();
            unit.PlaceAt(tile);
            unit.SetFacing(facing);
            return unit;
        }

        void BuildView()
        {
            _body = new GameObject("Body").transform;
            _body.SetParent(transform, false);

            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.transform.SetParent(_body, false);
            capsule.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            capsule.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            capsule.GetComponent<Renderer>().material.color = Def.BodyColor;

            // Visor shows which way the unit faces (matters for flank/back attacks).
            var visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(visor.GetComponent<Collider>());
            visor.transform.SetParent(_body, false);
            visor.transform.localPosition = new Vector3(0f, 0.72f, 0.22f);
            visor.transform.localScale = new Vector3(0.32f, 0.12f, 0.12f);
            visor.GetComponent<Renderer>().material.color = new Color(0.1f, 0.1f, 0.12f);

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            ring.transform.localScale = new Vector3(0.8f, 0.01f, 0.8f);
            ring.GetComponent<Renderer>().material.color =
                Team == Team.Player ? new Color(0.3f, 0.6f, 1f) : new Color(1f, 0.3f, 0.25f);
        }

        public void PlaceAt(Tile tile)
        {
            if (Tile != null && Tile.Occupant == this) Tile.Occupant = null;
            Tile = tile;
            tile.Occupant = this;
            transform.position = tile.TopCenter;
        }

        public void SetFacing(Vector2Int dir)
        {
            if (dir == Vector2Int.zero) return;
            Facing = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y)
                ? new Vector2Int((int)Mathf.Sign(dir.x), 0)
                : new Vector2Int(0, (int)Mathf.Sign(dir.y));
            _body.localRotation = Quaternion.LookRotation(new Vector3(Facing.x, 0f, Facing.y));
        }

        public void FaceToward(Tile target) => SetFacing(target.Coord - Tile.Coord);

        public IEnumerator MoveAlong(List<Tile> path)
        {
            for (int i = 1; i < path.Count; i++)
            {
                var from = path[i - 1];
                var to = path[i];
                SetFacing(to.Coord - from.Coord);
                Vector3 a = from.TopCenter, b = to.TopCenter;
                float hop = 0.12f + Mathf.Abs(to.Height - from.Height) * GridMap.StepHeight * 0.6f;
                for (float t = 0f; t < 1f; t += Time.deltaTime / StepDuration)
                {
                    var p = Vector3.Lerp(a, b, t);
                    p.y += Mathf.Sin(t * Mathf.PI) * hop;
                    transform.position = p;
                    yield return null;
                }
            }
            PlaceAt(path[path.Count - 1]);
        }

        public IEnumerator Lunge()
        {
            var offset = new Vector3(Facing.x, 0f, Facing.y) * 0.3f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.25f)
            {
                _body.localPosition = offset * Mathf.Sin(t * Mathf.PI);
                yield return null;
            }
            _body.localPosition = Vector3.zero;
        }

        public void SpendMp(int amount) => Mp = Mathf.Max(0, Mp - amount);

        public void Heal(int amount) => Hp = Mathf.Min(Stats.MaxHp, Hp + amount);

        public void ApplyDamage(int amount)
        {
            Hp = Mathf.Max(0, Hp - amount);
            if (IsAlive) return;
            if (Tile.Occupant == this) Tile.Occupant = null;
            gameObject.SetActive(false);
        }
    }
}
