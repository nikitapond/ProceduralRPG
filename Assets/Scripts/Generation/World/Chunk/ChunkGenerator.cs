using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Diagnostics;
[System.Serializable]
public class ChunkGenerator
{


    private int[,] EMPTY_PLAINS;
    private int[,] EMPTY_DESERT;
    private int[,] MOUNTAIN;
    private int[,] OCEAN;
    private float[,] OCEAN_HEIGHT;
    private ChunkBase2[,] ChunkBases;
    private GameGenerator2 GameGen;
    private int Seed;
    /// <summary>
    /// Initiates the ChunkGenerator.
    /// 
    /// </summary>
    /// <param name="gameGen"></param>
    public ChunkGenerator(GameGenerator2 gameGen)
    {
        GameGen = gameGen;
        ChunkBases = gameGen.TerGen.ChunkBases;
        Seed = gameGen.Seed;
        //Set up default tile arrays 
        EMPTY_PLAINS = new int[World.ChunkSize, World.ChunkSize];
        OCEAN = new int[World.ChunkSize, World.ChunkSize];
        EMPTY_DESERT = new int[World.ChunkSize, World.ChunkSize];
        OCEAN_HEIGHT = new float[World.ChunkSize, World.ChunkSize];
        MOUNTAIN = new int[World.ChunkSize, World.ChunkSize];

        for (int x = 0; x < World.ChunkSize; x++)
        {
            for (int z = 0; z < World.ChunkSize; z++)
            {
                EMPTY_PLAINS[x, z] = Tile.GRASS.ID;
                OCEAN[x, z] = Tile.WATER.ID;
                EMPTY_DESERT[x, z] = Tile.SAND.ID;
                OCEAN_HEIGHT[x, z] = 1;
                MOUNTAIN[x, z] = Tile.STONE_FLOOR.ID;
            }
        }


    }

    /// <summary>
    /// Generates the chunk based on its biome, as well as things added to it such as forrests and wooded areas
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public ChunkData GenerateChunk(int x, int z)
    {



        ChunkBase2 cb = ChunkBases[x, z];
        ChunkData cd = null;
        if (cb.ChunkFeature is ChunkLake)
            cd = GenerateLakeChunk(x, z, cb);
        else if (cb.ChunkFeature is ChunkRiverNode)
            cd = GenerateRiverChunk(x, z, cb);
        else
            cd = GenerateSimpleChunk(x, z, cb);
        return cd;
    }



    /// <summary>
    /// Generates a chunk that has no special land features
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <param name="cb"></param>
    /// <returns></returns>
    private ChunkData GenerateSimpleChunk(int x, int z, ChunkBase2 cb)
    {
        
        if (cb.Biome == ChunkBiome.ocean)
        {
            return new ChunkData(x, z, (int[,])OCEAN.Clone(), false);
        }/*
        if(cb.Biome == ChunkBiome.dessert)
        {
            return new ChunkData(x, z, (int[,])EMPTY_DESERT.Clone(), cb.IsLand, 1, (float[,])OCEAN_HEIGHT.Clone());
        }if(cb.Biome == ChunkBiome.mountain)
        {
            return new ChunkData(x, z, (int[,])MOUNTAIN.Clone(), cb.IsLand, 1, (float[,])OCEAN_HEIGHT.Clone());

        }*/
        int[,] tiles = (int[,])(cb.Biome == ChunkBiome.dessert ? EMPTY_DESERT.Clone() : cb.Biome == ChunkBiome.mountain ? MOUNTAIN.Clone() : EMPTY_PLAINS.Clone());

        GenerationRandom genRan = new GenerationRandom(Seed + x << 16 + z);

        //if(genRan.Random() > 0.1f)
        //    return new ChunkData(x, z, tiles, cb.IsLand);

        Dictionary<int, WorldObjectData> obs = new Dictionary<int, WorldObjectData>(256);
        float[,] heights = new float[World.ChunkSize, World.ChunkSize];
        for(int i=0; i < World.ChunkSize; i++)
        {
            for(int j=0; j<World.ChunkSize; j++)
            {
                if(genRan.Random() < 0.01f)
                {
                    //obs.Add(WorldObject.ObjectPositionHash(i,j), new Tree(new Vec2i(x * World.ChunkSize + i, z * World.ChunkSize + j)));
                }else if(genRan.Random() < 0.3f)
                {
                    //obs.Add(WorldObject.ObjectPositionHash(i, j), new Tree(new Vec2i(x * World.ChunkSize + i, z * World.ChunkSize + j)));
                }
                heights[i, j] = GameGen.TerGen.GetWorldHeightAt(x * World.ChunkSize + i, z * World.ChunkSize + j);
            }
        }
        ChunkData cd = new ChunkData(x, z, tiles, true,cb.Height, heightMap: heights);
        return cd;
    }

    private ChunkData GenerateLakeChunk(int x, int z, ChunkBase2 cb)
    {
        ChunkData cd = new ChunkData(x, z, (int[,])OCEAN.Clone(), false);
        return cd;
    }

    private ChunkData GenerateRiverChunk(int x, int z, ChunkBase2 cb)
    {
        GenerationRandom genRan = new GenerationRandom(new Vec2i(x, z).GetHashCode() + Seed);
        int[,] tiles = new int[World.ChunkSize, World.ChunkSize];
        WorldObjectData[,] data = new WorldObjectData[World.ChunkSize, World.ChunkSize];
        float[,] heights = new float[World.ChunkSize, World.ChunkSize];
        Vec2i exitDelta = new Vec2i(1,0);
        Vec2i entrDelta = new Vec2i(-1,0);

        if (exitDelta == null)
        {
            exitDelta = new Vec2i(0, 0);
        }
        if (entrDelta == null)
        {
            entrDelta = new Vec2i(0, 0);
        }

        //Calculatee the tile position of the entrance and exit point of the river
        int entrX = (entrDelta.x == 1) ? 16 : ((entrDelta.x == 0) ? 8 : 0);
        int entrZ = (entrDelta.z == 1) ? 16 : ((entrDelta.z == 0) ? 8 : 0);

        int exitX = (exitDelta.x == 1) ? 16 : ((exitDelta.x == 0) ? 8 : 0);
        int exitZ = (exitDelta.z == 1) ? 16 : ((exitDelta.z == 0) ? 8 : 0);




        float dx = entrX - exitX;
        float dz = entrZ - exitZ;
        //If dx or dz is 0, then 
        float a, b, c;
        bool angle = (dx != 0 && dz != 0);
        float divBy = angle ? 2 : 1;
        if (dx == 0)
        {
            a = 0;
            b = 1;
            c = -entrX;
        }
        else if (dz == 0)
        {
            a = 1;
            b = 0;
            c = -entrZ;
        }
        else
        {
            float m = dz / dx;
            c = -(entrZ - m * entrX);

            a = 1;
            b = -m;
        }

        int inWidth = 8;
        int outWidth = 8;


        float dem_sqr = (a * a + b * b);
        ChunkVoxelData vox = new ChunkVoxelData();
        for (int tx = 0; tx < World.ChunkSize; tx++)
        {
            for (int tz = 0; tz < World.ChunkSize; tz++)
            {
                float baseheight = GameGen.TerGen.GetWorldHeightAt(x * World.ChunkSize + tx, z * World.ChunkSize + tz);

                float dist_sqr = ((a * tz + b * tx + c) * (a * tz + b * tx + c)) / dem_sqr;
                if (dist_sqr < (inWidth* inWidth) / divBy)
                {
                    Vector2 off = new Vector2(x * World.ChunkSize + tx, z * World.ChunkSize + tz);
                    //Debug.Log("here");
                    tiles[tx, tz] = Tile.WATER.ID;
                    //float baseheight = GameGen.TerrainGenerator.WorldHeight(x * World.ChunkSize + tx, z * World.ChunkSize + tz);
                    float lerp = dist_sqr / ((inWidth* inWidth) / divBy);


                    heights[tx, tz] = Mathf.Lerp(baseheight, baseheight-5, 1/lerp);
                    heights[tx, tz] = Mathf.Clamp(cb.Height - 5, 1, 16);
                    for(int y=Mathf.FloorToInt(heights[tx,tz]); y<baseheight; y++)
                    {
                        vox.SetVoxelNode(tx, y, tz, new VoxelNode(Voxel.glass));
                    }
                    /*
                    if (!(data[tx, tz] is Water))
                    {
                        data[tx, tz] = new Water(new Vec2i(x * World.ChunkSize + tx, z * World.ChunkSize + tz));
                        (data[tx, tz] as Water).SetUVOffset(off);
                    }

                    if (tx < World.ChunkSize - 1 && !(data[tx + 1, tz] is Water))
                    {
                        data[tx + 1, tz] = new Water(new Vec2i(x * World.ChunkSize + tx + 1, z * World.ChunkSize + tz));
                        (data[tx + 1, tz] as Water).SetUVOffset(off + new Vector2(1, 0));
                    }
                    if (tz < World.ChunkSize - 1 && !(data[tx, tz + 1] is Water))
                    {
                        data[tx, tz + 1] = new Water(new Vec2i(x * World.ChunkSize + tx, z * World.ChunkSize + tz + 1));
                        (data[tx, tz + 1] as Water).SetUVOffset(off + new Vector2(0, 1));
                    }
                    if (tx < World.ChunkSize - 1 && tz < World.ChunkSize - 1 && !(data[tx + 1, tz + 1] is Water))
                    {
                        data[tx + 1, tz + 1] = new Water(new Vec2i(x * World.ChunkSize + tx + 1, z * World.ChunkSize + tz + 1));
                        (data[tx + 1, tz + 1] as Water).SetUVOffset(off + new Vector2(1, 1));
                    }

                    if (tx > 0 && !(data[tx - 1, tz] is Water))
                    {
                        data[tx - 1, tz] = new Water(new Vec2i(x * World.ChunkSize + tx - 1, z * World.ChunkSize + tz));
                        (data[tx - 1, tz] as Water).SetUVOffset(off + new Vector2(-1, 0));
                    }
                    if (tz > 0 && !(data[tx, tz - 1] is Water))
                    {
                        data[tx, tz - 1] = new Water(new Vec2i(x * World.ChunkSize + tx, z * World.ChunkSize + tz - 1));
                        (data[tx, tz - 1] as Water).SetUVOffset(off + new Vector2(0, -1));
                    }
                    if (tx > 0 && tz > 0 && !(data[tx - 1, tz - 1] is Water))
                    {
                        data[tx - 1, tz - 1] = new Water(new Vec2i(x * World.ChunkSize + tx - 1, z * World.ChunkSize + tz - 1));
                        (data[tx - 1, tz - 1] as Water).SetUVOffset(off + new Vector2(-1, -1));
                    }

                    if (tx > 0 && tz < World.ChunkSize - 1 && !(data[tx - 1, tz + 1] is Water))
                    {
                        data[tx - 1, tz + 1] = new Water(new Vec2i(x * World.ChunkSize + tx - 1, z * World.ChunkSize + tz + 1));
                        (data[tx - 1, tz + 1] as Water).SetUVOffset(off + new Vector2(-1, +1));
                    }
                    if (tz > 0 && tx < World.ChunkSize - 1 && !(data[tx + 1, tz - 1] is Water))
                    {
                        data[tx + 1, tz - 1] = new Water(new Vec2i(x * World.ChunkSize + tx + 1, z * World.ChunkSize + tz - 1));
                        (data[tx + 1, tz - 1] as Water).SetUVOffset(off + new Vector2(1, -1));
                    }*/

                }
                else if (dist_sqr < (inWidth * inWidth) * 2 / divBy)
                {
                    tiles[tx, tz] = Tile.SAND.ID;
                    heights[tx, tz] = baseheight;
                    for (int y = Mathf.FloorToInt(heights[tx, tz]); y < baseheight; y++)
                    {
                        vox.SetVoxelNode(tx, y, tz, new VoxelNode(Voxel.glass));
                    }
                }
                else
                {
                    tiles[tx, tz] = Tile.GRASS.ID;
                    heights[tx, tz] = baseheight;

                    // heights[tx, tz] = cb.BaseHeight;

                    //if (genRan.Random() < 0.25f)
                    //    data[tx, tz] = new Grass(new Vec2i(x * World.ChunkSize + tx + 1, z * World.ChunkSize + tz - 1));

                }
            }
        }

        //data[0, 0] = new Tree(new Vec2i(x * World.ChunkSize, z * World.ChunkSize));

        //data[2, 2] = new RockFormation(new Vec2i(x * World.ChunkSize + 2, z * World.ChunkSize + 2));


        Dictionary<int, WorldObjectData> data_ = new Dictionary<int, WorldObjectData>();

        for(int i=0; i<World.ChunkSize; i++)
        {
            for(int j=0; j<World.ChunkSize; j++)
            {
                if (data[i, j] != null)
                    data_.Add(WorldObject.ObjectPositionHash(i, j), data[i, j]);
            }
        }
        ChunkData cd = new ChunkData(x, z, tiles, true, baseHeight: cb.Height, heightMap: heights);
        cd.SetVoxelData(vox);
        return cd;
    }


    private void GenerateRiverBridge(ChunkBase cb, WorldObjectData[,] chunkObjs, int bridgeWidth=5)
    {
        RiverNode rn = cb.RiverNode;
        int rDirX = Mathf.Abs(rn.RiverNodeDirection().x);
        int rDirZ = Mathf.Abs(rn.RiverNodeDirection().z);

        if (rDirX == 1 && rDirZ == 1)
            return;

        Vec2i absDir = new Vec2i(rDirX, rDirZ);

        int riverWidth = (int)rn.EntranceWidth;
        int halfWidth = bridgeWidth/2;

        Vec2i start, end;
        if(rDirX == 1)
        {
            start = new Vec2i(World.ChunkSize / 2 - halfWidth, World.ChunkSize / 2 - riverWidth);
            end = new Vec2i(World.ChunkSize / 2 + halfWidth, World.ChunkSize / 2 + riverWidth + 1);
        }
        else
        {
            start = new Vec2i(World.ChunkSize / 2 - riverWidth, World.ChunkSize / 2 - halfWidth);
            end = new Vec2i(World.ChunkSize / 2 + riverWidth + 1, World.ChunkSize / 2 + halfWidth);
        }
        

     //   RiverBridgeObject rbObj = new RiverBridgeObject(start, end, absDir);
     //   IMultiTileObjectChild[,] childs = rbObj.GetChildren();
     /*
        for (int x = 0; x < rbObj.Size.x; x++)
        {
            for (int z = 0; z < rbObj.Size.z; z++)
            {
                chunkObjs[start.x + x, start.z + z] = childs[x, z] as WorldObjectData;
            }
        }*/

        /*
        //If these do not sum to 1, the direction is not simple (i.e, diagonal) and no bridge can be made
        if (rnDir.x + rnDir.z != 1)
            return;
        //If the river is travelling in the z direction
        if (rnDir.x == 0)
        {
            Vec2i start = new Vec2i(World.ChunkSize / 2 - halfWidth, World.ChunkSize / 2 - riverWidth);
            Vec2i end = new Vec2i(World.ChunkSize / 2 + halfWidth, World.ChunkSize / 2 + riverWidth);

            RiverBridgeObject rbObj = new RiverBridgeObject(start, end, new Vec2i(1,0));
            IMultiTileObjectChild[,] childs = rbObj.GetChildren();
            for(int x=0; x<rbObj.Size.x; x++)
            {
                for(int z=0; z<rbObj.Size.z; z++)
                {
                    chunkObjs[start.x + x, start.z + z] = childs[x, z] as WorldObjectData;
                }
            }
        }
        else
        {

        }*/

    }
}