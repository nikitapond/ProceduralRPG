using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
/// <summary>
/// An object that holds onto all tiles and 
/// objects for an area that is being generated.
/// Allows user to build up a structure via placing 'walls' etc
/// Then constructs these walls into the required objects
/// </summary>
public abstract class GenerationBase
{
    /// <summary>
    /// Position of the chunk at lowest x and z coordinate for this generator
    /// </summary>
    public Vec2i BaseChunk { get; private set; }

    public Vec2i ChunkSize { get; private set; }
    public Vec2i BaseCoord { get; private set; }
    public Vec2i Size { get; private set; }

    protected WorldObjectData[,] Objects;
    protected Tile[,] Tiles;

    protected Vec2i EntrancePosition;
    protected Vec2i[] BoundryPoints;

    private GenerationRandom GenRan;
    public GenerationBase(Vec2i baseChunk, Vec2i chunkSize, GenerationRandom genRan)
    {
        BaseChunk = baseChunk;
        ChunkSize = chunkSize;
        BaseCoord = baseChunk*World.ChunkSize;
        Size = chunkSize*World.ChunkSize;
        Objects = new WorldObjectData[Size.x, Size.z];
        Tiles = new Tile[Size.x, Size.z];

        GenRan = genRan;
    }


    public abstract void Generate();


    public virtual void SetInsideTile(int tileID)
    {
        for(int x=0; x<Size.x; x++)
        {
            for (int z = 0; z < Size.z; z++)
            {
                if(IsPointInPolygonBounds(new Vec2i(x, z)))
                {
                    Tiles[x, z] = Tile.FromID(tileID);
                }
            }
        }
    }

    /// <summary>
    /// Connects the two specified points with a set of copys of the specified object.
    /// </summary>
    /// <param name="copy"></param>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public void ConnectPointsWithObject(WorldObjectData copy, Vec2i a, Vec2i b)
    {
        int x = a.x;
        int z = a.z;

        int xDir = (int)Mathf.Sign(b.x - a.x);
        int zDir = (int)Mathf.Sign(b.z - a.z);

        while (!(x == b.x && z == b.z))
        {
            Objects[x, z] = copy.Copy(BaseCoord + new Vec2i(x, z));



            int dx = (int)Mathf.Abs(b.x - x);
            int dz = (int)Mathf.Abs(b.z - z);

            if (dx > dz)
            {
                x += xDir;
            }
            else if (dz > dx)
            {
                z += zDir;

            }
            else
            {
                x += xDir;
                z += zDir;
            }

        }
    }


    /// <summary>
    /// Generates the boundary points for the wall.
    /// Default version creates a solid rectangle about Generator
    /// </summary>
    /// <returns></returns>
    public virtual Vec2i[] DefineWallBounds()
    {
        return DefineRectangularWall();
    }
    /// <summary>
    /// Creates a rectangular wall about the 
    /// </summary>
    /// <param name="externalOffset"></param>
    /// <returns></returns>
    protected Vec2i[] DefineRectangularWall(int externalOffset=0)
    {
        Vec2i[] points = new Vec2i[4];
        points[0] = new Vec2i(externalOffset, externalOffset);
        points[1] = new Vec2i(Size.x - 1- externalOffset, externalOffset);
        points[2] = new Vec2i(Size.x - 1- externalOffset, Size.z - 1- externalOffset);
        points[3] = new Vec2i(externalOffset, Size.z - 1- externalOffset);

        return points;
    }
    protected Vec2i[] DefinePolygonWall(int pointCount = -1)
    {
        if (pointCount == -1)
            pointCount = GenRan.RandomInt(4, 8);
        Vec2i[] points = new Vec2i[pointCount];
        Vec2i mid = Size / 2;

        //The change in angle between each point
        float dTheta = 2 * Mathf.PI / pointCount;


        for (int i = 0; i < pointCount; i++)
        {
            float theta = i * dTheta;
            float sinT = Mathf.Sin(theta);
            float cosT = Mathf.Cos(theta);

            float rMax_wClamp = Size.x / (2 * Mathf.Abs(sinT));
            float rMax_hClamp = Size.z / (2 * Mathf.Abs(cosT));
            int maxR = Mathf.FloorToInt(Mathf.Min(rMax_wClamp, rMax_hClamp));

            int R = GenRan.RandomInt(maxR / 2, maxR);

            int wallX = Mathf.Clamp((int)(mid.x + sinT * R), 0, Size.x - 1);
            int wallZ = Mathf.Clamp((int)(mid.z + cosT * R), 0, Size.z - 1);

            points[i] = new Vec2i(wallX, wallZ);

        }

        return points;
    }

    /// <summary>
    /// Chooses an entrance point based on the wall points.
    /// Randomly chooses 2 points and places a wall directly in the
    /// middle of them
    /// </summary>
    /// <returns></returns>
    public virtual Vec2i ChooseEntrancePoint()
    {
        int index = GenRan.RandomInt(0, BoundryPoints.Length);
        int indexP1 = (index + 1) % BoundryPoints.Length;

        Vec2i halfDif = (BoundryPoints[indexP1] - BoundryPoints[index]) / 2;
        Vec2i pos = BoundryPoints[index] + halfDif;

        return pos;
    }



    /// <summary>
    /// Converts the data of this generator to a list of chunks
    /// </summary>
    /// <returns></returns>
    public List<ChunkData2> ToChunkData()
    {
        List<ChunkData2> data = new List<ChunkData2>(ChunkSize.x * ChunkSize.z);
        for (int cx = 0; cx < ChunkSize.x; cx++)
        {
            for (int cz = 0; cz < ChunkSize.z; cz++)
            {
                int[,] chunkTiles = new int[World.ChunkSize, World.ChunkSize];
                Dictionary<int, WorldObjectData> chunkObjs = new Dictionary<int, WorldObjectData>();
                ChunkBase cb = GameGenerator.Instance.TerrainGenerator.ChunkBases[BaseChunk.x + cx, BaseChunk.z + cz];

                int cbTileBase = Tile.GetFromBiome(cb.Biome).ID;

                for (int x = 0; x < World.ChunkSize; x++)
                {
                    for (int z = 0; z < World.ChunkSize; z++)
                    {
                        int tx = cx * World.ChunkSize + x;
                        int tz = cz * World.ChunkSize + z;
                        if(Tiles[tx,tz] == null)
                        {
                            chunkTiles[x, z] = cbTileBase;
                        }
                        else
                        {
                            chunkTiles[x, z] = Tiles[tx, tz].ID;
                        }
                        
                        if (Objects[tx, tz] != null)
                        {
                            chunkObjs.Add(WorldObject.ObjectPositionHash(x, z), Objects[tx, tz]);
                        }
                    }
                }
                data.Add(new ChunkData2(BaseChunk.x + cx, BaseChunk.z + cz, chunkTiles, true, baseHeight: cb.BaseHeight, null));
            }
        }
        return data;

    }

    public bool InRectBounds(int x, int z)
    {
        return x >= 0 && z >= 0 && x < Size.x && z < Size.z;
    }
    public bool InRectBounds(Vec2i v)
    {
        return v.x >= 0 && v.z >= 0 && v.x < Size.x && v.z < Size.z;
    }
    /// <summary>
    /// Determines if the given point is inside the polygon that defines
    /// the bounds of this generator
    /// </summary>
    /// <param name="testPoint">the given point</param>
    /// <returns>true if the point is inside the polygon; otherwise, false</returns>
    public bool IsPointInPolygonBounds(Vec2i testPoint)
    {
        if (!InRectBounds(testPoint))
            return false;
        bool result = false;
        int j = BoundryPoints.Length - 1;
        for (int i = 0; i < BoundryPoints.Length; i++)
        {
            if (BoundryPoints[i].z < testPoint.z && BoundryPoints[j].z >= testPoint.z || BoundryPoints[j].z < testPoint.z && BoundryPoints[i].z >= testPoint.z)
            {
                if (BoundryPoints[i].x + (testPoint.z - BoundryPoints[i].z) / (BoundryPoints[j].z - BoundryPoints[i].z) * (BoundryPoints[j].x - BoundryPoints[i].x) < testPoint.x)
                {
                    result = !result;
                }
            }
            j = i;
        }
        return result;
    }

    /// <summary>
    /// Adds given object to the array of building objects. 
    /// Checks if the designated spot is already filled, if so we do not place the object and returns false.
    /// If the object covers a single tile, we add it and return true
    /// If the object is multi tile, we check if it is placed within bounds, and if every child tile is free.
    /// if so, we return true, otherwise we return false and do not place the object
    /// </summary>
    /// <param name="current"></param>
    /// <param name="nObj"></param>
    /// <param name="x"></param>
    /// <param name="z"></param>
    public bool AddObject(WorldObjectData nObj, int x, int z)
    {
        //If not in bounds, return false
        if (!(x > 0 && x < Size.x && z > 0 && z < Size.z))
        {
            return false;
        }
        //Check if this is a single tile object
        if (!(nObj is IMultiTileObject))
        {
            //Single tile objects, we only need to check the single tile
            if (Objects[x, z] == null)
            {
                //If the tile is empty, add object and return true
                Objects[x, z] = nObj;
                return true;
            }
            //If the tile is taken, we don't place the object
            return false;

        }
        if (!(x+nObj.Size.x < Size.x && z + nObj.Size.z < Size.z))
        {
            //If the bounds of the multiu tile object are too big, return false;
            return false;
        }
        //For multi tile objects, we iterate the whole size
        for (int i = 0; i < nObj.Size.x; i++)
        {
            for (int j = 0; j < nObj.Size.z; j++)
            {
                //if any of the tiles is not null, we don't place and return false
                if (Objects[x + i, z + j] != null)
                    return false;
            }
        }
        //Set reference, then get chilren
        Objects[x, z] = nObj;
        IMultiTileObjectChild[,] children = (nObj as IMultiTileObject).GetChildren();
        //Iterate again to set children
        for (int i = 0; i < nObj.Size.x; i++)
        {
            for (int j = 0; j < nObj.Size.z; j++)
            {
                if (i == 0 && j == 0)
                    continue;

                //if any of the tiles is not null, we don't place and return false
                Objects[x + i, z + j] = (children[i, j] as WorldObjectData);

            }
        }

        return true;
    }

}

public interface IChunkStructureGenerationBase
{
    IInventoryObject GetMainLootChest();
}