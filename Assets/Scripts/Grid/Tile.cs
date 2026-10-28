using Tactics.Units;
using UnityEngine;

namespace Tactics.Grid
{
    public enum TerrainType { Grass, Stone, Water, Wall }

    public class Tile
    {
        public readonly Vector2Int Coord;
        public readonly int Height;
        public readonly TerrainType Terrain;
        public Unit Occupant;

        // Highlight layers, combined in Repaint().
        public Color? RangeTint;
        public Color? AreaTint;
        public bool Hovered;

        Renderer _renderer;
        Color _baseColor;

        public Tile(Vector2Int coord, int height, TerrainType terrain)
        {
            Coord = coord;
            Height = height;
            Terrain = terrain;
        }

        public bool Walkable => Terrain == TerrainType.Grass || Terrain == TerrainType.Stone;
        public Vector3 TopCenter => new Vector3(Coord.x, Height * GridMap.StepHeight, Coord.y);

        public void BindView(Renderer renderer, Color baseColor)
        {
            _renderer = renderer;
            _baseColor = baseColor;
            Repaint();
        }

        public void Repaint()
        {
            var c = _baseColor;
            if (RangeTint.HasValue) c = Color.Lerp(c, RangeTint.Value, 0.55f);
            if (AreaTint.HasValue) c = Color.Lerp(c, AreaTint.Value, 0.7f);
            if (Hovered) c = Color.Lerp(c, Color.yellow, 0.45f);
            _renderer.material.color = c;
        }
    }
}
