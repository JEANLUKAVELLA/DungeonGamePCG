using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace DungeonGame.Systems.Dungeon
{
    /// <summary>
    /// Supported dungeon environment types, defining visual themes and hazard behaviors.
    /// </summary>
    public enum BiomeType
    {
        Normal,
        Ocean,
        Hot,
        Dirt
    }

    /// <summary>
    /// Serializable palette container matching a BiomeType to its lists of floor, wall, and hazard tiles.
    /// Used by the procedural dungeon generator to paint different themes.
    /// </summary>
    [System.Serializable]
    public class BiomePalette
    {
        [Tooltip("The corresponding biome category.")]
        public BiomeType BiomeType;
        [Tooltip("List of tile assets representing the standard floor walkables.")]
        public List<TileBase> FloorTiles = new List<TileBase>();
        [Tooltip("List of tile assets representing blocking walls (e.g. straight segments).")]
        public List<TileBase> WallTiles = new List<TileBase>();
        [Tooltip("List of tile assets representing corners. If left empty, the last tile in WallTiles will be used as a fallback.")]
        public List<TileBase> CornerTiles = new List<TileBase>();
        [Tooltip("List of tile assets representing biome hazards (e.g., mud, lava).")]
        public List<TileBase> SpecialTiles = new List<TileBase>(); // Mud, Lava, etc.

        /// <summary>
        /// Retrieves a random floor tile asset from the palette.
        /// </summary>
        public TileBase GetRandomFloor()
        {
            if (FloorTiles.Count == 0) return null;
            return FloorTiles[Random.Range(0, FloorTiles.Count)];
        }

        /// <summary>
        /// Retrieves a random straight wall tile asset.
        /// If CornerTiles is configured, all WallTiles are treated as straight walls.
        /// If CornerTiles is empty, the last WallTile is reserved as a corner fallback.
        /// </summary>
        public TileBase GetStraightWall()
        {
            if (WallTiles.Count == 0) return null;

            if (CornerTiles != null && CornerTiles.Count > 0)
            {
                return WallTiles[Random.Range(0, WallTiles.Count)];
            }

            if (WallTiles.Count > 1)
            {
                return WallTiles[Random.Range(0, WallTiles.Count - 1)];
            }

            return WallTiles[0];
        }

        /// <summary>
        /// Retrieves a random corner wall tile asset.
        /// Falls back to the last element of WallTiles if CornerTiles list is empty.
        /// </summary>
        public TileBase GetCornerWall()
        {
            if (CornerTiles != null && CornerTiles.Count > 0)
            {
                return CornerTiles[Random.Range(0, CornerTiles.Count)];
            }

            if (WallTiles.Count > 0)
            {
                return WallTiles[WallTiles.Count - 1];
            }

            return null;
        }

        /// <summary>
        /// Retrieves a random hazard tile asset. Falls back to a standard floor tile if empty.
        /// </summary>
        public TileBase GetRandomSpecial()
        {
            if (SpecialTiles.Count == 0) return GetRandomFloor();
            return SpecialTiles[Random.Range(0, SpecialTiles.Count)];
        }
    }
}
