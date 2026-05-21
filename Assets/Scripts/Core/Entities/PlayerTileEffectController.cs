using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonGame.Core.Entities
{
    /// <summary>
    /// Monitors the tile the player is currently standing on and applies corresponding biome-specific hazards or movement effects 
    /// (e.g., lava damage, ocean water slows, mud slows), while respecting active protective items like lilypads.
    /// </summary>
    [RequireComponent(typeof(CharacterMovement))]
    public class PlayerTileEffectController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The Ground/Floor Tilemap to fetch active tiles from.")]
        [SerializeField] private Tilemap groundTilemap;
        
        [Header("Effect Settings")]
        [Tooltip("Normal movement speed applied when no special floor effects are active.")]
        [SerializeField] private float normalSpeed = 5f;
        [Tooltip("Slow movement speed applied when wading through ocean or mud tiles.")]
        [SerializeField] private float slowSpeed = 2f;
        [Tooltip("Lava damage applied per tick.")]
        [SerializeField] private float lavaDamage = 10f;
        [Tooltip("Interval in seconds between consecutive lava damage ticks.")]
        [SerializeField] private float damageInterval = 10f;
        [Tooltip("Acid damage applied by acid tiles (queried by AcidTile scripts).")]
        [SerializeField] private float acidDamage = 10f; 

        public float AcidDamage => acidDamage;

        private float nextDamageTime;
        private Health health;
        private CharacterMovement characterMovement;

        /// <summary>
        /// Keeps track of the number of overlapping lilypads the player is currently touching.
        /// If greater than 0, player is immune to ocean slow effects.
        /// </summary>
        public int LilypadCount { get; set; } = 0;

        private void Awake()
        {
            characterMovement = GetComponent<CharacterMovement>();
            health = GetComponent<Health>();
            characterMovement.baseSpeed = normalSpeed;
        }

        private void Start()
        {
            if (groundTilemap == null)
            {
                // Attempt to auto-find the Ground Tilemap under the main grid if not manually assigned
                GameObject gridObj = GameObject.Find("Grid");
                if (gridObj != null)
                {
                    var maps = gridObj.GetComponentsInChildren<Tilemap>();
                    foreach (var map in maps)
                    {
                        if (map.name.Contains("Ground") || map.name.Contains("Floor") || map.name.Contains("ground"))
                        {
                            groundTilemap = map;
                            break;
                        }
                    }
                }
            }
        }

        private void Update()
        {
            if (groundTilemap == null || health == null) return;

            // Convert player world position to tilemap cell coordinates
            Vector3Int cellPosition = groundTilemap.WorldToCell(transform.position);
            TileBase currentTile = groundTilemap.GetTile(cellPosition);

            if (currentTile != null)
            {
                string tileName = currentTile.name.ToLower();

                // 1. Lava Hazard: Periodic damage if standing on a tile containing 'damagable' or 'lava' in its name
                if (tileName.Contains("damagable") || tileName.Contains("lava"))
                {
                    if (Time.time >= nextDamageTime)
                    {
                        health.TakeDamage(lavaDamage);
                        nextDamageTime = Time.time + damageInterval;
                    }
                }

                // 2. Slow Effects
                // Dirt Biome: Slow applies on mud or special dirt tiles
                bool isSpecialDirt = tileName.Contains("dirt") && (tileName.Contains("special") || tileName.Contains("mud"));
                // Ocean Biome: Slow applies on any ocean tile
                bool isOcean = tileName.Contains("ocean");

                // Apply slow if on mud/water and player is NOT standing on a lilypad
                if ((isSpecialDirt || isOcean) && LilypadCount <= 0)
                {
                    characterMovement.SetSpeed(slowSpeed);
                }
                else
                {
                    characterMovement.SetSpeed(normalSpeed);
                }
            }
            else if (LilypadCount > 0)
            {
                characterMovement.SetSpeed(normalSpeed);
            }
            else
            {
                characterMovement.SetSpeed(normalSpeed);
            }
        }
    }
}
