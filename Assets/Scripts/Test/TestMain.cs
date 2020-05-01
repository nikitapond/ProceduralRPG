using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
/// <summary>
/// Adding documentation to test how git works
/// </summary>
public class TestMain : MonoBehaviour
{
    public static EventManager EventManager;
    public static EntityManager EntityManager;
    public static GUIManager2 GUIManager;
    public static PlayerManager PlayerManager;
    public static DebugGUI DebugGUI;
    public static WorldManager WorldManager;
    public static bool TEST_MODE = true;



    private LoadedChunk[,] loadedChunks;
    private Dungeon dungeon;
    private Player Player;
    private List<SettlementPathNode> TestNodes;
    private void Awake()
    {
        ResourceManager.LoadAllResources();
        EventManager = new EventManager();
        DebugGUI = GetComponent<DebugGUI>();
        WorldManager = GetComponent<WorldManager>();
        PlayerManager = transform.Find("PlayerManager").GetComponent<PlayerManager>();
        GUIManager = GetComponentInChildren<GUIManager2>();
        EntityManager = GetComponent<EntityManager>();
        GameManager.TestInitiate();
        
    }




    private void Start()
    {

        int test_ = 20;
        int sec = test_ & (16 - 1);
        Debug.Log(sec);
        CreateWorld2();

        CreatePlayer();
        //Spider s = new Spider();
        //s.SetPosition(new Vec2i(5, 5));
        
        //EntityManager.LoadEntity(s);

    }

    private void GenerateWorld3()
    {

        World world = new World();

        //Create chunk bases
        ChunkBase[,] chunk_b = new ChunkBase[World.RegionSize, World.RegionSize];

        ChunkData[,] chunks = new ChunkData[World.RegionSize, World.RegionSize];



        River r = new River();

        

        r.SetFirstChunk(new Vec2i(2, 2), 6);
        r.AddChunk(new Vec2i(3, 2), 6);
        r.AddChunk(new Vec2i(4, 2), 6);
        r.AddChunk(new Vec2i(4, 3), 6);
        r.AddChunk(new Vec2i(5, 3), 6);

        /*
        r.AddChunk(new Vec2i(3,2), 6);
        r.AddChunk(new Vec2i(3, 3), 6);
        r.AddChunk(new Vec2i(4, 3), 6);
        r.AddChunk(new Vec2i(4, 2), 6);
        r.AddChunk(new Vec2i(4, 1), 6);
        r.AddChunk(new Vec2i(3, 1), 6);
        r.AddChunk(new Vec2i(2, 1), 6);
        r.AddChunk(new Vec2i(1, 1), 6);
        */
        /*
        Vec2i last = new Vec2i(2, 2);
        for(int i=0; i<10; i++)
        {
            int t = (i % 8);
            int dx = 0;
            int dz = 0;
            if (t == 0 || t==1)
                dx = 1;
            else if (t == 2 || t == 3)
                dz = 1;
            else if (t == 3 || t == 5)
                dx = -1;
            else
                dz = -1;
            //int dx = (i % 2);
            //int dz = (i + 1) % 2;
            last = last + new Vec2i(dx, dz);
            r.AddChunk(last, 6);
        }*/


        for (int rx=0; rx<World.RegionSize; rx++)
        {
            for (int rz = 0; rz < World.RegionSize; rz++)
            {
                chunk_b[rx,rz] = new ChunkBase(new Vec2i(rx, rz),5, true);
            }
        }

        foreach(KeyValuePair<Vec2i, RiverNode> kvp in r.GenerateRiverNodes())
        {
            chunk_b[kvp.Key.x, kvp.Key.z].AddRiver(kvp.Value);
        }

        int[,] emptyLandChunk = new int[World.ChunkSize, World.ChunkSize];
        for(int x=0; x<World.ChunkSize; x++)
        {
            for(int z=0; z<World.ChunkSize; z++)
            {
                emptyLandChunk[x, z] = Tile.GRASS.ID;
            }
        }
        for (int rx = 0; rx < World.RegionSize; rx++)
        {
            for (int rz = 0; rz < World.RegionSize; rz++)
            {
                chunks[rx, rz] = GenerateChunk((int[,])emptyLandChunk.Clone(), chunk_b[rx, rz]);
            }
        }

        ChunkRegion region = new ChunkRegion(0, 0, chunks);
        WorldManager.SetWorld(world);
        WorldManager.LoadedRegions[0, 0] = region;
        world.SetChunkBases(chunk_b);
        for (int rx = 0; rx < World.RegionSize-1; rx++)
        {
            for (int rz = 0; rz < World.RegionSize-1; rz++)
            {
                WorldManager.CRManager.LoadChunk(new Vec2i(rx, rz));
            }
        }
    }

    private ChunkData GenerateChunk(int[,] landClone, ChunkBase cb)
    {
        int x = cb.Position.x;
        int z = cb.Position.z;
        //If there is a river passing through here
        if (cb.RiverNode != null)
        {
            float sqrt2 = Mathf.Sqrt(2);
            int[,] tiles = new int[World.ChunkSize, World.ChunkSize];
            WorldObjectData[,] data = new WorldObjectData[World.ChunkSize, World.ChunkSize];

            RiverNode rn = cb.RiverNode;
            Vec2i exitDelta = rn.RiverExitDelta;
            Vec2i entrDelta = rn.RiverEntranceDelta;

            if (exitDelta == null)
            {
                exitDelta =new Vec2i(0,0);
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
            if(dx == 0)
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
                       

            float dem_sqr = (a * a + b * b);

            for (int tx = 0; tx < World.ChunkSize; tx++)
            {
                for (int tz = 0; tz < World.ChunkSize; tz++)
                {
                    float dist_sqr = ((a * tz + b * tx + c) * (a * tz + b * tx + c)) / dem_sqr;
                    if (dist_sqr < (cb.RiverNode.EntranceWidth*cb.RiverNode.EntranceWidth)/ divBy)
                    {
                        Vector2 off = new Vector2(x * World.ChunkSize + tx, z * World.ChunkSize + tz);
                        //Debug.Log("here");
                        tiles[tx, tz] = Tile.WATER.ID;

                        if(!(data[tx,tz] is Water))
                        {
                            data[tx, tz] = new Water(new Vec2i(x * World.ChunkSize + tx, z * World.ChunkSize + tz));
                            (data[tx, tz] as Water).SetUVOffset(off);
                        }
                        
                        if(tx < World.ChunkSize - 1 && !(data[tx + 1, tz] is Water))
                        {                            
                            data[tx+1, tz] = new Water(new Vec2i(x * World.ChunkSize + tx+1, z * World.ChunkSize + tz));
                            (data[tx+1, tz] as Water).SetUVOffset(off + new Vector2(1,0));                            
                        }
                        if (tz < World.ChunkSize - 1 && !(data[tx, tz+1] is Water))
                        {
                            data[tx, tz+1] = new Water(new Vec2i(x * World.ChunkSize + tx, z * World.ChunkSize + tz+1));
                            (data[tx,tz+1] as Water).SetUVOffset(off + new Vector2(0, 1));
                        }
                        if (tx < World.ChunkSize - 1 && tz < World.ChunkSize - 1 && !(data[tx+1, tz + 1] is Water))
                        {
                            data[tx+1, tz + 1] = new Water(new Vec2i(x * World.ChunkSize + tx+1, z * World.ChunkSize + tz + 1));
                            (data[tx+1, tz + 1] as Water).SetUVOffset(off + new Vector2(1, 1));
                        }

                        if(tx > 0 && !(data[tx - 1, tz] is Water))
                        {
                            data[tx - 1, tz] = new Water(new Vec2i(x * World.ChunkSize + tx - 1, z * World.ChunkSize + tz));
                            (data[tx - 1, tz] as Water).SetUVOffset(off + new Vector2(-1, 0));
                        }
                        if (tz > 0 && !(data[tx, tz-1] is Water))
                        {                                                      
                            data[tx, tz-1] = new Water(new Vec2i(x * World.ChunkSize + tx, z * World.ChunkSize + tz-1));
                            (data[tx, tz-1] as Water).SetUVOffset(off + new Vector2(0, -1));
                        }
                        if(tx > 0 && tz >0 && !(data[tx-1, tz - 1] is Water))
                        {
                            data[tx-1, tz - 1] = new Water(new Vec2i(x * World.ChunkSize + tx-1, z * World.ChunkSize + tz - 1));
                            (data[tx-1, tz - 1] as Water).SetUVOffset(off + new Vector2(-1, -1));
                        }

                        if(tx > 0 && tz<World.ChunkSize-1 && !(data[tx - 1, tz + 1] is Water))
                        {
                            data[tx - 1, tz + 1] = new Water(new Vec2i(x * World.ChunkSize + tx - 1, z * World.ChunkSize + tz + 1));
                            (data[tx - 1, tz + 1] as Water).SetUVOffset(off + new Vector2(-1, +1));
                        }
                        if (tz > 0 && tx < World.ChunkSize - 1 && !(data[tx + 1, tz - 1] is Water))
                        {
                            data[tx + 1, tz - 1] = new Water(new Vec2i(x * World.ChunkSize + tx + 1, z * World.ChunkSize + tz - 1));
                            (data[tx + 1, tz - 1] as Water).SetUVOffset(off + new Vector2(1, -1));
                        }

                    }else if (dist_sqr < (cb.RiverNode.EntranceWidth * cb.RiverNode.EntranceWidth)*1.4f / divBy)
                    {
                        tiles[tx, tz] = Tile.SAND.ID;
                    }
                    else
                    {
                        tiles[tx, tz] = Tile.GRASS.ID;
                    }
                }
            }

            data[0, 0] = new Tree(new Vec2i(x * World.ChunkSize, z * World.ChunkSize));


            Dictionary<int, WorldObjectData> data_ = new Dictionary<int, WorldObjectData>();

            for (int i = 0; i < World.ChunkSize; i++)
            {
                for (int j = 0; j < World.ChunkSize; j++)
                {
                    if (data[i, j] != null)
                        data_.Add(WorldObject.ObjectPositionHash(i, j), data[i, j]);
                }
            }

            return new ChunkData(x, z, tiles, cb.IsLand, objects:data_);

            //Debug.Log("river");
        }
        else
        {
            ChunkData cd = new ChunkData(x, z, landClone, cb.IsLand);
            return cd;
        }
    }


    private void CreateWorld()
    {
        World world = new World();
        //DungeonGenerator dugeonGenerator = new DungeonGenerator();
        //dungeon = dugeonGenerator.GenerateDungeon();
        ChunkRegion r = new ChunkRegion(0, 0, dungeon.SubworldChunks);
        
        WorldManager.SetWorld(world);
        WorldManager.LoadedRegions[0, 0] = r;
    }
    private void CreateWorld2()
    {
        World world = new World();
        SettlementBase b = new SettlementBase(new Vec2i(8, 8), 8, SettlementType.CAPITAL);
        SettlementBuilder build = new SettlementBuilder(null, b);
        build.GenerateSettlement();
        /*foreach(SettlementPathNode p in build.nodes)
        {
            TestNodes.Add(p);
        }*/
        Tile[,] tiles = build.Tiles;
        WorldObjectData[,] objs = build.SettlementObjects;
        int tileSize = build.TileSize;
        int cSize = tileSize / World.ChunkSize;
        ChunkData[,] cData = new ChunkData[cSize, cSize];
        ChunkBase[,] cBase = new ChunkBase[cSize, cSize];
        for (int x = 0; x < cSize; x++)
        {
            for (int z = 0; z < cSize; z++)
            {
                cBase[x, z] = new ChunkBase(new Vec2i(x, z), 5, true);
                int[,] cTiles = new int[World.ChunkSize, World.ChunkSize];
                WorldObjectData[,] cObj = new WorldObjectData[World.ChunkSize, World.ChunkSize];
                Dictionary<int, WorldObjectData> wObjData = new Dictionary<int, WorldObjectData>();
                for (int x_ = 0; x_ < World.ChunkSize; x_++)
                {
                    for (int z_ = 0; z_ < World.ChunkSize; z_++)
                    {
                        if (tiles[x * World.ChunkSize + x_, z * World.ChunkSize + z_] != null)
                        {
                            cTiles[x_, z_] = tiles[x * World.ChunkSize + x_, z * World.ChunkSize + z_].ID;
                        }
                        else
                        {
                            cTiles[x_, z_] = Tile.GRASS.ID;
                        }
                        
                        if(objs[x * World.ChunkSize + x_, z * World.ChunkSize + z_] != null)
                        {
                            wObjData.Add(WorldObject.ObjectPositionHash(x_, z_), objs[x * World.ChunkSize + x_, z * World.ChunkSize + z_]);
                        }
                       // cObj[x_, z_] = objs[x * World.ChunkSize + x_, z * World.ChunkSize + z_];

                    }
                }
                cData[x, z] = new ChunkData(x, z, cTiles, true, objects:wObjData);
            }
        }


        ChunkRegion r = new ChunkRegion(0, 0, cData);
        WorldManager.LoadedRegions[0,0] = r;



        Kingdom k = new Kingdom("test", new Vec2i(0, 0));
        k.SetKingdomID(world.AddKingdom(k));        
        Settlement set = new Settlement(k, "test_set", build);
        set.SetSettlementID(world.AddSettlement(set));

        WorldManager.SetWorld(world);

        KingdomNPCGenerator npcGen = new KingdomNPCGenerator(new GameGenerator(0), k, EntityManager);
        Debug.Log("BASE:" + set.BaseCoord);
        Debug.Log(cSize);
        npcGen.GenerateSettlementNPC(set);

        world.SetChunkBases(cBase);





        
        

        for (int x = 0; x < cSize-1; x++)
        {
            for (int z = 0; z < cSize-1; z++)
            {
                //LoadChunk(cData[x, z]);
                WorldManager.CRManager.LoadChunk(new Vec2i(x, z));
                EntityManager.LoadChunk(null, new Vec2i(x, z));
            }
        }

                /*
                DungeonGenerator dugeonGenerator = new DungeonGenerator();
                dungeon = dugeonGenerator.GenerateDungeon();
                ChunkRegion r = new ChunkRegion(0, 0, dungeon.SubworldChunks);
                world.LoadedRegions.Add(new Vec2i(0, 0), r);
                WorldManager.SetWorld(world);*/
                //world.Set
    }

    private void CreateWorld3()
    {

    }
    private void CreatePlayer()
    {
        Player = new Player();
        Player.SetPosition(new Vec2i(22, 22));
        Player.Inventory.AddItem(new SteelLongSword());
        PlayerManager.SetPlayer(Player);
    }

    private void LoadChunk(ChunkData c)
    {
        GameObject chunkObject = new GameObject(); //We create a new empty gameobject for the chunk
        chunkObject.transform.parent = transform;
        chunkObject.name = "Chunk " + c;

        LoadedChunk loadedChunk = chunkObject.AddComponent<LoadedChunk>();
        loadedChunk.SetChunkData(c, new ChunkData[] { null, null, null });
    }

    public void ChooseChunks()
    {


    }

    private void OnDrawGizmos()
    {

        if (TestNodes == null)
            return;
        foreach (SettlementPathNode node in TestNodes)
        {
            if (node == null)
                continue;
            Gizmos.DrawSphere(new Vector3(node.Position.x, 0, node.Position.z), 0.3f);
            for (int i = 0; i < 4; i++)
            {
                if (node.Connected[i] != null)
                {
                    Gizmos.DrawLine(new Vector3(node.Position.x, 0, node.Position.z), new Vector3(node.Connected[i].Position.x, 0, node.Connected[i].Position.z));
                }
            }
        }
    }
}