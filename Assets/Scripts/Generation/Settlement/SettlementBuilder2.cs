using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
public class SettlementBuilder2 : BuilderBase
{
    public static int NODE_RES = 16;
    public static bool TEST = true;

    private GenerationRandom GenerationRandom;

    private GameGenerator GameGenerator;



    public int SettlementWallBoundry = 5;

    //Defines the size of the settlement from one edge to another, in units of tiles.
    public int TileSize { get; private set; }
    public Vec2i Middle { get; private set; }
    public SettlementType SettlementType { get; private set; }
    public List<Building> Buildings { get; private set; }
    //A list that defines the mid point of the settelment entrances, up to 4 (one on each face) can be defined.
    public Vec2i Entrance { get; private set; }
    //A list of all points that are parts of a path. Contains duplicates which are removed when the final settlement is built
    //A list of the possible plots to build on.
    public List<Recti> BuildingPlots { get; }
    //Used to generate paths inside this settlement



    private SettlementShell Shell;



    public SettlementBuilder2(HeightFunction heightFunc, SettlementShell shell) : base(shell.ChunkPosition, new Vec2i(1, 1) * shell.Type.GetSize(), heightFunc, shell.ChunkBases)
    {
        Shell = shell;
        Debug.Log(shell + "," + shell.LocationData);
        GenerationRandom = new GenerationRandom(0);
        TileSize = shell.Type.GetSize() * World.ChunkSize;
        Middle = new Vec2i(TileSize / 2, TileSize / 2);
        //Tiles = new Tile[TileSize, TileSize];
        //SettlementObjects = new WorldObjectData[TileSize, TileSize];
        Buildings = new List<Building>();
        //PathNodes = new List<Vec2i>();
        BuildingPlots = new List<Recti>();
        SettlementType = shell.Type;
    }
  

    /// <summary>
    /// Array defining the paths in the settlement.
    /// True = path, false = no path
    /// </summary>
    bool[,] Path;
    /// <summary>
    /// Array defining the path nodes of the map, used to quickly find the maxmimum size of plots
    /// </summary>
    bool[,] NodeMap;
    bool[,] BuildingMap;

    List<Vec2i> AllNodes;

    private int NodeSize = 32;
    private int MapNodeSize;

    public override void Generate(GenerationRandom genRan)
    {
        GenerationRandom = genRan;
        FlattenBase();
        Entrance = DecideSettlementEntrance();        
        PlaceMainPaths();
        PlaceLargeBuildings();
        PlaceSmallPaths(40);
        PlaceRemainingBuildings();

        //CreatePaths();
        DrawPath();
        //TestDraw();
    }
    
  

    #region TEST


    private void DrawPlot(Recti r, Tile tile)
    {
        SetTiles(r.X, r.Y, r.X + r.Width, r.Y + r.Height, tile);
    }

    private void DrawPlot(Plot plot, Tile tile)
    {
        SetTiles(plot.Bounds.X, plot.Bounds.Y, plot.Bounds.X + plot.Bounds.Width, plot.Bounds.Y + plot.Bounds.Height, tile);


        foreach(Vec2i v in plot.EntranceSides)
        {
            if (v.x == -1)
            {
                SetTiles(plot.Bounds.X, plot.Bounds.Y, plot.Bounds.X + 1, plot.Bounds.Y + plot.Bounds.Height, Tile.TEST_RED);
            }
            else if (v.x == 1)
            {
                SetTiles(plot.Bounds.X + plot.Bounds.Width - 1, plot.Bounds.Y, plot.Bounds.X + plot.Bounds.Width, plot.Bounds.Y + plot.Bounds.Height, Tile.TEST_RED);
            }
            else if (v.z == -1)
            {
                SetTiles(plot.Bounds.X, plot.Bounds.Y, plot.Bounds.X + plot.Bounds.Width, plot.Bounds.Y + 1, Tile.TEST_RED);
            }
            else if (v.z == 1)
            {
                SetTiles(plot.Bounds.X, plot.Bounds.Y + plot.Bounds.Height - 1, plot.Bounds.X + plot.Bounds.Width, plot.Bounds.Y + plot.Bounds.Height, Tile.TEST_RED);
            }
        }

        

    }
    #endregion

    /// <summary>
    /// Decides the tile placement in settlement coordinates of all entrances, based on
    /// the entrance direction data <see cref="Shell.LocationData"/>
    /// </summary>
    /// <returns></returns>
    private Vec2i DecideSettlementEntrance()
    {
        List<Vec2i> entrances = new List<Vec2i>();
        for(int i=0; i<8; i += 2)
        {
            int ip1 = (i + 1) % 8;

            if(Shell.LocationData.EntranceDirections[i] || Shell.LocationData.EntranceDirections[ip1])
            {
                Vec2i side = Vec2i.QUAD_DIR[i/2];

                float xAmount = GenerationRandom.Random(0.8f, 0.95f) * side.x;
                float zAmount = GenerationRandom.Random(0.8f, 0.95f) * side.z;
                Vec2i pos = Middle + new Vec2i((int)(xAmount * TileSize / 2), (int)(zAmount * TileSize / 2));
                entrances.Add(pos);
            }

        }

        return GenerationRandom.RandomFromList(entrances);
    }

    /// <summary>
    /// Places a small amount of large paths
    /// </summary>
    private void PlaceMainPaths(int initialPathCount=9)
    {
        //Calculate node map size
        MapNodeSize = TileSize / NodeSize;
        NodeMap = new bool[MapNodeSize, MapNodeSize];
        BuildingMap = new bool[MapNodeSize, MapNodeSize];
        Path = new bool[TileSize, TileSize];
        AllNodes = new List<Vec2i>();

        Vec2i startDir;
        Vec2i entranceToMid = Middle - Entrance;
        //Find the entrance direction
        if (Mathf.Abs(entranceToMid.x) > Mathf.Abs(entranceToMid.z))
        {
            startDir = new Vec2i((int)Mathf.Sign(entranceToMid.x), 0);
        }
        else
        {
            startDir = new Vec2i(0, (int)Mathf.Sign(entranceToMid.z));
        }
        //The start direction for our path     

        //Calculate a random length
        int length = GenerationRandom.RandomInt(Shell.Type.GetSize() / 2 - 1, Shell.Type.GetSize() - 1) * NodeSize;
        //Place 'main road'
        AddPath(Entrance, startDir, length, 5, NodeMap, AllNodes, NodeSize, true);
        //We generate other initial paths
        for (int i = 0; i < initialPathCount-1; i++)
        {
            GeneratePathBranch(AllNodes);
        }
    }



    /// <summary>
    /// Calculates the possible plots allowed by the paths, and then places 
    /// the large buildings for the settlement
    /// </summary>
    private void PlaceLargeBuildings()
    {
        List<BuildingPlan> buildings = new List<BuildingPlan>();

        //Add relevent required large buildings for each settlement type
        switch (Shell.Type)
        {
            case SettlementType.CAPITAL:
                buildings.Add(Building.CAPTIALCASTLE);
                buildings.Add(Building.MARKET);
                buildings.Add(Building.BARACKS);
                break;
            case SettlementType.CITY:
                buildings.Add(Building.CITYCASTLE);
                buildings.Add(Building.MARKET);
                buildings.Add(Building.BARACKS);
                break;
            case SettlementType.TOWN:
                buildings.Add(Building.HOLD);
                buildings.Add(Building.MARKET);
                break;
            case SettlementType.VILLAGE:
                buildings.Add(Building.VILLAGEHALL);
                break;
        }

        //Sort with large buildings first
        buildings.Sort((a,b) => {
            return b.MinSize.CompareTo(a.MinSize);        
        });

        List<Plot> plots = FindPlots();

        //Large plots are at least 64x64
        List<Plot> largePlots = new List<Plot>();
        //Medium plots are 32x32 -> 63x63
        List<Plot> mediumPlots = new List<Plot>();


        //We iterate each plot and find its size, then sort them if the are large enough
        foreach (Plot r in plots)
        {
            int size = Mathf.Min(r.Bounds.Height, r.Bounds.Width);
            if (size >= 64)
                largePlots.Add(r);
            else if (size >= 32)
                mediumPlots.Add(r);
           // Tile t = Tile.TEST_YELLOW;
           // DrawPlot(r, t);
        }

        foreach(BuildingPlan bp in buildings)
        {
            if(largePlots.Count > 0)
            {
                Plot p = GenerationRandom.RandomFromList(largePlots);
                GenBuildingInPlot(bp, p);
                largePlots.Remove(p);
            }else if(mediumPlots.Count > 0)
            {
                Plot p = GenerationRandom.RandomFromList(mediumPlots);

                GenBuildingInPlot(bp, p);
                mediumPlots.Remove(p);
            }
            else
            {
                Debug.LogError("Run out of big enough plots...");
            }
        }


    }

    private void GenBuildingInPlot(BuildingPlan bp, Plot plot)
    {

        Vec2i entrance = GenerationRandom.RandomFromArray(plot.EntranceSides);
        BuildingGenerator.BuildingGenerationPlan bpPlan = new BuildingGenerator.BuildingGenerationPlan()
        {
            BuildingPlan = bp,
            EntranceSide = entrance,
            MaxHeight = plot.Bounds.Height,
            MaxWidth = plot.Bounds.Width
        };
        if (bp.MinSize > plot.Bounds.Width || bp.MinSize > plot.Bounds.Height)
            return;
        Building b = BuildingGenerator.CreateBuilding(GenerationRandom, out BuildingVoxels vox, bpPlan);

        Vec2i pos = new Vec2i(plot.Bounds.X, plot.Bounds.Y);
        if(entrance.x == -1)
        {
            pos = new Vec2i(plot.Bounds.X, plot.Bounds.Y + GenerationRandom.RandomIntFromSet(0, plot.Bounds.Height - b.Height));
        }else if (entrance.x == 1)
        {
            pos = new Vec2i(plot.Bounds.X + (plot.Bounds.Width-b.Width), plot.Bounds.Y + GenerationRandom.RandomIntFromSet(0, plot.Bounds.Height - b.Height));
        }else if (entrance.z == -1)
        {
            pos = new Vec2i(plot.Bounds.X + GenerationRandom.RandomIntFromSet(0, plot.Bounds.Width - b.Width),plot.Bounds.Y );
        }
        else if (entrance.z == 1)
        {
            pos = new Vec2i(plot.Bounds.X + GenerationRandom.RandomIntFromSet(0, plot.Bounds.Width - b.Width), plot.Bounds.Y + (plot.Bounds.Height - b.Height));
        }
        AddBuilding(b, vox, pos);
    }




    /// <summary>
    /// Attempts to place multiple buildings inside this plot
    /// Works in order. i.e, if we can fit the first, we try to fit the second
    /// If we can't fit the second, we do no further checks
    /// If we can, we check the third..  and so on
    /// We return the number of buildings succesfully placed
    /// </summary>
    /// <param name="bps"></param>
    /// <param name="startIndex">The index of the first building to place</param>
    /// <param name="plot"></param>
    /// <returns></returns>
    private int GenMultipleBuildingsInPlot(List<BuildingPlan> bps, int startIndex, Plot plot)
    {

        //Represents 4 points for 4 corners of plot
        bool[,] isCornerPossible = new bool[2, 2];
        bool[] sideHasPath = new bool[4];
        //Iterate all entrances
        foreach(Vec2i v in plot.EntranceSides)
        {
            if(v.x == -1)
            {
                sideHasPath[0] = true;
                isCornerPossible[0, 0] = true;
                isCornerPossible[0, 1] = true;
            }else if (v.x == 1)
            {
                sideHasPath[1] = true;
                isCornerPossible[1, 0] = true;
                isCornerPossible[1, 1] = true;
            }
            if (v.z == -1)
            {
                sideHasPath[2] = true;
                isCornerPossible[0, 0] = true;
                isCornerPossible[1, 0] = true;
            }else if (v.z == 1)
            {
                sideHasPath[3] = true;
                isCornerPossible[0, 1] = true;
                isCornerPossible[1, 1] = true;
            }              
        }
        //Buildings sorted by the corner they have been placed in
       
        
        Recti[,] bounds = new Recti[2, 2];
        int j = 0;
        for(int x=0; x<2; x++)
        {
            for(int z=0; z<2; z++)
            {

                
                //Index of adjacect x, and adjacent z
                int adjX = (x + 1) % 2;
                int adjZ = (x + 1) % 2;

                Recti adjacentX = bounds[adjX, z];
                int adjacentXWidth = adjacentX == null ? 0 : adjacentX.Width;
                Recti adjacentZ = bounds[x, adjZ];
                int adjacentZHeight = adjacentZ == null ? 0 : adjacentZ.Height;

                //check this corner is free
                if (isCornerPossible[x, z])
                {
                    if (startIndex + j >= bps.Count)
                        return j;

                    BuildingPlan plan = bps[startIndex + j];


                    int maxWidth = plot.Bounds.Width - adjacentXWidth;
                    int maxHeight = plot.Bounds.Height - adjacentZHeight;

                    //If it won't fit, we continue
                    if (maxWidth < plan.MinSize || maxHeight < plan.MinSize)
                        continue;

                    int desWidth = GenerationRandom.RandomInt(plan.MinSize, Mathf.Min(plan.MaxSize, maxWidth));
                    int desHeight = GenerationRandom.RandomInt(plan.MinSize, Mathf.Min(plan.MaxSize, maxHeight));

                    List<Vec2i> posSides = new List<Vec2i>();
                    Vec2i entranceSide = null;
                    Vec2i pos = null;
                    //We calculate the entrance side and position of each building
                    if (x==0 && z == 0)
                    {
                        if (sideHasPath[2])
                            posSides.Add(new Vec2i(0, -1));
                        if (sideHasPath[0])
                            posSides.Add(new Vec2i(-1, 0));
                        if (posSides.Count == 0)
                            continue;
                        entranceSide = GenerationRandom.RandomFromList(posSides);

                        pos = new Vec2i(plot.Bounds.X, plot.Bounds.Y);
                    }
                    else if (x == 0 && z == 1)
                    {
                        if (sideHasPath[3])
                            posSides.Add(new Vec2i(0, 1));
                        if (sideHasPath[0])
                            posSides.Add(new Vec2i(-1, 0));
                        if (posSides.Count == 0)
                            continue;
                        entranceSide = GenerationRandom.RandomFromList(posSides);
                        pos = new Vec2i(plot.Bounds.X, plot.Bounds.Y + plot.Bounds.Height - desHeight);

                    }
                    else if (x == 1 && z == 1)
                    {
                        if (sideHasPath[3])
                            posSides.Add(new Vec2i(0, 1));
                        if (sideHasPath[1])
                            posSides.Add(new Vec2i(1, 0));
                        if (posSides.Count == 0)
                            continue;
                        entranceSide = GenerationRandom.RandomFromList(posSides);
                        pos = new Vec2i(plot.Bounds.X + plot.Bounds.Width - desWidth, plot.Bounds.Y + plot.Bounds.Height - desHeight);

                    }
                    else if (x == 1 && z == 0)
                    {
                        if (sideHasPath[2])
                            posSides.Add(new Vec2i(0, -1));
                        if (sideHasPath[1])
                            posSides.Add(new Vec2i(1, 0));
                        if (posSides.Count == 0)
                            continue;
                        entranceSide = GenerationRandom.RandomFromList(posSides);
                        pos = new Vec2i(plot.Bounds.X + plot.Bounds.Width - desWidth, plot.Bounds.Y);

                    }

                    Recti curBound = new Recti(pos.x, pos.z, desWidth, desHeight);
                    bool intersect = false;
                    foreach(Recti ri in bounds)
                    {
                        if (ri != null)
                            if (ri.Intersects(curBound))
                                intersect = true;
                    }
                    if (intersect)
                        continue;
                    bounds[x, z] = curBound;

                    BuildingGenerator.BuildingGenerationPlan curCorn = new BuildingGenerator.BuildingGenerationPlan() {
                        BuildingPlan = bps[startIndex + j],
                        MaxWidth = maxWidth,
                        MaxHeight = maxHeight,
                        EntranceSide = entranceSide,
                        DesiredSize = new Vec2i(desWidth, desHeight)
                    };

                    Building build = BuildingGenerator.CreateBuilding(GenerationRandom, out BuildingVoxels vox, curCorn);
                    AddBuilding(build, vox, pos);
                    j++;
                }
            }
        }


        return j;

    }

    private void PlaceSmallPaths(int count = 10)
    {
        for (int i = 0; i < count - 1; i++)
        {
            GeneratePathBranch(AllNodes);
        }
    }

    private void PlaceRemainingBuildings()
    {
        List<BuildingPlan> buildings = new List<BuildingPlan>();
        if(Shell.RequiredBuildings != null)
            buildings.AddRange(Shell.RequiredBuildings);
        //Add relevent required large buildings for each settlement type
        switch (Shell.Type)
        {
            case SettlementType.CAPITAL:
                buildings.Add(Building.BLACKSMITH);
                buildings.Add(Building.TAVERN);
                buildings.Add(Building.TAVERN);
                buildings.Add(Building.BAKERY);
                break;
            case SettlementType.CITY:
                buildings.Add(Building.CITYCASTLE);
                buildings.Add(Building.MARKET);
                buildings.Add(Building.BARACKS);
                break;
            case SettlementType.TOWN:
                buildings.Add(Building.HOLD);
                buildings.Add(Building.MARKET);
                break;
            case SettlementType.VILLAGE:
                buildings.Add(Building.VILLAGEHALL);
                break;
        }
        for(int i=0; i<Shell.Type.GetSize() * 6; i++)
        {
            buildings.Add(Building.HOUSE);
        }

        //Sort with large buildings first
        buildings.Sort((a, b) => {
            return b.MinSize.CompareTo(a.MinSize);
        });
        List<Plot> plots = FindPlots();

        plots.Sort((a, b) => {
            return Mathf.Min(b.Bounds.Width, b.Bounds.Height).CompareTo(Mathf.Min(a.Bounds.Width, a.Bounds.Height));
        });

       // foreach (Plot p in plots)
        //    DrawPlot(p, Tile.TEST_BLUE);
//return;

        for(int i=0; i<buildings.Count; i++)
        {
            if (plots.Count == 0)
                break;
            Plot p = plots[0];
            plots.RemoveAt(0);

            if(buildings[i].MinSize < p.Bounds.Width / 2)
            {
                int jump = GenMultipleBuildingsInPlot(buildings, i, p);
                if(jump == 0)
                {
                    GenBuildingInPlot(buildings[i], p);
                }
                else
                {
                    i += (jump - 1);
                }
            }

        }
   
    }

    

    /// <summary>
    /// Only for test, to display positions of nodes
    /// </summary>
    private List<Vec2i> PATHNODES;
    private void CreatePaths()
    {
        MapNodeSize = TileSize / NodeSize;
        NodeMap = new bool[MapNodeSize, MapNodeSize];
        Path = new bool[TileSize, TileSize];
        AllNodes = new List<Vec2i>();

        Vec2i entranceSide = null;
        Vec2i entranceToMid = Middle - Entrance;

        if(Mathf.Abs(entranceToMid.x) > Mathf.Abs(entranceToMid.z))
        {
            entranceSide = new Vec2i((int)Mathf.Sign(entranceToMid.x), 0);
        }
        else {
            entranceSide = new Vec2i(0, (int)Mathf.Sign(entranceToMid.z));
        }

        Vec2i startDir = entranceSide;

        List<Vec2i> nodes = new List<Vec2i>();

        MapNodeSize = TileSize / NodeSize;

         NodeMap = new bool[MapNodeSize, MapNodeSize];

        int length = GenerationRandom.RandomInt(Shell.Type.GetSize()/2 - 1, Shell.Type.GetSize()-1) * NodeSize;
        AddPath(Entrance, startDir, length, 5, NodeMap, nodes, NodeSize, true);

        //We generate some initial paths
        for (int i=0; i<8; i++)
        {
            GeneratePathBranch(nodes);
        }
        //We then find the plots this set of paths results in
        List<Plot> plots = FindPlots();
        
        //Large plots are at least 64x64
        List<Plot> largePlots = new List<Plot>();
        //Medium plots are 32x32 -> 63x63
        List<Plot> mediumPlots = new List<Plot>();
        //We iterate each plot and find its size, then sort them if the are large enough
        foreach (Plot r in plots)
        {
            int size = Mathf.Min(r.Bounds.Height, r.Bounds.Width);
            if (size >= 64)
                largePlots.Add(r);
            else if (size >= 32)
                mediumPlots.Add(r);
            Tile t = Tile.TEST_YELLOW;
            DrawPlot(r, t);
        }      
        
        PATHNODES = nodes;
    }


    private void GeneratePathBranch(List<Vec2i> nodes, int remIt=5)
    {
        Vec2i start = GenerationRandom.RandomFromList(nodes);
        Vec2i dir = GenerationRandom.RandomQuadDirection();
        Vec2i test = start + dir * NodeSize;

        int nodeX = test.x / NodeSize;
        int nodeZ = test.z / NodeSize;

        if (nodeX >= MapNodeSize-1 || nodeZ >= MapNodeSize-1 || NodeMap[nodeX, nodeZ])
        {
            remIt--;
            if (remIt <= 0)
                return;
            GeneratePathBranch(nodes, remIt);
        }
        int length = GenerationRandom.RandomInt(3, 7) * NodeSize;
        AddPath(start, dir, length, 3, NodeMap, nodes, NodeSize);
    }

    /// <summary>
    /// Creates a straight path from the start position, travelling in the direction specified for the length specified.
    /// 
    /// </summary>
    /// <param name="start">The position the path should start at</param>
    /// <param name="dir">The direction the path should be in. 
    /// This must be a QUAD_DIR <see cref="Vec2i.QUAD_DIR"/></param>
    /// <param name="length">The length from the start of the path to the end, 
    /// assuming there is no obsticle and that the end is within the map bounds</param>
    /// <param name="width">The total widith of the path</param>
    /// <param name="nodeMap">A bool[,] that defines all nodes {true=node, false=no node}</param>
    /// <param name="allNodePos">A list contaning all nodes, we add created nodes to this</param>
    /// <param name="nodeSize">The distance between each node. Default=32</param>
    /// <param name="ignoreBoundry">If true, we allow the path to extend the whole size of the settlement. If false,
    ///  we do not allow the path to exist inside the settlement boundry </param>
    /// <param name="crossRoadProb">Normally, the algorithm will terminate upon reaching another node (cross road). 
    /// if RNG in range (0,1) is less than this number, we will continue the path as normal, producing a cross road </param>
    private void AddPath(Vec2i start, Vec2i dir, int length, int width, bool[,] nodeMap, List<Vec2i> allNodePos, 
        int nodeSize = 32, bool ignoreBoundry=false, float crossRoadProb = 0.1f)
    {
        //Get half width, and direction perpendicular to path
        int hW = width / 2;
        Vec2i perp = Vec2i.GetPerpendicularDirection(dir);
        //iterate over paths length
        for(int l=0; l<length; l++)
        {
            //Get length based position
            //Check for boundry & obsticles, then for nodes
            Vec2i v1 = start + new Vec2i(dir.x * l, dir.z * l);
            if (!InBounds(v1, ignoreBoundry))
                break;
            if (!IsTileFree(v1.x, v1.z))
                break;

            //Get node coordinates
            int nodeX = v1.x / nodeSize ;
            int nodeZ = v1.z / nodeSize ;

            //If we are at a node, and it isn't the first in the path
            if (l > 0 && l % nodeSize == 0 )
            {
                
                //If this path is hitting another node -> 
                //We have reached this paths end
                //TODO - maybe make a small probablility of continuing to produce more cross roads
                if (nodeMap[nodeX, nodeZ] && GenerationRandom.Random() > crossRoadProb)
                    return;
                //Place a node here
                nodeMap[nodeX, nodeZ] = true;
                //
                allNodePos.Add(new Vec2i(nodeX*nodeSize, nodeZ * nodeSize));
                

                //If instead, this is the firstnode, but no node is placed
            }
            else if(l==0 && !nodeMap[nodeX, nodeZ])
            {
                //We place it
                nodeMap[nodeX, nodeZ] = true;
                //And add to random selection if required
                if (InBounds(v1))
                    allNodePos.Add(v1);
            }
            //If we are able to build this path, we 
            //iterate over the width
            for (int w=-hW; w<= hW; w++)
            {
                Vec2i v = v1 + new Vec2i(perp.x * w, perp.z * w);
                if (!InBounds(v, ignoreBoundry))
                    return;
                //Define the path to exist at this point
                Path[v.x, v.z] = true;
                
            }
        }
    }

    private List<Plot> FindPlots(int pathCornerSearch = 4)
    {
        List<Plot> plots = new List<Plot>();

 
        //Nodes already checked
        bool[,] nodeSkip = new bool[MapNodeSize, MapNodeSize];



        for (int x=1; x<MapNodeSize-1; x++)
        {
            for (int z = 1; z < MapNodeSize-1; z++)
            {
                //If we don't wish to skip this node
                if (!nodeSkip[x, z] && !BuildingMap[x,z])
                {

                    Debug.Log("Starting plot " + plots.Count + " at " + new Vec2i(x,z));

                    nodeSkip[x, z] = true;
                    //We find the tile position that the paths are not on
                    Vec2i tilePos = new Vec2i(x * NodeSize, z * NodeSize);
                    //Works up to paths
                    for (int i = 0; i < pathCornerSearch; i++)
                    {                     
                        
                        if (!Path[tilePos.x + i, tilePos.z + i] )
                        {
                            tilePos = new Vec2i(tilePos.x + i, tilePos.z + i);
                            break;
                        }
                    }

                    int size = 1;

                    for (int i = 1; i < 4; i++)
                    {
                        bool valid = true;
                        for (int x_ = 1; x_ <= i; x_++)
                        {
                            for (int z_ = 1; z_ <= i; z_++)
                            {

                                if (x + x_ >= MapNodeSize - 1 || z + z_ >= MapNodeSize - 1 || NodeMap[x + x_, z + z_] || BuildingMap[x+x_, z+z_])
                                    valid = false;
                               
                                

                            }
                            if (!valid)
                                break;
                        }
                        
                        size = i;
                        if (!valid)
                            break;
                    }
                    for (int x_ = 0; x_ < size; x_++)
                    {
                        for (int z_ = 0; z_ < size; z_++)
                        {
                            nodeSkip[x + x_, z + z_] = true;
                        }
                    }
                    Vec2i endPos = new Vec2i((x + size) * NodeSize, (z + size) * NodeSize);

                    //Works up to paths
                    for (int i = 0; i < pathCornerSearch; i++)
                    {
                        if (!Path[endPos.x - i, endPos.z - i])
                        {
                            endPos = new Vec2i(endPos.x - i + 1, endPos.z - i + 1);
                            break;
                        }
                    }
                    Recti bounds = new Recti(tilePos, endPos - tilePos);
                    List<Vec2i> sides = new List<Vec2i>(4);
                    if (Path[tilePos.x-1, tilePos.z])
                        sides.Add(new Vec2i(-1, 0));
                    if (Path[tilePos.x, tilePos.z - 1])
                        sides.Add(new Vec2i(0, -1));
                    if (Path[tilePos.x + bounds.Width + 1, tilePos.z])
                        sides.Add(new Vec2i(1, 0));
                    if (Path[tilePos.x, tilePos.z + bounds.Height + 1])
                        sides.Add(new Vec2i(0, 1));
                    if (sides.Count == 0)
                        sides.Add(new Vec2i(1, 0));
                    Plot p = new Plot(bounds, sides.ToArray());
                    plots.Add(p);

                }


                

                
            }
        }


        return plots;
    }


    private void DrawPath() { 
    
        for(int x=0; x<TileSize; x++)
        {
            for (int z = 0;z < TileSize; z++)
            {
                if (Path[x, z])
                    SetTile(x, z, Tile.STONE_PATH);
            }
        }

        for (int x = 0; x < MapNodeSize; x++)
        {
            for (int z = 0; z < MapNodeSize; z++)
            {
                if (NodeMap[x, z])
                    SetTile(x*NodeSize, z* NodeSize, Tile.TEST_BLUE);
            }
        }


      
    }






    private void SetTiles(int minX, int minZ, int maxX, int maxZ, Tile tile)
    {
        if (!InBounds(new Vec2i(minX, minZ)) || !InBounds(new Vec2i(maxX, maxZ)))
            return;
        for (int x = minX; x < maxX; x++)
        {
            for (int z = minZ; z < maxZ; z++)
            {
                SetTile(x, z, tile);
                if (tile == Tile.TEST_BLUE)
                {

                    AddVoxel(x, 0, z, Voxel.dirt_path);
                }
            }
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
            if (GetTile(toOut.x, toOut.z) == 0)
                return toOut;
            toOut = GenerationRandom.RandomVec2i(1, TileSize - 2);

        }


    }


    public bool InBounds(Vec2i v, bool ignoreBoundry=false)
    {
        if(ignoreBoundry)
            return v.x >= 0 && v.x < TileSize && v.z >= 0 && v.z < TileSize;
        return v.x >= World.ChunkSize && v.x < TileSize- World.ChunkSize && v.z >= World.ChunkSize && v.z < TileSize- World.ChunkSize;
    }
       

    public Recti AddBuilding(Building b, BuildingVoxels vox, Vec2i pos)
    {

        if (!IsAreaFree(pos.x, pos.z, b.Width, b.Height))
            return null;


        int nodeX = Mathf.FloorToInt(((float)pos.x-1) / NodeSize);
        int nodeZ = Mathf.FloorToInt(((float)pos.z-1) / NodeSize);
        int nodeWidth = Mathf.CeilToInt(((float)b.Width+1) / NodeSize);
        int nodeHeight = Mathf.CeilToInt(((float)b.Height+1) / NodeSize);
        for(int x=nodeX; x<nodeX+nodeWidth; x++)
        {
            for (int z = nodeZ; z < nodeZ + nodeHeight; z++)
            {
                BuildingMap[x, z] = true;
            }
        }

        int height = (int)FlattenArea(pos.x - 1, pos.z - 1, b.Width + 2, b.Height + 2);

        //Debug.Log("Adding building " + b);
        //SetTiles(pos.x, pos.z, b.Width, b.Height, b.BuildingTiles);
        for (int x = 0; x < b.Width; x++)
        {
            for (int z = 0; z < b.Height; z++)
            {



                SetTile(x + pos.x, z + pos.z, b.BuildingTiles[x, z]);


                //SetHeight(x + pos.x, z + pos.z, maxHeight);
                for (int y = 0; y < vox.Height; y++)
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
        return new Recti(pos.x - 1, pos.z - 1, b.Width + 2, b.Height + 2);
    }




    public bool IsTileFree(int x, int z)
    {
        if (x < 0 || z < 0 || x >= TileSize || z >= TileSize)
            return false;

         foreach (Recti ri in BuildingPlots)
             if (ri.ContainsPoint(x, z))
                 return false;
                 
        if (GetTile(x, z) != 0)
            return false;
        return true;
    }
    public bool IsAreaFree(int x, int z, int width, int height, Tile ignoreTile = null)
    {
        if (x < 0 || z < 0)
            return false;
        if (GameGenerator != null)
        {
            Vec2i baseChunk = BaseChunk + new Vec2i(Mathf.FloorToInt((float)x / World.ChunkSize), Mathf.FloorToInt((float)z / World.ChunkSize));
            Vec2i cBase = World.GetChunkPosition(this.BaseTile + new Vec2i(x, z)) - new Vec2i(2, 2);
            int chunkWidth = Mathf.FloorToInt((float)width / World.ChunkSize) + 1;
            int chunkHeight = Mathf.FloorToInt((float)height / World.ChunkSize) + 1;

            for (int cx = 0; cx < chunkWidth; cx++)
            {
                for (int cz = 0; cz < chunkHeight; cz++)
                {
                    int cbx = baseChunk.x + cx;
                    int cbz = baseChunk.z + cz;
                    if (cbx < 0 || cbx > World.WorldSize - 1 || cbz < 0 || cbz > World.WorldSize - 1)
                        return false;
                    ChunkBase cb = GameGenerator.TerrainGenerator.ChunkBases[cbx, cbz];

                    if (cb.RiverNode != null)
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
        foreach (Recti r in BuildingPlots)
        {
            if (rect.Intersects(r))
                return false;
        }

        for (int x_ = x; x_ < x + width; x_++)
        {
            for (int z_ = z; z_ < z + height; z_++)
            {
                if (z_ >= TileSize - 1 || x_ >= TileSize - 1)
                {
                    return false;
                }

                int cx = WorldToChunk(x_);
                int cz = WorldToChunk(z_);
                int baseIgnore = Tile.NULL.ID;
                if (ChunkBases != null && ChunkBases[cx, cz] != null)
                {
                    baseIgnore = Tile.GetFromBiome(ChunkBases[cx, cz].Biome).ID;
                }

                if (ignoreTile != null)
                {
                    if (GetTile(x_, z_) != 0 && GetTile(x_, z_) != ignoreTile.ID && GetTile(x_, z_) != baseIgnore)
                        return false;
                }
                else if (GetTile(x_, z_) != 0 && GetTile(x_, z_) != baseIgnore)
                    return false;
            }
        }

        return true;
    }



}
public class Plot
{
    public Recti Bounds { get; private set; }
    public Vec2i[] EntranceSides { get; private set; }
    public Plot(Recti bounds, Vec2i[] entranceSides)
    {
        Bounds = bounds;
        EntranceSides = entranceSides;
    }
 

}
