using UnityEngine;
using System.Collections.Generic;

namespace DungeonGame.Systems.Dungeon
{
    /// <summary>
    /// Represents a generated room container within the dungeon layout hierarchy.
    /// Tracks bounds, biome theme, and active player/enemy spawn nodes.
    /// </summary>
    public class RoomNode : MonoBehaviour
    {
        [Tooltip("The biome category assigned to this room.")]
        public BiomeType RoomBiome;
        [Tooltip("The bounding rectangle of the room in tile grid units.")]
        public RectInt Bounds;
        [Tooltip("List of player spawning transform positions (primarily in the starting room).")]
        public List<Transform> PlayerSpawnNodes = new List<Transform>();
        [Tooltip("List of enemy spawning transform positions scattered within the room.")]
        public List<Transform> EnemySpawnNodes = new List<Transform>();

        /// <summary>
        /// Draws colored wireframes and spawn sphere helpers in the Scene view for developer layout inspection.
        /// </summary>
        private void OnDrawGizmos()
        {
            // Color code room bounds by biome
            Gizmos.color = RoomBiome switch
            {
                BiomeType.Normal => Color.white,
                BiomeType.Ocean => Color.blue,
                BiomeType.Hot => Color.red,
                BiomeType.Dirt => new Color(0.6f, 0.3f, 0f), // Brown
                _ => Color.white
            };

            // Draw bounding box
            Gizmos.DrawWireCube(new Vector3(Bounds.center.x, Bounds.center.y, 0), new Vector3(Bounds.width, Bounds.height, 0));

            // Draw green sphere markers for player spawning nodes
            Gizmos.color = Color.green;
            foreach (var node in PlayerSpawnNodes)
            {
                if (node != null) Gizmos.DrawSphere(node.position, 0.3f);
            }

            // Draw magenta sphere markers for enemy spawning nodes
            Gizmos.color = Color.magenta;
            foreach (var node in EnemySpawnNodes)
            {
                if (node != null) Gizmos.DrawSphere(node.position, 0.3f);
            }
        }
    }
}
