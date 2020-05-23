using UnityEngine;
using UnityEditor;

/// <summary>
/// Tile is the class that holds the data for the floor of the world - 
/// </summary>
/// 
[System.Serializable]
public class Tile
{

    public static Tile FromID(int id)
    {
        switch (id)
        {
            case 0:
                return Tile.NULL;
            case 15:
                return Tile.WATER;
            case 1:
                return GRASS;
            case 2:
                return WOOD_FLOOR;
            case 3:
                return STONE_FLOOR;
            case 5:
                return TEST_WROK_TILE;
            case 6:
                return TEST_RED;
            case 7:
                return TEST_BLUE;
            case 8:
                return TEST_MAGENTA;
            case 9:
                return TEST_YELLOW;
            case 10:
                return TEST_GREY;
            case 11:
                return TEST_PURPLE;
            case 12:
                return STONE_PATH;
            case 13:
                return SAND;
            case 14:
                return DIRT;
        }
        return NULL;
    }

    public static readonly Tile NULL = new Tile(0, Color.magenta, 1);
    public static readonly Tile WATER = new Tile(15, Color.blue, 0.3f);
    public static readonly Tile GRASS = new Tile(1, Color.green, 1f);
    public static readonly Tile WOOD_FLOOR = new Tile(2, new Color(0.6f, 0.3f, 0), 1.5f);
    public static readonly Tile STONE_FLOOR = new Tile(3, new Color(0.8f, 0.8f, 0.8f), 2f);
    public static readonly Tile TEST_WROK_TILE = new Tile(4, Color.magenta, 2f);

    public static readonly Tile TEST_RED = new Tile(6, Color.red, 2);
    public static readonly Tile TEST_BLUE = new Tile(7, Color.blue, 2);
    public static readonly Tile TEST_MAGENTA = new Tile(8, Color.magenta, 2);
    public static readonly Tile TEST_YELLOW = new Tile(9, Color.yellow, 2);
    public static readonly Tile TEST_GREY = new Tile(10, Color.gray, 2);
    public static readonly Tile TEST_PURPLE = new Tile(11, new Color(1, 0, 1), 2);
    public static readonly Tile TEST_GREEN = new Tile(15, new Color(0, 1, 0), 2);
    public static readonly Tile STONE_PATH = new Tile(12, new Color(0.8f, 0.8f, 0.8f), 2f);

    public static readonly Tile SAND = new Tile(13, new Color(1, 1, 0), 0.8f);
    public static readonly Tile DIRT = new Tile(14, new Color(150f / 255f, 75f / 255f, 0), 0.8f);
    
    public int ID { get; private set; } //The ID of the tile
    private float[] _Color;
    public float SpeedMultiplier { get; private set; } //The multiplier to an entities speed when travelling on this tile

    /// <summary>
    /// Private constructor - tiles should only be accessed via the static values at the beginning of this class.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="tileColour"></param>
    /// <param name="speedMultiplier"></param>
    private Tile(int id, Color color, float speedMultiplier)
    {
        ID = id;
        _Color = new float[] { color.r, color.g, color.b };
        SpeedMultiplier = speedMultiplier;
    }

    public Color GetColor()
    {
        return new Color(_Color[0], _Color[1], _Color[2]);
    }

    public static Tile GetFromBiome(ChunkBiome b)
    {
        switch (b)
        {
            case ChunkBiome.dessert:
                return Tile.SAND;
            case ChunkBiome.forrest:
                return Tile.GRASS;
            case ChunkBiome.grassland:
                return Tile.GRASS;
            case ChunkBiome.ocean:
                return Tile.WATER;
        }
        return Tile.TEST_MAGENTA;
    }
}