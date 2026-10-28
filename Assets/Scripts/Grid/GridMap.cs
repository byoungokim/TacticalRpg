using System.Collections.Generic;
using UnityEngine;

namespace Tactics.Grid
{
    public class GridMap : MonoBehaviour
    {
        public const float StepHeight = 0.5f;
        const float BaseThickness = 0.2f;

        Tile[,] _tiles;
        readonly List<Tile> _all = new List<Tile>();

        public int Width { get; private set; }
        public int Depth { get; private set; }
        public IReadOnlyList<Tile> AllTiles => _all;
        public Vector3 Center => new Vector3((Width - 1) / 2f, 0.5f, (Depth - 1) / 2f);

        static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        // Layout legend (first row = far side of the map):
        //   '0'-'9' grass at that height, 'A'-'J' stone at height 0-9,
        //   '~' water (impassable), '#' wall (impassable).
        public void Build(string[] rows)
        {
            Depth = rows.Length;
            Width = rows[0].Length;
            _tiles = new Tile[Width, Depth];

            for (int z = 0; z < Depth; z++)
            for (int x = 0; x < Width; x++)
            {
                Parse(rows[Depth - 1 - z][x], out int height, out TerrainType terrain);
                var tile = new Tile(new Vector2Int(x, z), height, terrain);
                _tiles[x, z] = tile;
                _all.Add(tile);
                CreateView(tile);
            }
        }

        public Tile Get(Vector2Int c) => Get(c.x, c.y);

        public Tile Get(int x, int z)
        {
            if (x < 0 || z < 0 || x >= Width || z >= Depth) return null;
            return _tiles[x, z];
        }

        public IEnumerable<Tile> Neighbors(Tile tile)
        {
            foreach (var d in Directions)
            {
                var n = Get(tile.Coord + d);
                if (n != null) yield return n;
            }
        }

        public List<Tile> TilesWithin(Tile center, int radius)
        {
            var result = new List<Tile>();
            foreach (var t in _all)
                if (t.Terrain != TerrainType.Wall && Distance(t, center) <= radius) result.Add(t);
            return result;
        }

        public void ClearTints()
        {
            foreach (var t in _all)
            {
                if (t.RangeTint == null && t.AreaTint == null) continue;
                t.RangeTint = null;
                t.AreaTint = null;
                t.Repaint();
            }
        }

        public static int Distance(Tile a, Tile b) =>
            Mathf.Abs(a.Coord.x - b.Coord.x) + Mathf.Abs(a.Coord.y - b.Coord.y);

        static void Parse(char ch, out int height, out TerrainType terrain)
        {
            if (ch >= '0' && ch <= '9') { height = ch - '0'; terrain = TerrainType.Grass; }
            else if (ch >= 'A' && ch <= 'J') { height = ch - 'A'; terrain = TerrainType.Stone; }
            else if (ch == '~') { height = 0; terrain = TerrainType.Water; }
            else { height = 4; terrain = TerrainType.Wall; }
        }

        void CreateView(Tile tile)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Tile {tile.Coord.x},{tile.Coord.y}";
            go.transform.SetParent(transform, false);

            float top = tile.Height * StepHeight;
            if (tile.Terrain == TerrainType.Water) top -= 0.15f;
            float thickness = top + BaseThickness + 0.15f;
            go.transform.localPosition = new Vector3(tile.Coord.x, top - thickness / 2f, tile.Coord.y);
            go.transform.localScale = new Vector3(0.96f, thickness, 0.96f);

            go.AddComponent<TileView>().Tile = tile;
            tile.BindView(go.GetComponent<Renderer>(), BaseColor(tile));
        }

        static Color BaseColor(Tile tile)
        {
            Color c;
            switch (tile.Terrain)
            {
                case TerrainType.Grass:
                    c = Color.Lerp(new Color(0.24f, 0.42f, 0.2f), new Color(0.56f, 0.66f, 0.34f), tile.Height / 4f);
                    break;
                case TerrainType.Stone:
                    c = Color.Lerp(new Color(0.48f, 0.45f, 0.4f), new Color(0.7f, 0.67f, 0.6f), tile.Height / 4f);
                    break;
                case TerrainType.Water:
                    c = new Color(0.18f, 0.36f, 0.58f);
                    break;
                default:
                    c = new Color(0.3f, 0.27f, 0.25f);
                    break;
            }
            // Subtle checkerboard so individual tiles are easy to count.
            return (tile.Coord.x + tile.Coord.y) % 2 == 0 ? c : c * 0.92f;
        }
    }
}
