using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
[System.Serializable]
public enum SettlementType
{
    CAPITAL, CITY, TOWN, VILLAGE
}
public class SettlementBuilder : BuilderBase
{
    public static int NODE_RES = 16;
    public static bool TEST = true;

    private GenerationRandom GenerationRandom;

    private GameGenerator GameGenerator;
    //Defines the minimum xz coordinate of this settlement, such that world positions are equal to BaseCoord+localPosition
    //public Vec2i BaseTile { get; private set; }
    //Defines the middle tile of the Settelment in local coordinates
    private Vec2i MidTile { get; }
    //The chunk coordinate at the centre of the settlement, defines its position in the world
    public Vec2i Centre { get; private set; }

    //An array of all the chunks that this settelment contains/belongs to.
    public Vec2i[] SettlementChunks { get; private set; }

    //Array that holds all WorkObjects that will exist in this settlement
    public WorldObjectData[,] SettlementObjects { get; set; }
    //Array that hold all Tiles for this settlement
    //public Tile[,] Tiles { get; set; }

    //public float[,] Heights { get; set; }

    //public Voxel[] Voxels;

    public int SettlementWallBoundry = 5;

    //Defines the size of the settlement from one edge to another, in units of tiles.
    public int TileSize { get; private set; }
    public SettlementType SettlementType { get; private set; }
    public List<Building> Buildings { get; private set; }
    //A list that defines the mid point of the settelment entrances, up to 4 (one on each face) can be defined.
    public Vec2i Entrance { get; private set; }
    //A list of all points that are parts of a path. Contains duplicates which are removed when the final settlement is built
    //A list of the possible plots to build on.
    public List<Recti> BuildingPlots { get; }
    //Used to generate paths inside this settlement

    public SettlementPathFinder SettlementPathFinder;
    public Tile PathTile { get; private set; }

    //public float AverageHeight = 5;

    public SettlementBuilder(HeightFunction heightFunc, SettlementBase set) : base(set.BaseChunk, new Vec2i(set.ChunkSize, set.ChunkSize), heightFunc)
    {
       
        GenerationRandom = new GenerationRandom(0);
        Centre = set.Centre;
        SettlementChunks = set.SettlementChunks;
        TileSize = set.TileSize;
        PathTile = Tile.STONE_PATH;
        MidTile = new Vec2i(TileSize / 2, TileSize / 2);
        //Tiles = new Tile[TileSize, TileSize];
        SettlementObjects = new WorldObjectData[TileSize, TileSize];
        Buildings = new List<Building>();
        //PathNodes = new List<Vec2i>();
        BuildingPlots = new List<Recti>();
        SettlementType = set.SettlementType;

        // Heights = new float[TileSize, TileSize];


        //TestNodes = new List<SettlementPathNode>();

        //Defines a path node to be once every chunk
        PathNodeRes = World.ChunkSize;
        PathNodes = new float[TileSize / NODE_RES, TileSize / NODE_RES];
        PNSize = TileSize / PathNodeRes;

        //AverageHeight = gameGen.TerrainGenerator.ChunkBases[Centre.x, Centre.z].BaseHeight;


        // Voxels = new Voxel[(TileSize) * TileSize * World.ChunkHeight];
    }

    public SettlementBuilder(GameGenerator gameGen, SettlementBase set) : base (set.BaseChunk, new Vec2i(set.ChunkSize, set.ChunkSize), gameGen)
    {
        GameGenerator = gameGen;
        if(gameGen != null)
            GenerationRandom = new GenerationRandom(gameGen.Seed);
        else
            GenerationRandom = new GenerationRandom(0);
        Centre = set.Centre;
        SettlementChunks = set.SettlementChunks;
        TileSize = set.TileSize;
        PathTile = Tile.STONE_PATH;
        MidTile = new Vec2i(TileSize / 2, TileSize / 2);
        //Tiles = new Tile[TileSize, TileSize];
        SettlementObjects = new WorldObjectData[TileSize, TileSize];
        Buildings = new List<Building>();
        //PathNodes = new List<Vec2i>();
        BuildingPlots = new List<Recti>();
        SettlementType = set.SettlementType;

       // Heights = new float[TileSize, TileSize];
        

        //TestNodes = new List<SettlementPathNode>();

        //Defines a path node to be once every chunk
        PathNodeRes = World.ChunkSize; 
        PathNodes = new float[TileSize / NODE_RES, TileSize / NODE_RES];
        PNSize = TileSize / PathNodeRes;

        FlattenBase(5);
        //AverageHeight = gameGen.TerrainGenerator.ChunkBases[Centre.x, Centre.z].BaseHeight;


       // Voxels = new Voxel[(TileSize) * TileSize * World.ChunkHeight];
    }


    public override void Generate(GenerationRandom genRan)
    {
        GenerationRandom = genRan;
        //ChooseRandomEntrancePoints();
        AddInitPaths();
        List<BuildingPlan> mustAdd = new List<BuildingPlan>();
        /*
        for(int x=10; x<20; x++)
        {
            for(int z=10; z<20; z++)
            {
                for(int y=40; y<128; y++)
                {
                    SetVoxelNode(x, y, z, new VoxelNode(Voxel.stone));
                }
            }
        }*/

       

        BuildingPlan defaultRemaining = Building.HOUSE;
        switch (SettlementType)
        {
            case SettlementType.CAPITAL:

                //AddMainBuilding(BuildingGenerator.GenerateCastle(48));
                mustAdd.Add(Building.BARACKS);
                mustAdd.Add(Building.MARKET);
                mustAdd.Add(Building.TAVERN);
                mustAdd.Add(Building.TAVERN);
                mustAdd.Add(Building.TAVERN);
                mustAdd.Add(Building.BLACKSMITH);
                mustAdd.Add(Building.BLACKSMITH);

                /*
                mustAdd.Add(Building.MARKET);
                mustAdd.Add(Building.BARACKSCITY);
                mustAdd.Add(Building.TAVERN);
                mustAdd.Add(Building.BLACKSMITH);
                mustAdd.Add(Building.ALCHEMISTS);
                mustAdd.Add(Building.TAVERN);
                mustAdd.Add(Building.BLACKSMITH);
                mustAdd.Add(Building.TAVERN);
                mustAdd.Add(Building.GENERALMERCHANT);
                mustAdd.Add(Building.ARCHERYSTORE);
                mustAdd.Add(Building.SWORDSELLER);
                */
                for (int i=0; i<10; i++)
                {
                    mustAdd.Add(Building.HOUSE);
                }

                defaultRemaining = Building.HOUSE;
                break;
            case SettlementType.CITY:
               // AddMainBuilding(BuildingGenerator.GenerateCastle(32));
                //mustAdd.Add(Building.BARACKSCITY);
                mustAdd.Add(Building.BLACKSMITH);
                /*
                mustAdd.Add(Building.GENERALMERCHANT);
                mustAdd.Add(Building.TAVERN);
                mustAdd.Add(Building.MARKET);
                */
                for (int i = 0; i < 15; i++)
                {
                    mustAdd.Add(Building.HOUSE);
                }
                break;
            case SettlementType.TOWN:
                //AddMainBuilding(BuildingGenerator.CreateBuilding(Building.HOLD));
                //mustAdd.Add(Building.BARACKSTOWN);

                mustAdd.Add(Building.BLACKSMITH);
                /*mustAdd.Add(Building.GENERALMERCHANT);
                mustAdd.Add(Building.TAVERN);
                mustAdd.Add(Building.MARKET);*/
                for (int i = 0; i < 10; i++)
                {
                    mustAdd.Add(Building.HOUSE);
                }
                break;

            case SettlementType.VILLAGE:
               // AddMainBuilding(BuildingGenerator.CreateBuilding(Building.VILLAGEHALL));
                mustAdd.Add(Building.BLACKSMITH);
                /*
                mustAdd.Add(Building.GENERALMERCHANT);
                mustAdd.Add(Building.TAVERN);
                mustAdd.Add(Building.MARKET);*/
                for (int i = 0; i < 6; i++)
                {
                    mustAdd.Add(Building.HOUSE);
                }
                break;
        }

        PlaceBuildings(mustAdd);
        CreatePathNodes();
        /*
        for (int x = 0; x < TileSize; x++)
        {
            for (int z = 0; z < TileSize; z++)
            {
                if (x < TileSize / 2 && z < TileSize / 2)
                {
                    SetTile(x, z, Tile.TEST_RED);
                }
                else if (x < TileSize / 2)
                {
                    SetTile(x, z, Tile.TEST_BLUE);
                }
                else
                {
                    SetTile(x, z, Tile.TEST_GREEN);
                }
                //SetVoxelNode(x, 0, z, new VoxelNode(Voxel.grass));
            }
        }*/

    }
    public SettlementPathNode ENTR_NODE;
    /// <summary>
    /// PathNodes - 2D float array representing the passable map for entities.
    /// Used in path finding within settlements to increase performance, as well as
    /// to create the paths within the settlement
    /// PathNodeRes - the distance between each node on the PathNode Grid
    /// PNSize - int, the size of each dimension of PathNodes. PNSize = TileSize/PathNodeRes;
    /// </summary>
    public float[,] PathNodes;
    public int PathNodeRes;
    public int PNSize;
    public Vec2i EntranceNode;

    private void AddInitPaths()
    {

        int nodeSize = TileSize / NODE_RES;

        int xStart = GenerationRandom.RandomInt(0+3, nodeSize - 3);
        int zLen = nodeSize - GenerationRandom.RandomInt(0, 3); //Ends 0-3 chunks from settlement end




        int zStartWest = GenerationRandom.RandomInt(2, nodeSize - 3);
        int xLenWest = GenerationRandom.RandomInt(xStart-2, xStart);

        EntranceNode = new Vec2i(xStart, 0);
        //Add the path nodes along the z direction (start at south -> north)
        for (int z=0; z<zLen; z++)
        {
            PathNodes[xStart, z] = 100;


            SetTile(xStart * NODE_RES, z * NODE_RES, Tile.TEST_BLUE);
        }


        
        int zStartEast = GenerationRandom.RandomInt(2, nodeSize - 3);
        int xLenEast = GenerationRandom.RandomInt(nodeSize - xStart - 2, nodeSize - xStart);


        for (int x=0; x<xLenEast; x++)
        {
            PathNodes[xStart + x, zStartEast] = 100;

        }
        

        for(int x=0; x<xLenWest; x++)
        {
            PathNodes[xStart - x, zStartWest] = 100;

        }

        for (int x = 0; x < nodeSize; x++)
        {
            for (int z = 0; z < nodeSize; z++)
            {
               
                if(PathNodes[x,z] != 0)
                {
                    if(x > 0 && PathNodes[x-1,z] != 0)
                    {
                        SetTiles((x - 1) * NODE_RES, z * NODE_RES - 2, (x) * NODE_RES, z * NODE_RES + 2, Tile.TEST_BLUE);
                    }
                    if (z > 0 && PathNodes[x, z-1] != 0)
                    {
                        SetTiles((x) * NODE_RES-2, (z-1) * NODE_RES, (x) * NODE_RES+2, z * NODE_RES, Tile.TEST_BLUE);
                    }
                    if (x < nodeSize-1 && PathNodes[x + 1, z] != 0)
                    {
                        SetTiles((x) * NODE_RES, z * NODE_RES - 2, (x+1) * NODE_RES, z * NODE_RES + 2, Tile.TEST_BLUE);
                    }
                    if (z < nodeSize-1 && PathNodes[x, z + 1] != 0)
                    {
                        SetTiles((x) * NODE_RES - 2, (z) * NODE_RES, (x) * NODE_RES + 2, (z+1) * NODE_RES, Tile.TEST_BLUE);
                    }
                }
            }
        }

    }

    private void SetTiles(int minX, int minZ, int maxX, int maxZ, Tile tile)
    {
        if (!InBounds(new Vec2i(minX, minZ)) || !InBounds(new Vec2i(maxX, maxZ)))
            return;
        for(int x=minX; x<maxX; x++)
        {
            for (int z = minZ; z < maxZ; z++)
            {
                SetTile(x, z, tile);
                if(tile == Tile.TEST_BLUE)
                {
                    
                    AddVoxel(x, 0, z, Voxel.dirt_path);
                }
            }
        }
    }

    private void CreatePathNodes()
    {
        
        int nodeSize = TileSize / NODE_RES;

        int tSize = TileSize/ NODE_RES;
        int checkDist = 4;
        //Create a node at every node position not inside a building
        for(int x=0; x<tSize; x++)
        {
            for (int z = 0; z < tSize; z++)
            {

                if (PathNodes[x, z] != 0)
                    continue;
                //Define the position of this node in the settlement
                
                Vec2i pos = new Vec2i(x * NODE_RES, z * NODE_RES);
                if(IsTileFree(pos.x, pos.z))
                {
                    PathNodes[x, z] = 50;
                    //TestNodes2[x, z] = new SettlementPathNode(new Vec2i(x * NODE_RES, z * NODE_RES));

                }
                else
                {
                    PathNodes[x, z] = -1;
                }

            }
        }

        for (int x = 0; x < tSize; x++)
        {
            for (int z = 0; z < tSize; z++)
            {
                if (PathNodes[x, z] <= 0)
                    continue;
                if (PathNodes[x, z] == 100)
                    continue;
                if (x > 0 && PathNodes[x - 1, z] != 0)
                {
                    //If nodes [x,z] and [x-1,z] are both non 0, check if a path exists between them
                    //If there is no path, we try to remove one of the nodes
                    if (!IsAreaFree((x - 1) * PathNodeRes, z * PathNodeRes - 1, PathNodeRes, 3, Tile.TEST_BLUE))
                    {
                        PathNodes[x, z] = -1;
                   
                    }
                }
                if (z > 0 && PathNodes[x, z - 1] != 0)
                {
                    //If nodes [x,z] and [x,z-1] are both non 0, check if a path exists between them
                    //If there is no path, we try to remove one of the nodes
                    if (!IsAreaFree((x) * PathNodeRes - 1, (z - 1) * PathNodeRes, 3, PathNodeRes, Tile.TEST_BLUE))
                    {
                        PathNodes[x, z] = -1;
                        continue;

                    }
                }
                if (x < tSize - 1 && PathNodes[x + 1, z] != 0)
                {
                    if (!IsAreaFree(x * PathNodeRes, z * PathNodeRes - 1, PathNodeRes, 3, Tile.TEST_BLUE))
                    {
                        PathNodes[x, z] = -1;
                        continue;
      
                    }
                }
                if (z < tSize - 1 && PathNodes[x, z + 1] != 0)
                {
                    if (!IsAreaFree(x * PathNodeRes - 1, z * PathNodeRes, 3, PathNodeRes, Tile.TEST_BLUE))
                    {
                        PathNodes[x, z] = -1;
                        continue;

                    }

                }
            }
        }
        
        float[] nodes_ = new float[4];
        //Remove circular path nodes
        for (int x = 0; x < tSize - 1; x++)
        {
            for (int z = 0; z < tSize - 1; z++)
            {
                nodes_[0] = PathNodes[x, z];
                nodes_[1] = PathNodes[x + 1, z];
                nodes_[2] = PathNodes[x + 1, z + 1];
                nodes_[3] = PathNodes[x, z + 1];
                //If all nodes have a path on them
                if (nodes_[0] > 0 && nodes_[1] > 0 && nodes_[2] > 0 && nodes_[3] > 0)
                {
                    int destroy = GenerationRandom.RandomInt(0, 4);
                    while (nodes_[destroy] == 100)
                        destroy = GenerationRandom.RandomInt(0, 4);
                    switch (destroy)
                    {
                        case 0:
                            PathNodes[x, z] = 0;
                            break;
                        case 1:
                            PathNodes[x + 1, z] = 0;
                            break;
                        case 2:
                            PathNodes[x + 1, z + 1] = 0;
                            break;
                        case 3:
                            PathNodes[x, z + 1] = 0;
                            break;
                    }
                }
            }
        }



        //Create a settlement path finder, we will use this to find and remove islands
        SettlementPathFinder = new SettlementPathFinder(BaseTile, PathNodes);
        
        List<Vec2i> testing;
        List<Vec2i> tested = new List<Vec2i>(PNSize * PNSize / 4);

        //Iterate all nodes
        for (int x = 0; x < tSize; x++)
        {
            for (int z = 0; z < tSize; z++)
            {
                //If node is 0, we ignore it
                if (PathNodes[x, z] <= 0)
                    continue;
                int conCount = ConnectedCount(x, z);
                /*
                if (conCount == 0)
                {
                    PathNodes[x, z] = -1;
                    SettlementPathFinder.PathNodes[x, z] = -1;
                }*/
                Vec2i pnPos = new Vec2i(x,z);
                //If we have already tested this point
                
                
                    
                //If a node has only 1 connection, it is a good candidate
                //for checking for islands
                //if (conCount == 1)
                if(true)
                {
                    float cost;
                    //We check to see if a path can be found to the entrance from this node
                    if(SettlementPathFinder.ConnectNodes(pnPos, EntranceNode, out testing, out cost, false))
                    {
                        
                        foreach(Vec2i v in testing)
                        {
                            if(PathNodes[v.x,v.z] <= 0)
                            {
                                PathNodes[v.x, v.z] = 50;
                                SettlementPathFinder.PathNodes[v.x, v.z] = 50;
                            }
                        }
                        //If a valid path is found, we add the entire path to the list of tested paths, to reduce double checking.
                        
                       // Debug.Log("Path found with cost " + cost);
                        if (cost < int.MaxValue)
                        {
                            tested.AddRange(testing);
                            foreach (Vec2i v in testing)
                            {
                                PathNodes[v.x, v.z] = 50;
                                SettlementPathFinder.PathNodes[v.x, v.z] = 50;
                            }
                        }
                    }
                    else
                    {
                        //Debug.Log("Node " + x + "," + z + " is island. Path cost = " + cost);
                        bool canMake = true;
                        //If no path is found, we iterate each tested point and remove the path
                        foreach(Vec2i v in testing)
                        {
                            if(PathNodes[v.x, v.z] == -1)
                            {
                                canMake = false;
                                break;
                            }
                           // PathNodes[v.x, v.z] = 0;
                            //SettlementPathFinder.PathNodes[v.x, v.z] = 0;
                        }
                        if (canMake)
                        {
                            foreach (Vec2i v in testing)
                            {
                                if (PathNodes[v.x, v.z] != 100)
                                {
                                    PathNodes[v.x, v.z] = 50;
                                    SettlementPathFinder.PathNodes[v.x, v.z] = 50;
                                }
                                
                            }
                        }
                        else
                        {
                            //TODO - do we need to delete all points if this happens?
                        }
                    }
                }
            }
        }
        
        //Connect all nodes
        for (int x = 0; x < tSize; x++)
        {
            for (int z = 0; z < tSize; z++)
            {
                if (PathNodes[x, z] <= 0)
                    continue;
                bool is100 = PathNodes[x, z] == 100;
                if (x > 0 && PathNodes[x - 1, z] > 0)
                {
                    if (!(PathNodes[x - 1, z] == 100 && is100))   
                        SetTiles((x - 1) * NODE_RES, z * NODE_RES - 1, (x) * NODE_RES, z * NODE_RES + 1, Tile.TEST_BLUE);
                }
                if (z > 0 && PathNodes[x, z - 1] > 0)
                {
                    if (!(PathNodes[x, z-1] == 100 && is100))
                        SetTiles((x) * NODE_RES - 1, (z - 1) * NODE_RES, (x) * NODE_RES + 1, z * NODE_RES, Tile.TEST_BLUE);
                }
                if (x < nodeSize - 1 && PathNodes[x + 1, z] > 0)
                {
                    if (!(PathNodes[x + 1, z] == 100 && is100))
                        SetTiles((x) * NODE_RES, z * NODE_RES - 1, (x + 1) * NODE_RES, z * NODE_RES + 1, Tile.TEST_BLUE);
                }
                if (z < nodeSize - 1 && PathNodes[x, z + 1] > 0)
                {
                    if (!(PathNodes[x, z+1] == 100 && is100))
                        SetTiles((x) * NODE_RES - 1, (z) * NODE_RES, (x) * NODE_RES + 1, (z + 1) * NODE_RES, Tile.TEST_BLUE);
                }
            }
        }

        return;
      

    }
    /// <summary>
    /// Finds the number of path nodes 'connected' to the node (x,z).
    /// Does this by checking each of the relative directions from the node
    /// (excluding diagonals), and adds 1 to the count if the value is not 0
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    private int ConnectedCount(int x, int z)
    {
        int count = 0;
        if (x > 0 && PathNodes[x - 1, z] > 0)
            count++;
        if (z > 0 && PathNodes[x , z- 1] > 0)
            count++;
        if (x < PNSize-1 && PathNodes[x + 1, z] > 0)
            count++;
        if (z < PNSize - 1 && PathNodes[x , z + 1] > 0)
            count++;
        return count;
    }




    private void PlaceBuildings(List<BuildingPlan> buildings)
    {


        foreach (BuildingPlan bp in buildings)
        {

            //try
            //{
                Building b = BuildingGenerator.CreateBuilding(GenerationRandom, out BuildingVoxels vox, bp);
               // Debug.Log("Added building " + b);
                Recti r = null;
                int i = 0;
                while (r == null && i < 5)
                {
                    r = AddBuilding(b, vox);
                    i++;
                }
                if (r == null)
                    continue;
                this.Buildings.Add(b);
                //SurroundByPath(r.X, r.Y, r.Width, r.Height, 2);
                SettlementPathNode[] nodes = AddPlot(r);
          /*  }catch(System.Exception e)
            {
                Debug.Log(e);
            }*/
            


        }

    }





    private SettlementPathNode[] AddPlot(Recti r)
    {
        SettlementPathNode[] nodes = new SettlementPathNode[4];
        if (r.X > 0 && r.Y > 0)
        {
            Vec2i n1 = new Vec2i(r.X - 1, r.Y - 1);
            nodes[0] = new SettlementPathNode(n1); //Bottom left
            //PathNodes.Add(new Vec2i(r.X - 1, r.Y - 1));
        }
        if (r.X > 0 && r.X + r.Width + 1 < TileSize && r.Y > 0)
        {
            //PathNodes.Add(new Vec2i(r.X + r.Width + 1, r.Y - 1));
            nodes[1] = new SettlementPathNode(new Vec2i(r.X + r.Width + 1, r.Y - 1)); //Bottom right
        }
        if (r.X > 0 && r.Y > 0 && r.Y + r.Height + 1 < TileSize)
        {
            //PathNodes.Add(new Vec2i(r.X - 1, r.Y + r.Height + 1));
            nodes[2] = new SettlementPathNode(new Vec2i(r.X - 1, r.Y + r.Height + 1)); //Top Left

        }
        if (r.X > 0 && r.X + r.Width + 1 < TileSize && r.Y > 0 && r.Y + r.Height + 1 < TileSize)
        {
            //PathNodes.Add(new Vec2i(r.X + r.Width + 1, r.Y + r.Height + 1));
            nodes[3] = new SettlementPathNode(new Vec2i(r.X + r.Width + 1, r.Y + r.Height + 1)); //Top Right
        }
        if (nodes[0] != null)
        {
            if (nodes[1] != null)
            {
                nodes[0].AddConnection(SettlementPathNode.EAST, nodes[1]);
                nodes[1].AddConnection(SettlementPathNode.WEST, nodes[0]);
            }
            if (nodes[2] != null)
            {
                nodes[0].AddConnection(SettlementPathNode.NORTH, nodes[2]);
                nodes[2].AddConnection(SettlementPathNode.SOUTH, nodes[0]);
            }
        }
        if (nodes[3] != null)
        {
            if (nodes[1] != null)
            {
                nodes[3].AddConnection(SettlementPathNode.SOUTH, nodes[1]);
                nodes[1].AddConnection(SettlementPathNode.NORTH, nodes[3]);
            }
            if (nodes[2] != null)
            {
                nodes[3].AddConnection(SettlementPathNode.WEST, nodes[2]);
                nodes[2].AddConnection(SettlementPathNode.EAST, nodes[3]);
            }
        }
        //TestNodes.AddRange(nodes);
        BuildingPlots.Add(r);
        return nodes;

    }
    /// <summary>
    /// Surrounds the given region by a path of width 'pathSize'
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="pathSize"></param>
    public void SurroundByPath(int x, int z, int width, int height, int pathSize)
    {
        //iterate pathsize
        for (int i = 0; i < pathSize; i++)
        {
            //Iterate along x axis
            for (int x_ = x - pathSize; x_ < x + width + pathSize; x_++)
            {
                //Add path for each x point at minimum and maximum z values
                SetTile(x_, z - 1 - i, Tile.STONE_FLOOR);
                SetTile(x_, z + height + i, Tile.STONE_FLOOR);
            }
            for (int z_ = z; z_ <= z + height; z_++)
            {
                //Add path for each z point at minimum and maximum x values
                SetTile(x - 1 - i, z_, Tile.STONE_FLOOR);
                SetTile(x + width + i, z_, Tile.STONE_FLOOR);
            }
        }


    }


    public bool PointFree(int x, int z)
    {
        if (x < 0 || x >= TileSize || z < 0 || z >= TileSize)
        {
            return false;
        }
        return GetTile(x, z) == 0;
    }



    public Vec2i GetFreePoint()
    {
        Vec2i toOut = GenerationRandom.RandomVec2i(1, TileSize - 2);
        //Vec2i toOut = new Vec2i(MiscMaths.RandomRange(1, TileSize - 2), MiscMaths.RandomRange(1, TileSize - 2));
        while (true)
        {
            if (GetTile(toOut.x, toOut.z) == 0 && SettlementObjects[toOut.x, toOut.z] == null)
                return toOut;
            toOut = GenerationRandom.RandomVec2i(1, TileSize - 2);

        }


    }


    /// <summary>
    /// Adds the given main building to a point near the centre of the settlement, then surrounds it
    /// by a large path
    /// </summary>
    /// <param name="building"></param>
    public void AddMainBuilding(Building building)
    {
        //Choose the position and place building
        //Vec2i pos = MidTile + new Vec2i(MiscMaths.RandomRange(-20, 20), MiscMaths.RandomRange(-20, 20));

        Recti r = null;

        //while(r==null)
        //    r = AddBuilding(building);
        //Add the plot and collect the path nodes
        SettlementPathNode[] nodes = AddPlot(r);


        return;
        //Find which node is nearest the settlement entrance.
        int nearestNodeToEntrace = 0;
        for (int i = 0; i < 4; i++)
        {
            if (nodes[i] == null)
                continue;
            if (Vec2i.QuickDistance(nodes[i].Position, Entrance) < Vec2i.QuickDistance(nodes[nearestNodeToEntrace].Position, Entrance))
                nearestNodeToEntrace = i;
        }

        ConnectEntranceNode(nodes[nearestNodeToEntrace], 5);
        //ConnectPathNodes(EntranceNode, nodes[nearestNodeToEntrace], 5);
        //CreatePathFromNode(nodes[nearestNodeToEntrace], 4);
        SurroundByPath(r.X, r.Y, r.Width, r.Height, 5);
        //BuildingPlots.Add(r);
    }

    /// <summary>
    /// Connects the given node to the entrance node
    /// </summary>
    /// <param name="startNode"></param>
    /// <param name="width"></param>
    private void ConnectEntranceNode(SettlementPathNode startNode, int width)
    {
        return;
        /*
        SettlementPathNode entranceNode = EntranceNode;
        int entranceDirection = EntranceNodeDirection;
        Vec2i entranceDirectionStep = SettlementPathNode.GetDirection(entranceDirection);
        Vec2i diff = startNode.Position - entranceNode.Position;

        Vec2i midNodePosition = entranceNode.Position + new Vec2i(Mathf.Abs(diff.x) * entranceDirectionStep.x, Mathf.Abs(diff.z) * entranceDirectionStep.z);
        SettlementPathNode midNode = new SettlementPathNode(midNodePosition);
        entranceNode.AddConnection(entranceDirection, midNode);
        midNode.AddConnection(SettlementPathNode.OppositeDirection(entranceDirection), entranceNode);
       // TestNodes.Add(midNode);*/


    }



    public void ConnectPathNodes(SettlementPathNode first, SettlementPathNode second, int width)
    {
        Vec2i diff = second.Position - first.Position; //Find the vector between the two nodes
        //First generate the path in the x direction
        Vec2i xDiff = new Vec2i(diff.x, 0);
        int xDiffDirection = SettlementPathNode.GetDirection(xDiff);
        SettlementPathNode midNode = CreatePathFromNode(first, width, chosenDirection: xDiffDirection, length: Mathf.Abs(diff.x));
        //TestNodes.Add(midNode);
        //Add connections
        first.AddConnection(xDiffDirection, midNode);
        midNode.AddConnection(SettlementPathNode.OppositeDirection(xDiffDirection), first);

        Vec2i zDiff = new Vec2i(0, diff.z);
        ConnectNodes(midNode, second, width);

    }


    public void ConnectNodes(SettlementPathNode first, SettlementPathNode second, int width)
    {
        Vec2i diff = second.Position - first.Position; //Find the vector between the two nodes

        int length = diff.x + diff.z;
        int direction = SettlementPathNode.GetDirection(diff);
        //first.AddConnection(direction, second);
        //second.AddConnection(SettlementPathNode.OppositeDirection(direction), first);
        Vec2i step = SettlementPathNode.GetDirection(direction);
        Vec2i perpDirection = SettlementPathNode.GetPerpendicular(direction);
        int halfWidth = width / 2;

        for (int l = 0; l < length; l++)
        {
            for (int w = -halfWidth; w <= halfWidth; w++)
            {
                Vec2i pos = first.Position + step * l + perpDirection * w;
                SetTile(pos.x, pos.z, Tile.TEST_BLUE);
            }
        }
    }

    public void CreatePathBetweenNodes(SettlementPathNode firstNode, int secondNodeDirection, int width, int length = -1)
    {
        SettlementPathNode second = firstNode.Connected[secondNodeDirection];
        if (second == null)
        {
            Debug.Error("Paths are not connected");
            return;
        }

        Vec2i pathDiff = second.Position - firstNode.Position; //Find the vector that describes first->second
        int pathDiffLen = pathDiff.x + pathDiff.z; //Vector should have only 1 component, so total length can be written as sum
        int startPointOffset = pathDiffLen / 4;
        int startPoint = MiscMaths.RandomRange(startPointOffset, pathDiffLen - startPointOffset);

        //Generate node between two existing nodes
        SettlementPathNode interPathNode = new SettlementPathNode(firstNode.Position + SettlementPathNode.GetDirection(secondNodeDirection) * startPoint);
        int oppositeDir = SettlementPathNode.OppositeDirection(secondNodeDirection);
        //TestNodes.Add(interPathNode);
        //Update connections
        firstNode.AddConnection(secondNodeDirection, interPathNode);
        interPathNode.AddConnection(oppositeDir, firstNode);
        second.AddConnection(oppositeDir, interPathNode);
        interPathNode.AddConnection(secondNodeDirection, second);
        CreatePathFromNode(interPathNode, width, length: length);

    }


    public SettlementPathNode CreatePathFromNode(SettlementPathNode node, int width, bool extraLength = false, int chosenDirection = -1, int length = -1)
    {
        //If no direction is given, choose a null one
        if (chosenDirection == -1)
        {
            List<int> nullDirection = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                if (node.Connected[i] == null)
                    nullDirection.Add(i);
            }
            Debug.Log(nullDirection.Count);
            //Choose a valid direction and find the vector step
            chosenDirection = GenerationRandom.RandomFromList(nullDirection);
        }

        Vec2i step = SettlementPathNode.GetDirection(chosenDirection);
        //If no length is given or given length is invalid, choose a path length
        if (length == -1 || !InBounds(node.Position + step * length))
        {
            int attemptLength = length == -1 ? GenerationRandom.RandomInt(40, TileSize) : length;
            while (!InBounds(node.Position + step * attemptLength))
                attemptLength -= 1;

            length = attemptLength;
        }
        int halfWidth = width / 2;
        Vec2i perpDirection = SettlementPathNode.GetPerpendicular(chosenDirection);
        if (extraLength)
            length += halfWidth;
        for (int l = 0; l < length; l++)
        {
            for (int w = -halfWidth; w <= halfWidth; w++)
            {
                Vec2i pos = node.Position + step * l + perpDirection * w;
                SetTile(pos.x, pos.z, Tile.TEST_BLUE);
            }
        }
        SettlementPathNode endNode = new SettlementPathNode(node.Position + step * length);
        node.AddConnection(chosenDirection, endNode);
        endNode.AddConnection(SettlementPathNode.OppositeDirection(chosenDirection), node);
        return endNode;
    }



    /// <summary>
    /// Sets all tiles on the ground to this tile
    /// </summary>
    /// <param name="tile"></param>
    public void SetBaseTile(Tile tile)
    {
        for (int x = 0; x < TileSize; x++)
        {
            for (int z = 0; z < TileSize; z++)
            {
                if (GetTile(x, z) == 0)
                    SetTile(x, z, tile);
            }
        }
    }

    public bool InBounds(Vec2i v)
    {
        return v.x >= 0 && v.x < TileSize && v.z >= 0 && v.z < TileSize;
    }

    public Vec2i ChoosePlot(int width, int height, int attempts = 20)
    {

        for (int i = 0; i < attempts; i++)
        {
            Vec2i pos = GenerationRandom.RandomVec2i(SettlementWallBoundry, TileSize - 1 - width- SettlementWallBoundry);
            if (IsAreaFree(pos.x-1, pos.z-1, width+2, height+2))
            {
                return pos;
            }
        }
        return null;
    }






    public Recti AddBuilding(Building b, BuildingVoxels vox, Vec2i pos = null, bool force = false)
    {
        if (pos != null)
        {

            if (force == false && !IsAreaFree(pos.x, pos.z, b.Width, b.Height))
                return null;
        }
        else
        {
            pos = ChoosePlot(b.Width, b.Height);
        }
        //We choose a random allowed position.
        //Vec2i pos = ChoosePlot(b.Width, b.Height);
        //If no possible position is found, we return null
        if (pos == null)
            return null;

        if (force)
        {
            if (pos.x + b.Width >= TileSize || pos.z + b.Height >= TileSize)
                return null;
        }

        int height = (int)FlattenArea(pos.x-1, pos.z-1, b.Width+2, b.Height+2);
  
        //Debug.Log("Adding building " + b);
        //SetTiles(pos.x, pos.z, b.Width, b.Height, b.BuildingTiles);
        for (int x = 0; x < b.Width; x++)
        {
            for (int z = 0; z < b.Height; z++)
            {



                SetTile(x + pos.x, z + pos.z, b.BuildingTiles[x, z]);


                //SetHeight(x + pos.x, z + pos.z, maxHeight);
                for(int y=0; y < vox.Height; y++)
                {
                    SetVoxelNode(x + pos.x, height + y, z + pos.z, vox.GetVoxelNode(x, y, z));
                    
                }
            }
        }
        foreach (WorldObjectData obj in b.GetBuildingObjects())
        {

            //We must set the object to have coordinates based on the settlement,
            //such that the object is added to the correct chunk.
            obj.SetPosition(obj.Position + pos.AsVector3());
            //We (should) already have checked for object validity when creating the building
            AddObject(obj, true);
        }

        Vec2i wPos = pos + BaseTile;
        Vec2i cPos = World.GetChunkPosition(wPos);
        //Debug.Log("Calculating tiles!!!");
        b.SetPositions(BaseTile, pos);
        b.CalculateSpawnableTiles(vox);
        Buildings.Add(b);
        //PathNodes.Add(b.Entrance);
        return new Recti(pos.x-1, pos.z-1, b.Width+2, b.Height+2);
    }




    public bool IsTileFree(int x, int z)
    {
        if (x < 0 || z < 0 || x >= TileSize || z >= TileSize)
            return false;

        /* foreach (Recti ri in BuildingPlots)
             if (ri.ContainsPoint(x, z))
                 return false;
                 */
        if (GetTile(x, z) != 0)
            return false;
        return true;
    }
    public bool IsAreaFree(int x, int z, int width, int height, Tile ignoreTile=null)
    {
        if (x < 0 || z < 0)
            return false;
        if(GameGenerator != null)
        {
            Vec2i baseChunk = BaseChunk + new Vec2i(Mathf.FloorToInt((float)x / World.ChunkSize), Mathf.FloorToInt((float)z / World.ChunkSize));
            Vec2i cBase = World.GetChunkPosition(this.BaseTile + new Vec2i(x, z)) - new Vec2i(2,2);
            int chunkWidth = Mathf.FloorToInt((float)width / World.ChunkSize)+1;
            int chunkHeight = Mathf.FloorToInt((float)height / World.ChunkSize)+1;

            for (int cx = 0; cx < chunkWidth; cx++)
            {
                for (int cz = 0; cz < chunkHeight; cz++)
                {
                    int cbx = baseChunk.x + cx;
                    int cbz = baseChunk.z + cz;
                    if (cbx < 0 || cbx > World.WorldSize - 1 || cbz < 0 || cbz > World.WorldSize - 1)
                        return false;
                    ChunkBase cb = GameGenerator.TerrainGenerator.ChunkBases[cbx, cbz];
                    
                    if(cb.RiverNode != null)
                    {
                        cb.RiverNode.AddBridge(new RiverBridge());
                        return false;
                    }
                    if (cb.RiverNode != null || cb.Lake != null || !cb.IsLand)
                    {
                        
                        return false;
                    }
                }
            }
        }


        Recti rect = new Recti(x, z, width, height);
        foreach(Recti r in BuildingPlots)
        {
            if (rect.Intersects(r))
                return false;
        }

        for (int x_ = x; x_ < x + width; x_++)
        {
            for (int z_ = z; z_ < z + height; z_++)
            {
                if (z_ >= TileSize-1 || x_ >= TileSize-1)
                {
                    return false;
                }

                int cx = WorldToChunk(x_);
                int cz = WorldToChunk(z_);
                int baseIgnore = Tile.NULL.ID;
                if(ChunkBases!= null && ChunkBases[cx,cz] != null)
                {
                    baseIgnore = Tile.GetFromBiome(ChunkBases[cx, cz].Biome).ID;
                }

                if(ignoreTile != null)
                {
                    if (GetTile(x_, z_) != 0 && GetTile(x_, z_) != ignoreTile.ID && GetTile(x_,z_) != baseIgnore)
                        return false;
                }else if (GetTile(x_, z_) != 0 && GetTile(x_, z_) != baseIgnore)
                    return false;
            }
        }

        return true;
    }

    private float GetLowestChunkHeight(int tx, int tz, int width, int height)
    {
        if (GameGenerator == null)
            return 0;
        int lx = (int)(((float)tx + this.BaseTile.x) / World.ChunkSize);
        int lz = (int)(((float)tz + this.BaseTile.z) / World.ChunkSize);
        int hx = (int)(((float)(tx + width + this.BaseTile.x)) / World.ChunkSize);
        int hz = (int)(((float)(tz + height + this.BaseTile.z)) / World.ChunkSize);
        float curHeight = float.MaxValue;
        
        for (int x = lx; x <= hx; x++)
        {
            for (int z = lz; z <= hz; z++)
            {
                float cHeight = GameGenerator.TerrainGenerator.ChunkBases[x, z].BaseHeight;
                if (cHeight < curHeight)
                    curHeight = cHeight;
            }
        }
        return curHeight;

    }
    private float GetHighestChunkHeight(int lx, int lz, int width, int height)
    {
        if (GameGenerator == null)
            return 0;
        float curHeight = -1;

        for (int x=lx; x<lx+width; x++)
        {
            for (int z = lz; z < lz + height; z++)
            {
                int cx = WorldToChunk(x);
                int cz = WorldToChunk(z);
                if (cx < 0 || cz < 0 || cx >= ChunkSize.x || cz >= ChunkSize.z)
                    continue;
                float cHeight = ChunkBaseHeights[cx, cz];
                if (cHeight > curHeight)
                    curHeight = cHeight;
            }
        }

        return curHeight;

    }
}