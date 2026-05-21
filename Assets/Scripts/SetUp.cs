using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Configuration class that holds all general constraint settings, parameters, and tile assets 
/// used by the Dungeon Generator to build levels.
/// </summary>
public class SetUp : MonoBehaviour
{
    // Including all map settings.
    [FoldoutGroup("Fixed Constraints")]
    [Tooltip("The initial seed for random level generation.")]
    [SerializeField]
    private int seed = 12345;

    [FoldoutGroup("Fixed Constraints")]
    [Tooltip("The width of the level in blocks.")]
    [SerializeField]
    private int dungeonWidth = 60;

    [FoldoutGroup("Fixed Constraints")]
    [Tooltip("The y-axis length of the dungeon.")]
    [SerializeField]
    private int dungeonHeight = 60;

    [FoldoutGroup("Fixed Constraints")]
    [Tooltip("Total number of biome rooms.")]
    [SerializeField]
    private int biomeRoomCount = 10;

    [FoldoutGroup("Tile Corridor")]
    [Tooltip("Floor (plain).")]
    [SerializeField]
    private TileBase floorTile;

    [FoldoutGroup("Tile Corridor")]
    [Tooltip("Wall.")]
    [SerializeField]
    private TileBase wallTile;

    [FoldoutGroup("Tile Corridor")]
    [Tooltip("Corner tile.")]
    [SerializeField]
    private TileBase cornerTile;

    [FoldoutGroup("Tile Dirt Room")]
    [Tooltip("Dirt Floor.")]
    [SerializeField]
    private TileBase floorDirtTile;

    [FoldoutGroup("Tile Dirt Room")]
    [Tooltip("Dirt Wall.")]
    [SerializeField]
    private TileBase wallDirtTile;

    [FoldoutGroup("Tile Dirt Room")]
    [Tooltip("Dirt Corner.")]
    [SerializeField]
    private TileBase cornerDirtTile;

    [FoldoutGroup("Tile Lava Room")]
    [Tooltip("Lava Floor.")]
    [SerializeField]
    private TileBase floorLavaTile;

    [FoldoutGroup("Tile Lava Room")]
    [Tooltip("Lava Wall.")]
    [SerializeField]
    private TileBase wallLavaTile;

    [FoldoutGroup("Tile Lava Room")]
    [Tooltip("Lava Corner.")]
    [SerializeField]
    private TileBase cornerLavaTile;

    [FoldoutGroup("Tile Ocean Room")]
    [Tooltip("Ocean Floor.")]
    [SerializeField]
    private TileBase floorOceanTile;

    [FoldoutGroup("Tile Ocean Room")]
    [Tooltip("Ocean Wall.")]
    [SerializeField]
    private TileBase wallOceanTile;

    [FoldoutGroup("Tile Ocean Room")]
    [Tooltip("Ocean Corner.")]
    [SerializeField]
    private TileBase cornerOceanTile;

    // InteractiveProps
    // Spikes
    // Non-interactive props
    // Boxes
    // Axes

    // --- Property Getters ---
    public int Seed => seed;
    public int DungeonWidth => dungeonWidth;
    public int DungeonHeight => dungeonHeight;
    public int BiomeRoomCount => biomeRoomCount;
    public TileBase FloorTile => floorTile;
    public TileBase WallTile => wallTile;
    public TileBase CornerTile => cornerTile;
    public TileBase FloorDirtTile => floorDirtTile;
    public TileBase WallDirtTile => wallDirtTile;
    public TileBase CornerDirtTile => cornerDirtTile;
    public TileBase FloorLavaTile => floorLavaTile;
    public TileBase WallLavaTile => wallLavaTile;
    public TileBase CornerLavaTile => cornerLavaTile;
    public TileBase FloorOceanTile => floorOceanTile;
    public TileBase WallOceanTile => wallOceanTile;
    public TileBase CornerOceanTile => cornerOceanTile;
}
