using UnityEngine;

namespace Tactics.Grid
{
    // Links a tile's GameObject back to its Tile so mouse raycasts can find it.
    public class TileView : MonoBehaviour
    {
        public Tile Tile;
    }
}
