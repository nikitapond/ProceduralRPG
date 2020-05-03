using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
/// <summary>
/// The abstracted base class for all locations that are generated in the world.
/// Holds onto the height maps, object maps, and voxel maps for all chunks within it
/// 
/// </summary>
public abstract class BuilderBase
{

    public Vec2i BaseChunk { get; private set; } //Minimum point in chunk coords
    public Vec2i ChunkSize { get; private set; } //Size in chunks
    public Vec2i BaseTile { get; private set; } //Minimum point in world coords
    public Vec2i TileSize { get; private set; } //Size in tiles

    protected ChunkBase[,] ChunkBases;
    private ChunkVoxelData[,] ChunkVoxels;
    private float[,][,] HeightMaps;
    private int[,][,] TileMaps;
    private List<WorldObjectData>[,] ObjectMaps;

    public BuilderBase(Vec2i baseChunk, Vec2i chunkSize)
    {
        BaseChunk = baseChunk;
        ChunkSize = chunkSize;
        BaseTile = BaseChunk*World.ChunkSize;
        TileSize = ChunkSize * World.ChunkSize;

        ChunkVoxels = new ChunkVoxelData[ChunkSize.x, ChunkSize.z];
        HeightMaps = new float[ChunkSize.x, ChunkSize.z][,];
        TileMaps = new int[ChunkSize.x, ChunkSize.z][,];
        ObjectMaps = new List<WorldObjectData>[ChunkSize.x, ChunkSize.z];

        if (GameGenerator.Instance != null && GameGenerator.Instance.TerrainGenerator != null)
        {
            ChunkBases = new ChunkBase[ChunkSize.x, ChunkSize.z];
            
        }

        for (int x=0; x<ChunkSize.x; x++)
        {
            for(int z=0; z<ChunkSize.z; z++)
            {
                HeightMaps[x, z] = new float[World.ChunkSize, World.ChunkSize];

                if (ChunkBases != null)
                    ChunkBases[x, z] = GameGenerator.Instance.TerrainGenerator.ChunkBases[x + BaseChunk.x, z + BaseChunk.z];
                float baseHeight = ChunkBases == null ? 0 : ChunkBases[x, z].BaseHeight;
                for (int x_=0; x_<World.ChunkSize; x_++)
                {
                    for (int z_ = 0; z_ < World.ChunkSize; z_++)
                    {
                        HeightMaps[x, z][x_,z_] = baseHeight;
                    }
                }
                TileMaps[x, z] = new int[World.ChunkSize, World.ChunkSize];
                ChunkVoxels[x, z] = new ChunkVoxelData();
                
            }
        }

        

    }

    public bool AddObject(WorldObjectData data, bool force = false)
    {

        //Find local chunk position of object
        Vec2i cPos = World.GetChunkPosition(Vec2i.FromVector3(data.Position));
        //The global position of this object
        Vector3 finalPos = (BaseChunk * World.ChunkSize).AsVector3() + data.Position;
        //If force, we do not check for collision with other objects.
        if (force)
        {
            if (ObjectMaps[cPos.x, cPos.z] == null)
            {
                ObjectMaps[cPos.x, cPos.z] = new List<WorldObjectData>();

            }
            ObjectMaps[cPos.x, cPos.z].Add(data);


            return true;
        }

        if(ObjectMaps[cPos.x, cPos.z] != null)
        {
            //iterate all obejcts in chunk, check for intersection
            foreach(WorldObjectData objDat in ObjectMaps[cPos.x, cPos.z])
            {
                //If we intersect, return false and don't place the object
                if (data.Intersects(objDat))
                    return false;
            }



            ObjectMaps[cPos.x, cPos.z].Add(data);
            return true;
        }
        else
        {
            ObjectMaps[cPos.x, cPos.z] = new List<WorldObjectData>();
            ObjectMaps[cPos.x, cPos.z].Add(data);
            return true;
        }
        
    }


    public void SetHeight(int x, int z, float y)
    {
        int cx = WorldToChunk(x);
        int cz = WorldToChunk(z);
        int tx = x % World.ChunkSize;
        int tz = z % World.ChunkSize;
        HeightMaps[cx, cz][tx, tz] = y;
    
            
    }

    public void SetTile(int x, int z, Tile tile)
    {
        if (tile == null)
            return;
        int cx = WorldToChunk(x);
        int cz = WorldToChunk(z);
        int tx = x % World.ChunkSize;
        int tz = z % World.ChunkSize;
        TileMaps[cx, cz][tx, tz] = tile.ID;
    }
    public int GetTile(int x, int z)
    {
        int cx = WorldToChunk(x);
        int cz = WorldToChunk(z);
        int tx = x % World.ChunkSize;
        int tz = z % World.ChunkSize;
        return TileMaps[cx, cz][tx, tz];
    }
    public void SetTiles(int x, int z, int width, int height, Tile[,] tiles)
    {
        
        int cx = WorldToChunk(x);
        int cz = WorldToChunk(z);
       
        for(int x_=0; x_<width; x_++)
        {
            for(int z_=0; z_<height; z_++)
            {
                if (tiles[x_, z_] == null)
                    continue;
                int tx = (x_+x) % World.ChunkSize;
                int tz = (z_+z) % World.ChunkSize;
                //If tx or tz are 0, then cz must have changed - update it
                if (tx == 0)
                    cx = (WorldToChunk(x + x_));
                if (tz == 0)
                    cz = (WorldToChunk(x + x_));

                TileMaps[cx, cz][tx, tz] = tiles[x_, z_].ID;
            }
        }
    }
    public void SetVoxel(int x, int y, int z, Voxel vox)
    {
        int cx = WorldToChunk(x);
        int cz = WorldToChunk(z);
        int tx = x % World.ChunkSize;
        int tz = z % World.ChunkSize;
        
        ChunkVoxels[cx, cz].SetVoxel(tx, y, tz, vox);
        if(tx == 0 && tz == 0 && cx>0 && cz>0)
        {
            ChunkVoxels[cx - 1, cz - 1].SetVoxel(World.ChunkSize, y, World.ChunkSize, vox);
        }else if (tx == 0 && cx > 0)
        {
            ChunkVoxels[cx - 1, cz].SetVoxel(World.ChunkSize, y, tz, vox);
        }
        else if (tz == 0 && cz > 0)
        {
            ChunkVoxels[cx, cz-1].SetVoxel(tx, y, World.ChunkSize, vox);
        }

    }
    public void SetVoxels(int x, int y, int z, BuildingVoxels vox)
    {
        int cx = WorldToChunk(x);
        int cz = WorldToChunk(z);
   
        for (int x_ = 0; x_ < vox.Width; x_++)
        {
            for (int z_ = 0; z_ < vox.Depth; z_++)
            {
                int tx = (x_ + x) % World.ChunkSize;
                int tz = (z_ + z) % World.ChunkSize;
                //If tx or tz are 0, then cz must have changed - update it
                if (tx == 0)
                    cx = (WorldToChunk(x + x_));
                if (tz == 0)
                    cz = (WorldToChunk(x + x_));
                for(int y_=0; y_<vox.Height; y_++)
                {
                    ChunkVoxels[cx, cz].SetVoxel(tx, y_+y, tz, vox.GetVoxel(x_,y_, z_));

                }

            }
        }
    }

    public bool InBounds(int x, int y, int z)
    {
        return x >= 0 && z >= 0 && x < TileSize.x && z < TileSize.z && y > 0 && y < World.ChunkHeight;
    }
    public bool InBounds(int x, int z)
    {
        return x >= 0 && z >= 0 && x < TileSize.x && z < TileSize.z;
    }
    public Voxel GetVoxel(int x, int y, int z)
    {
        int cx = WorldToChunk(x);
        int cz = WorldToChunk(z);
        int tx = x % World.ChunkSize;
        int tz = z % World.ChunkSize;
        return ChunkVoxels[cx, cz].GetVoxel(tx, y, tz);
    }


    public List<ChunkData2> ToChunkData()
    {
        List<ChunkData2> chunks = new List<ChunkData2>(ChunkSize.x * ChunkSize.z);

        for(int x=0; x<ChunkSize.x; x++)
        {
            for (int z = 0; z < ChunkSize.z; z++)
            {
                float height = ChunkBases == null ? 0 : ChunkBases[x, z].BaseHeight;
                ChunkData2 chunk_xz = new ChunkData2(BaseChunk.x + x, BaseChunk.z + z, TileMaps[x, z], true, height, HeightMaps[x, z], ObjectMaps[x,z]);
                chunk_xz.SetVoxelData(ChunkVoxels[x, z]);
                chunks.Add(chunk_xz);
            }
        }

        return chunks;
    }

    protected static int WorldToChunk(int w)
    {
        return Mathf.FloorToInt((float)w / World.ChunkSize);
    }


}