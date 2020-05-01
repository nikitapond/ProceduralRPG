using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public abstract class DungeonBuilder
{
    public int[,] BASE_DIRT;

    protected Vec2i ChunkPosition;
    protected Vec2i Size;
    protected Vec2i TileSize;
    protected DungeonType Type;

    protected WorldObjectData[,] DungeonObjects;
    protected Tile[,] DungeonTiles;
    public DungeonBuilder(Vec2i chunkPos, Vec2i size, DungeonType type)
    {
        ChunkPosition = chunkPos;
        Size = size;
        TileSize = Size * World.ChunkSize;
        Type = type;
        DungeonObjects = new WorldObjectData[TileSize.x, TileSize.z];
        DungeonTiles = new Tile[TileSize.x, TileSize.z];
        BASE_DIRT = new int[World.ChunkSize, World.ChunkSize];
        for (int x = 0; x < World.ChunkSize; x++)
        {
            for (int z = 0; z < World.ChunkSize; z++)
            {
                BASE_DIRT[x, z] = Tile.DIRT.ID;
            }
        }
    }

    public abstract Dungeon Generate(DungeonEntrance entr, GenerationRandom ran = null);



    protected abstract DungeonBoss GenerateDungeonBoss();

    protected abstract List<Entity> GenerateEntities();
    /// <summary>
    /// Generates all the chunks for the dungeon based on the 
    /// tile and object arrays
    /// </summary>
    /// <returns></returns>
    protected ChunkData[,] CreateChunks()
    {
        ChunkData[,] chunks = new ChunkData[Size.x, Size.z];

        for(int x=0; x < Size.x; x++)
        {
            for (int z=0; z < Size.z; z++)
            {
                Dictionary<int, WorldObjectData> objs = new Dictionary<int, WorldObjectData>();
                for(int x_=0; x_<World.ChunkSize; x_++)
                {
                    for(int z_=0; z_<World.ChunkSize; z_++)
                    {
                        if(DungeonObjects[x*World.ChunkSize + x_, z*World.ChunkSize + z_] != null)
                        {
                            objs.Add(WorldObject.ObjectPositionHash(x_, z_), DungeonObjects[x * World.ChunkSize + x_, z * World.ChunkSize + z_]);
                        }
                    }
                }
                chunks[x, z] = new ChunkData(x, z, (int[,])BASE_DIRT.Clone(), true, objects:objs);
            }
        }
        return chunks;
    }  

}




public enum DungeonType
{
    FIRE, CAVE
}