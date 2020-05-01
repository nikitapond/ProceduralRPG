using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class BanditCampBuilder
{


    private Vec2i TileSize;
    private int[,] Tiles;
    private WorldObjectData[,] Objects;
    private BanditCamp Shell;
    private GenerationRandom GenRan;

    private Vec2i EntrancePosition;
    private Vec2i[] BoundaryPoints;
    private Vec2i TileBase;
    public IInventoryObject FinalLootChest { get; private set; }
    

    public BanditCampBuilder(BanditCamp shell)
    {
        Shell = shell;
        TileSize = shell.Size * World.ChunkSize;
        TileBase = Shell.Position * World.ChunkSize; ;

        Tiles = new int[TileSize.x, TileSize.z];
        Objects = new WorldObjectData[TileSize.x, TileSize.z];
    }


    public List<ChunkData> Generate(GenerationRandom genRan)
    {

        GenRan = genRan;

        for(int cx=0; cx<Shell.Size.x; cx++)
        {
            for (int cz = 0; cz < Shell.Size.z; cz++)
            {
                ChunkBase cb = GameGenerator.Instance.TerrainGenerator.ChunkBases[cx+Shell.Position.x, cz+Shell.Position.z];
                int id = Tile.GetFromBiome(cb.Biome).ID;
                for(int x=0; x<World.ChunkSize; x++)
                {
                    for (int z = 0; z < World.ChunkSize; z++)
                    {
                        Tiles[cx * World.ChunkSize + x, cz * World.ChunkSize + z] = id;
                    }
                }
            }
        }

        GenerateWall();

        Vec2i tilebase = Shell.Position * World.ChunkSize;
        
        if (Shell.BanditCampLevel > 1 && Shell.Size.x>3 && Shell.Size.z > 3)
        {
            //If this camp is large enough, generate a dungeon entrance.

            Vec2i localPos = GetRandomPointNearWall(stepIn:5);

            CaveDungeonEntrance entr = new CaveDungeonEntrance(tilebase + localPos, null, new WorldObjectMetaData(direction: new Vec2i(1, 0)));
            IMultiTileObjectChild[,] children = entr.GetChildren();
            Objects[localPos.x, localPos.z] = entr;
            for(int x=0; x<entr.Size.x; x++)
            {
                for (int z = 0; z < entr.Size.z; z++)
                {
                    if (x == 0 && z == 0)
                        continue;
                    Objects[localPos.x + x, localPos.z + z] = children[x, z] as WorldObjectData;
                }
            }
            Debug.Log("Generated Bandit Camp with Dungeon at " + this.Shell.Position, Debug.CHUNK_STRUCTURE_GENERATION);
            
            Shell.SetDungeonEntrance(entr);
            entr.SetChunkStructure(Shell);
        }
        else
        {
            Debug.Log("Generated Bandit Camp no Dungeon at " + this.Shell.Position, Debug.CHUNK_STRUCTURE_GENERATION);

        }

        for(int i=0; i<4; i++)
        {
            AddGaurdTower();
        }

        Vec2i chestPos = RandomPointWithinBounds();

        while(Objects[chestPos.x, chestPos.z] != null)
        {
            chestPos = RandomPointWithinBounds();
        }
        Chest chest = new Chest(chestPos + tilebase);

        Debug.Log("Placing chest at " + chestPos + "_ " + chest + "_" + chest.GetInventory());

        Objects[chestPos.x, chestPos.z] = chest;
        //Objects[11, 11] = new LootSack(tilebase + new Vec2i(11, 11));

        FinalLootChest = chest;
        Debug.Log("final loot chest " + FinalLootChest);
        EntityFaction banditFaction = new EntityFaction("Bandit_Camp");
        for(int x=0; x<Shell.Size.x; x++)
        {
            for (int z = 0; z < Shell.Size.z; z++)
            {
                //Entity e = new Bandit();
                //e.SetPosition(tilebase + new Vec2i(x * World.ChunkSize + 5, z * World.ChunkSize + z + 3));
                //Shell.AddEntity(x, z, e);
                //e.SetEntityFaction(banditFaction);
            }
        }
        Entity e = new Bandit();
        e.SetPosition(tilebase + new Vec2i(2 * World.ChunkSize + 5, 2 * World.ChunkSize + 2 + 3));
        Shell.AddEntity(0, 0, e);
        e.SetEntityFaction(banditFaction);



        return ToChunkData();
    }

    public void AddGaurdTower()
    {
        Recti bound = FindObjectPlot(new Vec2i(3, 3));
        if (bound == null)
            return;
        BanditGaurdTower gbt = new BanditGaurdTower(new Vec2i(bound.X, bound.Y) + TileBase);
        AddObject(gbt, bound.X, bound.Y);
        
    }


    private Vec2i GetRandomPointNearWall(int stepIn=3)
    {

        //Choose a random section of wall
        int wallIndex = GenRan.RandomInt(0, BoundaryPoints.Length);
        int wallP1 = (wallIndex + 1) % BoundaryPoints.Length;

        Vec2i halfDif = BoundaryPoints[wallP1] - BoundaryPoints[wallIndex];
        Vec2i pos = BoundaryPoints[wallIndex] + halfDif;
        //Skip point if its too close to the entrance
        if (pos.QuickDistance(EntrancePosition) < 6 * 6)
            return GetRandomPointNearWall();
        Vec2i mid = TileSize / 2;
        float dx = mid.x - pos.x;
        float dz = mid.z - pos.z;
        float mag = Mathf.Sqrt(dx * dx + dz * dz);
        dx /= mag;
        dz /= mag;
        int nX = pos.x + (int)(dx * stepIn);
        int nZ = pos.z + (int)(dz * stepIn);
        return new Vec2i(nX, nZ);
        return pos;


    }


    /// <summary>
    /// Attmpts to find a plot with the specified 
    /// </summary>
    /// <param name="size"></param>
    /// <param name="attempts"></param>
    /// <returns></returns>
    private Recti FindObjectPlot(Vec2i size, int attempts=20)
    {

        Vec2i pos = RandomPointWithinBounds();

        for(int i=0; i<attempts; i++)
        {

            if(Vec2i.QuickDistance(EntrancePosition, pos) < 5 * 5)
            {
                pos = RandomPointWithinBounds();
                continue;
            }
                

            if(InPolygon(pos + new Vec2i(size.x,0)) && InPolygon(pos + new Vec2i(0, size.z)) && InPolygon(pos + size))
            {
                return new Recti(pos.x, pos.z, size.x, size.z);
            }
            pos = RandomPointWithinBounds();
        }
        return null;
        

    }

    /// <summary>
    /// Randomly generates a wall around the camp
    /// </summary>
    private void GenerateWall()
    {

        Vec2i tilebase = Shell.Position * World.ChunkSize;
        Vec2i mid = TileSize / 2;
        int pointCount = GenRan.RandomInt(6, 10);
        BoundaryPoints = new Vec2i[pointCount];

        //The change in angle between each point
        float dTheta = 2*Mathf.PI / pointCount;


        for(int i=0; i<pointCount; i++)
        {
            float theta = i * dTheta;
            float sinT = Mathf.Sin(theta);
            float cosT = Mathf.Cos(theta);
            
            float rMax_wClamp = TileSize.x / (2 * Mathf.Abs(sinT));
            float rMax_hClamp = TileSize.z / (2 * Mathf.Abs(cosT));
            int maxR = Mathf.FloorToInt(Mathf.Min(rMax_wClamp, rMax_hClamp));

            int R = GenRan.RandomInt(maxR/2, maxR);

            int wallX = Mathf.Clamp((int)(mid.x + sinT * R), 0, TileSize.x-1);
            int wallZ = Mathf.Clamp((int)(mid.z + cosT * R), 0, TileSize.z-1);
            Objects[wallX, wallZ] = new WoodSpikeWall(tilebase + new Vec2i(wallX, wallZ));

            BoundaryPoints[i] = new Vec2i(wallX, wallZ);

        }

        int wallEntranceIndex = 0;

        for (int i = 0; i < pointCount; i++)
        {
            int ip1 = (i + 1) % pointCount;
            GenerationUtil.ConnectPointsWithObject(Objects, BoundaryPoints[i], BoundaryPoints[ip1], new WoodSpikeWall(tilebase), tilebase);
            //GenerationUtil.FillTriangleWithTile(Tiles, mid, points[i], points[ip1], Tile.DIRT.ID);
                
            if(BoundaryPoints[i].x == BoundaryPoints[ip1].x || BoundaryPoints[i].z == BoundaryPoints[ip1].z)
            {
                wallEntranceIndex = i;
            }
        }

        GenerationUtil.FillBoundedShapeWithTile(Tiles, Objects, mid.x, mid.z, Tile.DIRT.ID, true);
        int wallEntrp1 = (wallEntranceIndex + 1) % pointCount;

        Vec2i entrOff = (BoundaryPoints[wallEntrp1] - BoundaryPoints[wallEntranceIndex]) / 2;
        EntrancePosition = BoundaryPoints[wallEntranceIndex] + entrOff;

        for (int x = -2; x <= 2; x++)
        {
            for (int z = -2; z <= 2; z++)
            {
                if (InBounds(EntrancePosition.x + x, EntrancePosition.z + z))
                {
                    Objects[EntrancePosition.x + x, EntrancePosition.z + z] = null;
                }
            }
        }

    }

    public bool InBounds(int x, int z)
    {
        return x >= 0 && z >= 0 && x < TileSize.x && z < TileSize.z;
    }
    public bool InBounds(Vec2i v)
    {
        return v.x >= 0 && v.z >= 0 && v.x < TileSize.x && v.z < TileSize.z;
    }
    public bool InPolygon(int x, int z)
    {
        return IsPointInPolygon(BoundaryPoints, new Vec2i(x, z));
    }
    public bool InPolygon(Vec2i v)
    {
        return IsPointInPolygon(BoundaryPoints, v);
    }

    private Vec2i RandomPointWithinBounds()
    {
        Vec2i pos = new Vec2i(-1, -1);
        while(!IsPointInPolygon(BoundaryPoints, pos)){
            pos = GenRan.RandomVec2i(0, TileSize.x, 0, TileSize.z);
        }
        return pos;
    }

    private List<ChunkData> ToChunkData()
    {
        List<ChunkData> data = new List<ChunkData>(Shell.Size.x * Shell.Size.z);
        for(int cx=0; cx<Shell.Size.x; cx++)
        {
            for (int cz = 0; cz < Shell.Size.z; cz++)
            {
                int[,] chunkTiles = new int[World.ChunkSize, World.ChunkSize];
                Dictionary<int, WorldObjectData> chunkObjs = new Dictionary<int, WorldObjectData>();

                for(int x=0; x<World.ChunkSize; x++)
                {
                    for (int z = 0; z < World.ChunkSize; z++)
                    {
                        int tx = cx * World.ChunkSize + x;
                        int tz = cz * World.ChunkSize + z;
                        chunkTiles[x, z] = Tiles[tx, tz];
                        if(Objects[tx,tz] != null)
                        {
                            chunkObjs.Add(WorldObject.ObjectPositionHash(x, z), Objects[tx, tz]);
                        }
                    }
                }
                ChunkBase cb = GameGenerator.Instance.TerrainGenerator.ChunkBases[Shell.Position.x + cx, Shell.Position.z + cz];
                data.Add(new ChunkData(Shell.Position.x + cx, Shell.Position.z + cz, chunkTiles, true, baseHeight: cb.BaseHeight, objects: chunkObjs));
            }
        }
        return data;

    }



    /// <summary>
    /// Determines if the given point is inside the polygon
    /// </summary>
    /// <param name="polygon">the vertices of polygon</param>
    /// <param name="testPoint">the given point</param>
    /// <returns>true if the point is inside the polygon; otherwise, false</returns>
    public static bool IsPointInPolygon(Vec2i[] polygon, Vec2i testPoint)
    {
        bool result = false;
        int j = polygon.Length - 1;
        for (int i = 0; i < polygon.Length; i++)
        {
            if (polygon[i].z < testPoint.z && polygon[j].z >= testPoint.z || polygon[j].z < testPoint.z && polygon[i].z >= testPoint.z)
            {
                if (polygon[i].x + (testPoint.z - polygon[i].z) / (polygon[j].z - polygon[i].z) * (polygon[j].x - polygon[i].x) < testPoint.x)
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
        if (!(x > 0 && x < TileSize.x && z > 0 && z < TileSize.z))
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
        if (!(x + nObj.Size.x < TileSize.x && z + nObj.Size.z < TileSize.z))
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