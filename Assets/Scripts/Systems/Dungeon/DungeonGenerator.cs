using UnityEngine;
using UnityEngine.Tilemaps;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using DungeonGame.Core.Entities;
using DungeonGame.Systems.Managers;
using System.Collections;

namespace DungeonGame.Systems.Dungeon
{
    /// <summary>
    /// Core engine class driving the procedural dungeon generation, dynamic difficulty scaling,
    /// grid-based room layouts, wall tile mapping, and biome-specific decoration placement.
    /// </summary>
    public class DungeonGenerator : MonoBehaviour
    {
        public static DungeonGenerator Instance { get; private set; }

        [Title("Dungeon Settings")]
        [Tooltip("Seed used for random number generation. Restores repeatability of generated layouts.")]
        [SerializeField] private int seed = 12345;
        [Tooltip("If true, system initializes generation with a unique TickCount-based seed.")]
        [SerializeField] private bool useRandomSeed = true;
        [Tooltip("The target number of rooms to generate in the layout. Scaled dynamically by player performance.")]
        [SerializeField] private int maxRooms = 10;
        [Tooltip("Minimum width and height bounds for generated rooms.")]
        [SerializeField] private Vector2Int roomSizeMin = new Vector2Int(5, 5);
        [Tooltip("Maximum width and height bounds for generated rooms.")]
        [SerializeField] private Vector2Int roomSizeMax = new Vector2Int(10, 10);
        [Tooltip("Space padding added around rooms to prevent layouts from overlapping too closely.")]
        [SerializeField] private int padding = 6; 

        [Title("Dynamic Difficulty Scaling")]
        [Tooltip("Target time limit (in seconds) players are expected to complete a floor within.")]
        [SerializeField] private float targetTimeLimit = 90f; 
        [Tooltip("Target cumulative damage limit players are expected to stay under.")]
        [SerializeField] private float targetDamageLimit = 30f; 

        /// <summary>
        /// Global multiplier scaling stats (e.g., enemy damage, spawns, trap counts) based on player speed and efficiency.
        /// </summary>
        public float CurrentDifficultyMultiplier { get; private set; } = 1.0f;

        private static int defaultMaxRooms = -1;
        private static int defaultEnemySpawnsPerRoom = -1;
        private static int defaultTrapsPerRoom = -1;

        [Title("Tilemaps & Output")]
        [Tooltip("Tilemap target for painting floors and walkables.")]
        [SerializeField] private Tilemap groundTilemap;
        [Tooltip("Tilemap target for painting collider walls.")]
        [SerializeField] private Tilemap wallTilemap;
        [Tooltip("Parent transform to instantiate generated room nodes under.")]
        [SerializeField] private Transform roomsContainer;

        [Title("Biomes")]
        [Tooltip("List of biome palettes associating theme tags to tile assets.")]
        [SerializeField] private List<BiomePalette> biomePalettes = new List<BiomePalette>();
        [Tooltip("Probability chance (0 to 1) of placing a special hazard floor tile (like mud/lava) instead of a standard walkable.")]
        [SerializeField, Range(0, 1)] private float specialTileChance = 0.2f;

        [Title("Spawns")]
        [Tooltip("Number of player start locations to generate in the initial starting room.")]
        [SerializeField] private int playerSpawnsPerNormalRoom = 1;
        [Tooltip("Baseline count of enemies to spawn per room. Scaled by difficulty.")]
        [SerializeField] private int enemySpawnsPerRoom = 2;
        [Tooltip("Prefab representing enemies spawned in Hot biomes.")]
        [SerializeField] private GameObject hotEnemyPrefab;
        [Tooltip("Prefab representing enemies spawned in Ocean biomes.")]
        [SerializeField] private GameObject oceanEnemyPrefab;
        [Tooltip("Prefab representing enemies spawned in Dirt biomes.")]
        [SerializeField] private GameObject dirtEnemyPrefab;

        [Title("Props & Traps")]
        [Tooltip("Spawner painting prefab placed in the starting room.")]
        [SerializeField] private GameObject spawnerPaintingPrefab;
        [Tooltip("Exit painting portal prefab placed in the victory room.")]
        [SerializeField] private GameObject exitPaintingPrefab;
        [Tooltip("Collectible key crystal prefab required to unlock the teleporter.")]
        [SerializeField] private GameObject keyCrystalPrefab;
        [Tooltip("List of trap prefabs (e.g., spikes) to choose from.")]
        [SerializeField] private List<GameObject> spikePrefabs = new List<GameObject>();
        [Tooltip("Baseline number of keys to spawn across the level.")]
        [SerializeField] private int keysToSpawn = 3;
        [Tooltip("Baseline number of traps to place per room. Scaled by difficulty.")]
        [SerializeField] private int trapsPerRoom = 3;

        [Title("Dirt Biome Props")]
        [Tooltip("Collectible props spawned in Dirt biome rooms.")]
        [SerializeField] private List<GameObject> dirtPropsPrefabs = new List<GameObject>();
        [Tooltip("Hatch trap prefab spawned in Dirt biome rooms.")]
        [SerializeField] private GameObject dirtHatchTrapPrefab;
        [Tooltip("Number of decorative props to scatter per Dirt room.")]
        [SerializeField] private int dirtPropsPerRoom = 4;
        
        [Title("Ocean Biome Props")]
        [Tooltip("Acid damage hazard tile spawned on floor grids in Ocean rooms.")]
        [SerializeField] private GameObject acidTilePrefab;
        [Tooltip("Lilypad prefabs spawned on Ocean room grids (helps player avoid water slows).")]
        [SerializeField] private List<GameObject> lilypadPrefabs = new List<GameObject>();
        [Tooltip("Number of props/decorations to scatter per Ocean room.")]
        [SerializeField] private int oceanPropsPerRoom = 5;

        [Title("Normal Biome Props")]
        [Tooltip("Candle decorative prefabs spawned in normal rooms.")]
        [SerializeField] private List<GameObject> candlePrefabs = new List<GameObject>();
        [Tooltip("Number of candles to spawn per normal room.")]
        [SerializeField] private int candlesPerRoom = 3;

        [Title("Hot Biome Props")]
        [Tooltip("Hot biome crystal prefabs.")]
        [SerializeField] private List<GameObject> crystalPrefabs = new List<GameObject>();
        [Tooltip("Rock prop prefab.")]
        [SerializeField] private GameObject rockPrefab;
        [Tooltip("Column/pillar prop prefabs.")]
        [SerializeField] private List<GameObject> columnPrefabs = new List<GameObject>();
        [Tooltip("Treasure chest prefabs spawned in Hot biomes.")]
        [SerializeField] private List<GameObject> chestPrefabs = new List<GameObject>();
        [Tooltip("Fire pit hazard prefab.")]
        [SerializeField] private GameObject firePitPrefab;
        [Tooltip("Number of props to spawn per Hot room.")]
        [SerializeField] private int hotPropsPerRoom = 6;

        private List<RectInt> generatedRooms = new List<RectInt>();
        private List<RoomNode> roomNodes = new List<RoomNode>();
        
        // Two-pass generation map caching floor cell coordinates to their associated biome theme
        private Dictionary<Vector3Int, BiomeType> floorMap = new Dictionary<Vector3Int, BiomeType>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Cache original inspector defaults statically on the very first run
            if (defaultMaxRooms <= 0)
            {
                if (maxRooms <= 0) maxRooms = 10;
                defaultMaxRooms = maxRooms;
            }
            if (defaultEnemySpawnsPerRoom < 0)
            {
                if (enemySpawnsPerRoom < 0) enemySpawnsPerRoom = 2;
                defaultEnemySpawnsPerRoom = enemySpawnsPerRoom;
            }
            if (defaultTrapsPerRoom < 0)
            {
                if (trapsPerRoom < 0) trapsPerRoom = 3;
                defaultTrapsPerRoom = trapsPerRoom;
            }
        }

        private void Start()
        {
            GenerateDungeon();
        }

        /// <summary>
        /// Clears all existing tiles and game objects, checks levels, computes seeds, 
        /// and runs the multi-stage procedural dungeon generator pipeline.
        /// </summary>
        [Button("Generate Dungeon", ButtonSizes.Large)]
        public void GenerateDungeon()
        {
            // Ensure defaults are cached if GenerateDungeon is called before Awake (e.g., in Edit Mode)
            if (defaultMaxRooms <= 0)
            {
                if (maxRooms <= 0) maxRooms = 10;
                defaultMaxRooms = maxRooms;
            }
            if (defaultEnemySpawnsPerRoom < 0)
            {
                if (enemySpawnsPerRoom < 0) enemySpawnsPerRoom = 2;
                defaultEnemySpawnsPerRoom = enemySpawnsPerRoom;
            }
            if (defaultTrapsPerRoom < 0)
            {
                if (trapsPerRoom < 0) trapsPerRoom = 3;
                defaultTrapsPerRoom = trapsPerRoom;
            }

            // Apply self-healing fallback to current values to prevent negative limits
            if (maxRooms <= 0) maxRooms = 10;
            if (enemySpawnsPerRoom < 0) enemySpawnsPerRoom = 2;
            if (trapsPerRoom < 0) trapsPerRoom = 3;

            ClearDungeon();

            if (ScoreManager.Instance != null)
            {
                // If this is level 1 (initial run or post-death restart), restore original inspector values
                if (ScoreManager.Instance.GetCurrentLevel() == 1)
                {
                    maxRooms = defaultMaxRooms;
                    enemySpawnsPerRoom = defaultEnemySpawnsPerRoom;
                    trapsPerRoom = defaultTrapsPerRoom;
                    CurrentDifficultyMultiplier = 1.0f;
                    Debug.Log($"DungeonGenerator: Restored original Level 1 defaults -> Rooms: {maxRooms}, Enemies/Room: {enemySpawnsPerRoom}, Traps/Room: {trapsPerRoom}"); // for testing purposes
                }

                ScoreManager.Instance.ResetForNextLevel();
            }

            if (useRandomSeed)
            {
                seed = System.Environment.TickCount ^ System.Guid.NewGuid().GetHashCode();
            }
            Random.InitState(seed);

            GenerateRoomLayout();
            DrawFloorsAndCorridors();
            DrawWalls();
            GenerateSpawnNodes();
            GenerateProps();

            Debug.Log($"Generated dungeon with {generatedRooms.Count} rooms. Seed: {seed}"); // for testing purposes

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.StartLevelTimer();
            }
        }

        /// <summary>
        /// Scales level parameters (room numbers, enemy spawns, trap counts) by comparing 
        /// player completion times and health remaining against targeted threshold limits.
        /// </summary>
        /// <param name="timeTaken">Completion duration of the level in seconds.</param>
        /// <param name="healthRemaining">Player's current health when finishing the level.</param>
        public void ScaleDifficulty(float timeTaken, float healthRemaining)
        {
            Debug.Log($"[Difficulty] Floor Finished in {timeTaken:F1}s (Target: {targetTimeLimit}s), Health Remaining: {healthRemaining:F1}"); // for testing purposes

            // I calculated the difficulty scaling factor:
            // Lesser time means I performed better -> increase difficulty
            // Higher health remaining means I performed better -> increase difficulty
            float timeFactor = targetTimeLimit / Mathf.Max(1f, timeTaken);
            float healthFactor = Mathf.Max(1f, healthRemaining) / targetDamageLimit;

            // I clamped individual factors to prevent extreme scaling
            timeFactor = Mathf.Clamp(timeFactor, 0.5f, 2.0f);
            healthFactor = Mathf.Clamp(healthFactor, 0.5f, 2.0f);

            // I calculated the overall performance multiplier
            float performanceMultiplier = (timeFactor + healthFactor) / 2f;
            CurrentDifficultyMultiplier = performanceMultiplier;

            // I applied scaling to dungeon parameters
            maxRooms = Mathf.Clamp(Mathf.RoundToInt(maxRooms * performanceMultiplier), 5, 20);
            enemySpawnsPerRoom = Mathf.Clamp(Mathf.RoundToInt(enemySpawnsPerRoom * performanceMultiplier), 1, 5);
            trapsPerRoom = Mathf.Clamp(Mathf.RoundToInt(trapsPerRoom * performanceMultiplier), 1, 8);

            Debug.Log($"[Difficulty] New Settings -> Rooms: {maxRooms}, Enemies/Room: {enemySpawnsPerRoom}, Traps/Room: {trapsPerRoom} (Mult: {performanceMultiplier:F2})"); // for testing purposes
        }

        /// <summary>
        /// Triggers level transition countdown, gathers performance metrics, adjusts difficulty, and spawns the next dungeon layout.
        /// </summary>
        public void TransitionToNextLevel()
        {
            StartCoroutine(LevelTransitionCoroutine());
        }

        /// <summary>
        /// Transition process coroutine managing player input locking, countdown displays, scaling, and scene generation.
        /// </summary>
        private IEnumerator LevelTransitionCoroutine()
        {
            // 1. Pause player input/movement
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            PlayerController playerCtrl = null;
            CharacterMovement characterMove = null;
            Rigidbody2D playerRb = null;

            if (player != null)
            {
                playerCtrl = player.GetComponent<PlayerController>();
                characterMove = player.GetComponent<CharacterMovement>();
                playerRb = player.GetComponent<Rigidbody2D>();

                if (playerCtrl != null) playerCtrl.enabled = false;
                if (characterMove != null) characterMove.SetMovement(Vector2.zero);
                if (playerRb != null) playerRb.velocity = Vector2.zero;
            }

            // 2. Countdown 3 -> 2 -> 1
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowInteractionMessage("3");
                yield return new WaitForSeconds(1f);
                UIManager.Instance.ShowInteractionMessage("2");
                yield return new WaitForSeconds(1f);
                UIManager.Instance.ShowInteractionMessage("1");
                yield return new WaitForSeconds(1f);
                UIManager.Instance.HideInteractionMessage();
            }
            else
            {
                yield return new WaitForSeconds(3f);
            }

            // 3. Stop timer and gather level performance metrics
            float timeTaken = 0f;
            float healthRemaining = 100f;

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.StopLevelTimer();
                timeTaken = ScoreManager.Instance.GetLevelTime();
            }

            Health playerHealth = null;
            if (player != null)
            {
                playerHealth = player.GetComponent<Health>();
                if (playerHealth != null)
                {
                    healthRemaining = playerHealth.CurrentHealth;
                }
            }

            // 4. Adjust and scale the next dungeon's difficulty
            ScaleDifficulty(timeTaken, healthRemaining);

            // Increment level count in ScoreManager
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.IncrementLevel();
            }

            // 5. Generate the new level structure
            GenerateDungeon();

            // 6. Heal player back to full health and reset level stats
            if (playerHealth != null)
            {
                playerHealth.ResetHealth();
            }

            // Configure Player Damage for the new level
            if (playerCtrl != null)
            {
                PlayerController pc = playerCtrl as PlayerController;
                if (pc != null) pc.ConfigureDamage();
            }

            // 7. Restart background level timer
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetLevelTime();
            }

            // 8. Restore player input/movement
            if (playerCtrl != null) playerCtrl.enabled = true;

            // 9. Show special welcome banner if it's a Boss Level!
            int nextLevel = ScoreManager.Instance != null ? ScoreManager.Instance.GetCurrentLevel() : 1;
            if (nextLevel % 5 == 0 && UIManager.Instance != null)
            {
                UIManager.Instance.ShowInteractionMessage($"⚠️ BOSS LEVEL {nextLevel} DETECTED! Double Score Active!");
                Instance.StartCoroutine(Instance.ClearMessageAfterDelay(3.5f));
            }
        }

        /// <summary>
        /// Hides the UI interaction message after the specified delay duration.
        /// </summary>
        private IEnumerator ClearMessageAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideInteractionMessage();
            }
        }

        // Cache of tile locations flagged as safe (around start/exit portals) to prevent hazard placements
        private HashSet<Vector3Int> safeTiles = new HashSet<Vector3Int>();

        /// <summary>
        /// Places all decorations, collectible key crystals, spawner/exit portal paintings, spikes, and biome-specific interactive props.
        /// </summary>
        private void GenerateProps()
        {
            if (roomNodes.Count == 0) return;
            safeTiles.Clear();

            // 1. Spawner Painting where the player spawns
            RoomNode startRoom = roomNodes.FirstOrDefault(r => r.PlayerSpawnNodes.Count > 0);
            if (startRoom != null && spawnerPaintingPrefab != null)
            {
                Vector3 spawnPos = startRoom.PlayerSpawnNodes[0].position;
                spawnPos.z = -1f;
                GameObject prop = Instantiate(spawnerPaintingPrefab, spawnPos, Quaternion.identity);
                prop.name = "SpawnerPainting";
                prop.transform.SetParent(startRoom.transform);

                // Reserve the 3x3 area around spawner as safe as safe from hazards
                Vector3Int basePos = groundTilemap.WorldToCell(spawnPos);
                for (int safeOffsetX = -1; safeOffsetX <= 1; safeOffsetX++)
                    for (int safeOffsetY = -1; safeOffsetY <= 1; safeOffsetY++)
                        safeTiles.Add(basePos + new Vector3Int(safeOffsetX, safeOffsetY, 0));
            }

            // 2. Exit Painting in a different room
            RoomNode exitRoom = roomNodes.Where(r => r != startRoom).LastOrDefault();
            if (exitRoom == null) exitRoom = roomNodes[roomNodes.Count - 1]; // Fallback if only 1 room

            if (exitPaintingPrefab != null)
            {
                // Place in the middle instead of the wall
                Vector3 spawnPos = groundTilemap.GetCellCenterWorld(new Vector3Int(exitRoom.Bounds.x + exitRoom.Bounds.width / 2, exitRoom.Bounds.y + exitRoom.Bounds.height / 2, 0));
                spawnPos.z = -1f;
                GameObject prop = Instantiate(exitPaintingPrefab, spawnPos, Quaternion.identity);
                prop.name = "ExitPainting";
                prop.transform.SetParent(exitRoom.transform);

                // Reserve the 3x3 area around exit as safe as safe from hazards
                Vector3Int basePos = groundTilemap.WorldToCell(spawnPos);
                for (int safeOffsetX = -1; safeOffsetX <= 1; safeOffsetX++)
                    for (int safeOffsetY = -1; safeOffsetY <= 1; safeOffsetY++)
                        safeTiles.Add(basePos + new Vector3Int(safeOffsetX, safeOffsetY, 0));
            }

            // After placing Spawner/Exit, force floor tiles to be normal in safe zones
            foreach (var tilePos in safeTiles)
            {
                if (floorMap.ContainsKey(tilePos))
                {
                    BiomePalette palette = GetPalette(floorMap[tilePos]) ?? GetPalette(BiomeType.Normal);
                    if (palette != null)
                    {
                        groundTilemap.SetTile(tilePos, palette.GetRandomFloor());
                    }
                }
            }

            // SMART PLACEMENT GENERATION
            // We iterate through all rooms, categorize their geometry, and assign props logically!
            foreach (var room in roomNodes)
            {
                GetCategorizedPositions(room.Bounds, out var corners, out var walls, out var centers, out var doorways);

                // Spikes (All rooms except Ocean)
                if (room.RoomBiome != BiomeType.Ocean && spikePrefabs != null && spikePrefabs.Count > 0)
                {
                    for (int i = 0; i < trapsPerRoom; i++)
                    {
                        if (Random.value < 0.3f || i == 0) // 30% chance or guaranteed first time
                        {
                            // Spikes logically guard doorways as chokepoints
                            Vector3 pos = GetSmartPosition(doorways.Count > 0 ? doorways : centers, room.Bounds);
                            pos.z = -1f;
                            GameObject prefab = spikePrefabs[Random.Range(0, spikePrefabs.Count)];
                            if (prefab != null)
                            {
                                GameObject trap = Instantiate(prefab, pos, Quaternion.identity);
                                trap.transform.SetParent(room.transform);
                            }
                        }
                    }
                }

                // Scatter Keys (Only if it's not start or exit room)
                if (keyCrystalPrefab != null && room != startRoom && room != exitRoom)
                {
                    // 50% chance a room has a key
                    if (Random.value > 0.5f) 
                    {
                        // Keys hide in corners to force exploration
                        Vector3 pos = GetSmartPosition(corners.Count > 0 ? corners : centers, room.Bounds);
                        pos.z = -1f;
                        GameObject key = Instantiate(keyCrystalPrefab, pos, Quaternion.identity);
                        key.transform.SetParent(room.transform);
                    }
                }

                // Biome Specifics
                if (room.RoomBiome == BiomeType.Dirt)
                {
                    // Dirt Props (Skulls/Bones) randomly scattered
                    if (dirtPropsPrefabs != null && dirtPropsPrefabs.Count > 0)
                    {
                        for (int i = 0; i < dirtPropsPerRoom; i++)
                        {
                            if (Random.value < 0.6f || i == 0)
                            {
                                Vector3 pos = GetSmartPosition(centers, room.Bounds);
                                pos.z = -1f;
                                GameObject prop = Instantiate(dirtPropsPrefabs[Random.Range(0, dirtPropsPrefabs.Count)], pos, Quaternion.identity);
                                prop.transform.SetParent(room.transform);
                            }
                        }
                    }

                    // Dirt Hatch Trap guards doorways
                    if (dirtHatchTrapPrefab != null && Random.value < 0.5f)
                    {
                        Vector3 pos = GetSmartPosition(doorways, room.Bounds);
                        pos.z = -1f;
                        GameObject trap = Instantiate(dirtHatchTrapPrefab, pos, Quaternion.identity);
                        trap.transform.SetParent(room.transform);
                    }
                }
                else if (room.RoomBiome == BiomeType.Ocean)
                {
                    for (int i = 0; i < oceanPropsPerRoom; i++)
                    {
                        // Ocean hazards/decor scattered in center
                        if (acidTilePrefab != null && Random.value < 0.4f)
                        {
                            Vector3 pos = GetSmartPosition(centers, room.Bounds);
                            pos.z = -1f;
                            GameObject acid = Instantiate(acidTilePrefab, pos, Quaternion.identity);
                            acid.transform.SetParent(room.transform);
                        }
                        
                        if (lilypadPrefabs != null && lilypadPrefabs.Count > 0 && Random.value < 0.4f)
                        {
                            Vector3 pos = GetSmartPosition(centers, room.Bounds);
                            pos.z = -1f;
                            GameObject lily = Instantiate(lilypadPrefabs[Random.Range(0, lilypadPrefabs.Count)], pos, Quaternion.identity);
                            lily.transform.SetParent(room.transform);
                        }
                    }
                }
                else if (room.RoomBiome == BiomeType.Hot)
                {
                    bool spawnedCrystal = false, spawnedRock = false, spawnedColumn = false, spawnedChest = false, spawnedFirepit = false;

                    for (int i = 0; i < hotPropsPerRoom; i++)
                    {
                        int propType = Random.Range(0, 5); 
                        if (!spawnedCrystal) propType = 0;
                        else if (!spawnedRock) propType = 1;
                        else if (!spawnedColumn) propType = 2;
                        else if (!spawnedChest) propType = 3;
                        else if (!spawnedFirepit) propType = 4;

                        GameObject prefab = null;
                        Vector3 pos = Vector3.zero;

                        switch (propType)
                        {
                            case 0:
                                if (crystalPrefabs != null && crystalPrefabs.Count > 0)
                                    prefab = crystalPrefabs[Random.Range(0, crystalPrefabs.Count)];
                                pos = GetSmartPosition(corners.Count > 0 ? corners : centers, room.Bounds);
                                spawnedCrystal = true;
                                break;
                            case 1:
                                prefab = rockPrefab;
                                pos = GetSmartPosition(centers, room.Bounds);
                                spawnedRock = true;
                                break;
                            case 2:
                                if (columnPrefabs != null && columnPrefabs.Count > 0)
                                    prefab = columnPrefabs[Random.Range(0, columnPrefabs.Count)];
                                pos = GetSmartPosition(walls.Count > 0 ? walls : centers, room.Bounds);
                                spawnedColumn = true;
                                break;
                            case 3:
                                if (!spawnedChest || Random.value < 0.2f)
                                {
                                    if (chestPrefabs != null && chestPrefabs.Count > 0)
                                        prefab = chestPrefabs[Random.Range(0, chestPrefabs.Count)];
                                    pos = GetSmartPosition(corners.Count > 0 ? corners : centers, room.Bounds);
                                }
                                spawnedChest = true;
                                break;
                            case 4:
                                if (!spawnedFirepit || Random.value < 0.3f)
                                {
                                    prefab = firePitPrefab;
                                    pos = GetSmartPosition(doorways.Count > 0 ? doorways : centers, room.Bounds);
                                }
                                spawnedFirepit = true;
                                break;
                        }

                        if (prefab != null)
                        {
                            pos.z = -1f;
                            GameObject prop = Instantiate(prefab, pos, Quaternion.identity);
                            prop.transform.SetParent(room.transform);
                        }
                    }
                }
                else if (room.RoomBiome == BiomeType.Normal)
                {
                    // Floor Candles scattered anywhere
                    for (int i = 0; i < candlesPerRoom; i++)
                    {
                        if (candlePrefabs != null && candlePrefabs.Count > 0 && Random.value < 0.5f)
                        {
                            Vector3 pos = GetSmartPosition(centers, room.Bounds);
                            pos.z = -1f;
                            GameObject candle = Instantiate(candlePrefabs[Random.Range(0, candlePrefabs.Count)], pos, Quaternion.identity);
                            candle.transform.SetParent(room.transform);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Utility to spawn a specific prop prefab on the top wall of a room.
        /// </summary>
        private void SpawnPropOnWall(RoomNode room, GameObject prefab, string name)
        {
            RectInt bounds = room.Bounds;
            int x = Random.Range(bounds.xMin + 1, bounds.xMax - 1);
            int y = bounds.yMax; // Top wall
            Vector3 pos = groundTilemap.GetCellCenterWorld(new Vector3Int(x, y, 0));
            pos.z = -1f;
            
            GameObject prop = Instantiate(prefab, pos, Quaternion.identity);
            prop.name = name;
            prop.transform.SetParent(room.transform);
        }

        /// <summary>
        /// Clears all tiles in both floor/wall tilemaps and destroys all instantiated room gameobjects.
        /// </summary>
        [Button("Clear Dungeon")]
        public void ClearDungeon()
        {
            if (groundTilemap != null) groundTilemap.ClearAllTiles();
            if (wallTilemap != null) wallTilemap.ClearAllTiles();

            if (roomsContainer != null)
            {
                for (int i = roomsContainer.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(roomsContainer.GetChild(i).gameObject);
                }
            }

            generatedRooms.Clear();
            roomNodes.Clear();
            floorMap.Clear();
        }

        /// <summary>
        /// Performs random walker/partition layout placement to spawn distinct, non-overlapping Rect room bounds.
        /// </summary>
        private void GenerateRoomLayout()
        {
            Vector2Int currentPos = Vector2Int.zero;
            int attempts = 0;
            int maxAttempts = 100;

            for (int i = 0; i < maxRooms; i++)
            {
                int w = Random.Range(roomSizeMin.x, roomSizeMax.x);
                int h = Random.Range(roomSizeMin.y, roomSizeMax.y);
                
                RectInt newRoom = new RectInt(currentPos.x - w/2, currentPos.y - h/2, w, h);
                
                // Check if this room overlaps with any existing room
                bool overlaps = false;
                foreach (var existing in generatedRooms)
                {
                    // Expand existing room bounds by padding for the check
                    RectInt paddedExisting = new RectInt(existing.x - padding, existing.y - padding, existing.width + padding * 2, existing.height + padding * 2);
                    if (newRoom.Overlaps(paddedExisting))
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                {
                    generatedRooms.Add(newRoom);
                    attempts = 0; // Reset attempts on success
                }
                else
                {
                    i--; // Try this room index again
                    attempts++;
                    if (attempts > maxAttempts) break; // Safety break
                }

                Vector2Int dir = GetRandomDirection();
                currentPos += new Vector2Int(dir.x * (w + padding), dir.y * (h + padding));
            }
        }

        /// <summary>
        /// Selects a random grid direction vector.
        /// </summary>
        private Vector2Int GetRandomDirection()
        {
            int r = Random.Range(0, 4);
            return r switch
            {
                0 => Vector2Int.up,
                1 => Vector2Int.down,
                2 => Vector2Int.left,
                3 => Vector2Int.right,
                _ => Vector2Int.up
            };
        }

        /// <summary>
        /// Paints the floor tiles on the ground tilemap and connects adjacent rooms with horizontal and vertical corridors.
        /// </summary>
        private void DrawFloorsAndCorridors()
        {
            bool hasNormalRoom = false;

            for (int i = 0; i < generatedRooms.Count; i++)
            {
                RectInt roomBounds = generatedRooms[i];
                
                BiomeType biomeType = (BiomeType)Random.Range(0, 4);
                if (!hasNormalRoom)
                {
                    biomeType = BiomeType.Normal;
                    hasNormalRoom = true;
                }
                
                BiomePalette palette = GetPalette(biomeType);
                if (palette == null) palette = GetPalette(BiomeType.Normal);

                GameObject roomObj = new GameObject($"Room_{i}_{biomeType}");
                if (roomsContainer != null) roomObj.transform.SetParent(roomsContainer);
                // Set room position at Z=0, children will be at -0.1
                roomObj.transform.position = new Vector3(roomBounds.center.x, roomBounds.center.y, 0);
                
                RoomNode roomNode = roomObj.AddComponent<RoomNode>();
                roomNode.RoomBiome = biomeType;
                roomNode.Bounds = roomBounds;
                roomNodes.Add(roomNode);

                // Add Room Floors to map
                for (int roomCellX = roomBounds.xMin; roomCellX < roomBounds.xMax; roomCellX++)
                {
                    for (int roomCellY = roomBounds.yMin; roomCellY < roomBounds.yMax; roomCellY++)
                    {
                        Vector3Int tilePos = new Vector3Int(roomCellX, roomCellY, 0);
                        floorMap[tilePos] = biomeType;

                        if (palette != null)
                        {
                            TileBase floorTile = palette.GetRandomFloor();
                            float chance = biomeType == BiomeType.Hot ? specialTileChance * 0.5f : specialTileChance;
                            if ((biomeType == BiomeType.Hot || biomeType == BiomeType.Dirt) && Random.value < chance)
                            {
                                floorTile = palette.GetRandomSpecial();
                            }
                            groundTilemap.SetTile(tilePos, floorTile);
                        }
                    }
                }
                
                // Draw Corridor to previous room
                if (i > 0)
                {
                    DrawCorridor(generatedRooms[i-1].center, generatedRooms[i].center);
                }
            }
        }

        /// <summary>
        /// Creates a corridor connecting two center coordinates, randomly choosing between L-shaped and Diagonal, and varying widths.
        /// </summary>
        private void DrawCorridor(Vector2 start, Vector2 end)
        {
            Vector3Int startCell = groundTilemap.WorldToCell(new Vector3(start.x, start.y, 0));
            Vector3Int endCell = groundTilemap.WorldToCell(new Vector3(end.x, end.y, 0));

            BiomePalette normalPalette = GetPalette(BiomeType.Normal);
            int width = Random.Range(1, 4); // Random width: 1, 2, or 3 tiles

            // 50% chance for Diagonal vs L-Shaped
            if (Random.value < 0.5f)
            {
                // Diagonal corridors of width 1 pinch off due to wall corners filling the gaps. 
                // Force diagonal corridors to be at least 2 tiles wide so the player can always walk through.
                if (width < 2) width = 2;
                
                DrawDiagonalCorridor(startCell, endCell, width, normalPalette);
            }
            else
            {
                DrawLShapedCorridor(startCell, endCell, width, normalPalette);
            }
        }

        private void DrawLShapedCorridor(Vector3Int startCell, Vector3Int endCell, int width, BiomePalette palette)
        {
            Vector3Int current = startCell;

            // Randomize whether we go X first or Y first for a more organic feel
            if (Random.value < 0.5f)
            {
                while (current.x != endCell.x)
                {
                    current.x += (int)Mathf.Sign(endCell.x - current.x);
                    PaintCorridorPoint(current, width, palette);
                }
                while (current.y != endCell.y)
                {
                    current.y += (int)Mathf.Sign(endCell.y - current.y);
                    PaintCorridorPoint(current, width, palette);
                }
            }
            else
            {
                while (current.y != endCell.y)
                {
                    current.y += (int)Mathf.Sign(endCell.y - current.y);
                    PaintCorridorPoint(current, width, palette);
                }
                while (current.x != endCell.x)
                {
                    current.x += (int)Mathf.Sign(endCell.x - current.x);
                    PaintCorridorPoint(current, width, palette);
                }
            }
        }

        private void DrawDiagonalCorridor(Vector3Int startCell, Vector3Int endCell, int width, BiomePalette palette)
        {
            // Implementation of Bresenham's Line Algorithm
            int currentGridX = startCell.x;
            int currentGridY = startCell.y;
            int deltaX = Mathf.Abs(endCell.x - currentGridX);
            int deltaY = Mathf.Abs(endCell.y - currentGridY);
            int stepX = currentGridX < endCell.x ? 1 : -1;
            int stepY = currentGridY < endCell.y ? 1 : -1;
            int lineError = deltaX - deltaY;

            while (true)
            {
                PaintCorridorPoint(new Vector3Int(currentGridX, currentGridY, 0), width, palette);
                if (currentGridX == endCell.x && currentGridY == endCell.y) break;
                
                int errorMultiplier = 2 * lineError;
                if (errorMultiplier > -deltaY)
                {
                    lineError -= deltaY;
                    currentGridX += stepX;
                }
                if (errorMultiplier < deltaX)
                {
                    lineError += deltaX;
                    currentGridY += stepY;
                }
            }
        }

        private void PaintCorridorPoint(Vector3Int center, int width, BiomePalette palette)
        {
            // Calculates the offset to draw a square block of floor tiles around the center point
            int halfWidth = width / 2;
            int startOffset = (width % 2 == 0) ? -halfWidth + 1 : -halfWidth;
            int endOffset = halfWidth;

            for (int offsetX = startOffset; offsetX <= endOffset; offsetX++)
            {
                for (int offsetY = startOffset; offsetY <= endOffset; offsetY++)
                {
                    Vector3Int pos = center + new Vector3Int(offsetX, offsetY, 0);
                    SetCorridorFloorTile(pos, palette);
                }
            }
        }

        /// <summary>
        /// Helper mapping a corridor tile position to a Normal floor walkable.
        /// </summary>
        private void SetCorridorFloorTile(Vector3Int pos, BiomePalette palette)
        {
            if (!floorMap.ContainsKey(pos))
            {
                floorMap[pos] = BiomeType.Normal;
                if (palette != null)
                {
                    groundTilemap.SetTile(pos, palette.GetRandomFloor());
                }
            }
        }

        /// <summary>
        /// Analyzes floor grid neighbors to find outer edges and paints them on the wall tilemap,
        /// auto-orienting straight walls and corners according to biome-specific defaults.
        /// </summary>
        private void DrawWalls()
        {
            if (wallTilemap == null) return;
            wallTilemap.ClearAllTiles();

            HashSet<Vector3Int> wallPositions = new HashSet<Vector3Int>();

            Vector3Int[] neighbors = {
                Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right,
                new Vector3Int(1, 1, 0), new Vector3Int(1, -1, 0), new Vector3Int(-1, 1, 0), new Vector3Int(-1, -1, 0)
            };

            // Collect all wall positions adjacent to floor cells
            foreach (var kvp in floorMap)
            {
                Vector3Int pos = kvp.Key;
                foreach (var dir in neighbors)
                {
                    Vector3Int neighborPos = pos + dir;
                    if (!floorMap.ContainsKey(neighborPos))
                    {
                        wallPositions.Add(neighborPos);
                    }
                }
            }

            // Auto-tile each wall position based on its surrounding floor tiles
            foreach (Vector3Int wallPos in wallPositions)
            {
                // Find neighboring floor tiles
                bool floorNorth = floorMap.ContainsKey(wallPos + Vector3Int.up);
                bool floorSouth = floorMap.ContainsKey(wallPos + Vector3Int.down);
                bool floorEast  = floorMap.ContainsKey(wallPos + Vector3Int.right);
                bool floorWest  = floorMap.ContainsKey(wallPos + Vector3Int.left);

                // Find the first adjacent floor tile to determine the wall's biome theme
                Vector3Int adjacentFloorPos = Vector3Int.zero;
                bool foundAdjacentFloor = false;
                
                Vector3Int[] checkOrder = {
                    Vector3Int.down, Vector3Int.up, Vector3Int.right, Vector3Int.left,
                    new Vector3Int(1, -1, 0), new Vector3Int(-1, -1, 0), new Vector3Int(-1, 1, 0), new Vector3Int(1, 1, 0)
                };

                foreach (var dir in checkOrder)
                {
                    Vector3Int checkPos = wallPos + dir;
                    if (floorMap.ContainsKey(checkPos))
                    {
                        adjacentFloorPos = checkPos;
                        foundAdjacentFloor = true;
                        break;
                    }
                }

                if (!foundAdjacentFloor) continue;

                BiomeType biome = floorMap[adjacentFloorPos];
                BiomePalette palette = GetPalette(biome) ?? GetPalette(BiomeType.Normal);
                if (palette == null || palette.WallTiles == null || palette.WallTiles.Count == 0) continue;

                TileBase straightTile = palette.GetStraightWall();
                TileBase cornerTile = palette.GetCornerWall() ?? straightTile;

                TileBase tileToSet = straightTile;
                float rotationAngle = 0f;

                int orthogonalFloorCount = (floorNorth ? 1 : 0) + (floorSouth ? 1 : 0) + (floorEast ? 1 : 0) + (floorWest ? 1 : 0);

                if (orthogonalFloorCount >= 2)
                {
                    // Inner corner (multiple orthogonal floors adjacent)
                    tileToSet = cornerTile;
                    if (floorSouth && floorEast) rotationAngle = 90f;       // Top-Left interior
                    else if (floorSouth && floorWest) rotationAngle = 0f;   // Top-Right interior
                    else if (floorNorth && floorWest) rotationAngle = 270f; // Bottom-Right interior
                    else if (floorNorth && floorEast) rotationAngle = 180f; // Bottom-Left interior
                }
                else if (orthogonalFloorCount == 1)
                {
                    // Straight wall (exactly one orthogonal floor adjacent)
                    tileToSet = straightTile;
                    if (floorSouth) rotationAngle = 0f;        // Top wall
                    else if (floorEast) rotationAngle = 90f;   // Left wall
                    else if (floorNorth) rotationAngle = 180f; // Bottom wall
                    else if (floorWest) rotationAngle = 270f;  // Right wall
                }
                else
                {
                    // Outer corner (0 orthogonal floors, only diagonal floors adjacent)
                    bool floorNE = floorMap.ContainsKey(wallPos + new Vector3Int(1, 1, 0));
                    bool floorSE = floorMap.ContainsKey(wallPos + new Vector3Int(1, -1, 0));
                    bool floorSW = floorMap.ContainsKey(wallPos + new Vector3Int(-1, -1, 0));
                    bool floorNW = floorMap.ContainsKey(wallPos + new Vector3Int(-1, 1, 0));

                    tileToSet = cornerTile;
                    if (floorSE) rotationAngle = 90f;
                    else if (floorSW) rotationAngle = 0f;
                    else if (floorNW) rotationAngle = 270f;
                    else if (floorNE) rotationAngle = 180f;
                    else tileToSet = straightTile; // Fallback if completely isolated
                }

                // Paint the tile and set the rotation transform matrix
                wallTilemap.SetTile(wallPos, tileToSet);
                Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, rotationAngle), Vector3.one);
                wallTilemap.SetTransformMatrix(wallPos, matrix);
            }
        }

        /// <summary>
        /// Instantiates player and enemy spawning points inside the rooms.
        /// Spawns player in a normal room and allocates biome-matching enemies to all other rooms.
        /// </summary>
        private void GenerateSpawnNodes()
        {
            // 1. Pick a random Normal room for the player to spawn in
            var normalRooms = roomNodes.Where(r => r.RoomBiome == BiomeType.Normal).ToList();
            if (normalRooms.Count > 0)
            {
                RoomNode startRoom = normalRooms[Random.Range(0, normalRooms.Count)];
                
                Vector3 pos = GetRandomPositionInRoom(startRoom.Bounds);
                pos.z = -2f;
                GameObject node = new GameObject("PlayerSpawnNode");
                node.transform.position = pos;
                node.transform.SetParent(startRoom.transform);
                startRoom.PlayerSpawnNodes.Add(node.transform);

                // Position the player
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    player.transform.position = pos;
                }
            }

            // 2. Handle Enemy Spawning for all other rooms (and Normal rooms if you want, but user said Normal usually doesn't)
            foreach (var room in roomNodes)
            {
                // Skip spawning enemies in the room where the player is
                if (room.PlayerSpawnNodes.Count > 0) continue;

                // Handle Enemy Spawning (For non-starting rooms)
                GameObject enemyPrefab = room.RoomBiome switch
                {
                    BiomeType.Hot => hotEnemyPrefab,
                    BiomeType.Ocean => oceanEnemyPrefab,
                    BiomeType.Dirt => dirtEnemyPrefab,
                    _ => null
                };

                if (enemyPrefab != null)
                {
                    for (int i = 0; i < enemySpawnsPerRoom; i++)
                    {
                        Vector3 pos = GetRandomPositionInRoom(room.Bounds);
                        pos.z = -2f;
                        GameObject enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
                        enemy.name = $"{room.RoomBiome}_Enemy_{i}";
                        enemy.transform.SetParent(room.transform);
                        room.EnemySpawnNodes.Add(enemy.transform);
                    }
                }
            }
        }

        /// <summary>
        /// Selects a random cell location within the room bounds and converts it to world coordinates.
        /// </summary>
        private Vector3 GetRandomPositionInRoom(RectInt bounds)
        {
            int randomCellX = Random.Range(bounds.xMin, bounds.xMax);
            int randomCellY = Random.Range(bounds.yMin, bounds.yMax);
            return groundTilemap.GetCellCenterWorld(new Vector3Int(randomCellX, randomCellY, 0));
        }

        private void GetCategorizedPositions(RectInt bounds, out List<Vector3> corners, out List<Vector3> walls, out List<Vector3> centers, out List<Vector3> doorways)
        {
            corners = new List<Vector3>();
            walls = new List<Vector3>();
            centers = new List<Vector3>();
            doorways = new List<Vector3>();

            for (int cellX = bounds.xMin; cellX < bounds.xMax; cellX++)
            {
                for (int cellY = bounds.yMin; cellY < bounds.yMax; cellY++)
                {
                    Vector3Int cellPos = new Vector3Int(cellX, cellY, 0);
                    if (!floorMap.ContainsKey(cellPos)) continue;
                    
                    int adjacentWalls = 0;
                    bool isDoorway = false;
                    
                    Vector3Int[] neighbors = {
                        cellPos + Vector3Int.up, cellPos + Vector3Int.down, cellPos + Vector3Int.left, cellPos + Vector3Int.right
                    };

                    foreach (var n in neighbors)
                    {
                        if (!floorMap.ContainsKey(n))
                        {
                            adjacentWalls++;
                        }
                        else if (!bounds.Contains((Vector2Int)n))
                        {
                            isDoorway = true;
                        }
                    }

                    if (safeTiles.Contains(cellPos)) continue; // Never classify safe tiles as valid prop spots

                    Vector3 worldPos = groundTilemap.GetCellCenterWorld(cellPos);

                    if (isDoorway) doorways.Add(worldPos);
                    else if (adjacentWalls >= 2) corners.Add(worldPos);
                    else if (adjacentWalls == 1) walls.Add(worldPos);
                    else centers.Add(worldPos);
                }
            }
        }

        private Vector3 GetSmartPosition(List<Vector3> preferredList, RectInt fallbackBounds)
        {
            if (preferredList != null && preferredList.Count > 0)
            {
                int index = Random.Range(0, preferredList.Count);
                Vector3 pos = preferredList[index];
                preferredList.RemoveAt(index);
                return pos;
            }
            return GetRandomPositionInRoom(fallbackBounds);
        }

        /// <summary>
        /// Matches a BiomeType to its palette configuration asset.
        /// </summary>
        private BiomePalette GetPalette(BiomeType type)
        {
            return biomePalettes.FirstOrDefault(p => p.BiomeType == type);
        }
    }
}
