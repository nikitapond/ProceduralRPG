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
    public Vec2i EntranceSide { get; private set; }

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
        Debug.BeginDeepProfile("Paths1");
        PlaceMainPaths(Shell.Type.GetSize()/ 2 + 1);
        Debug.EndDeepProfile("Paths1");
        Debug.BeginDeepProfile("MainBuild");
        PlaceLargeBuildings();
        Debug.EndDeepProfile("MainBuild");
        //DrawPath();
        

        Debug.BeginDeepProfile("Path2");
        PlaceSmallPaths(Shell.Type.GetSize() * 3);
        Debug.EndDeepProfile("Path2");

        

        Debug.BeginDeepProfile("FinalBuild");
        PlaceRemainingBuildings();
        Debug.EndDeepProfile("FinalBuild");
        //CreatePaths();
        DrawPath();
        GenerateDetails();
        SetTileBase();
        
        //TestDraw();
    }



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
    
    #region paths 
    /// <summary>
    /// Places a small amount of large paths
    /// </summary>
    private void PlaceMainPaths(int initialPathCount = 9)
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
        EntranceSide = startDir;
        //Calculate a random length
        int length = GenerationRandom.RandomInt(Shell.Type.GetSize() / 2 - 1, Shell.Type.GetSize() - 1) * NodeSize;
        //Place 'main road'
        AddPath(Entrance, startDir, length, 7, NodeMap, AllNodes, NodeSize, true);
        //We generate other initial paths
        for (int i = 0; i < initialPathCount - 1; i++)
        {
            GeneratePathBranch(AllNodes, width:5);
        }
    }



    /// <summary>
    /// Generates a number of paths off existing nodes
    /// </summary>
    /// <param name="count"></param>
    private void PlaceSmallPaths(int count = 10, int width=3)
    {
        for (int i = 0; i < count - 1; i++)
        {
            GeneratePathBranch(AllNodes, width);
        }
    }

    private void GeneratePathBranch(List<Vec2i> nodes, int width=3, int remIt = 5)
    {
        if (nodes.Count == 0)
            return;

        Vec2i start = GenerationRandom.RandomFromList(nodes);
        Vec2i dir = GenerationRandom.RandomQuadDirection();
        Vec2i perp = Vec2i.GetPerpendicularDirection(dir);


        Vec2i test = start + dir * NodeSize;

        int nodeX = test.x / NodeSize;
        int nodeZ = test.z / NodeSize;
        if(!InNodeBounds(nodeX, nodeZ))
        {
            remIt--;
            if (remIt <= 0)
                return;
            //nodes.Remove(start);
            GeneratePathBranch(nodes, remIt);
            return;
        }
        //Check if node is outside of main map
        if (nodeX >= MapNodeSize - 1 || nodeZ >= MapNodeSize - 1 || NodeMap[nodeX, nodeZ])
        {
            remIt--;
            if (remIt <= 0)
                return;
            //nodes.Remove(start);
            GeneratePathBranch(nodes, remIt);
            return;
        }
        int length = GenerationRandom.RandomInt(3, 7) * NodeSize;
        AddPath(start, dir, length, width, NodeMap, nodes, NodeSize);
    }

    private bool InNodeBounds(int x, int z)
    {
        if (x >= MapNodeSize - 1 || z >= MapNodeSize - 1 || x < 0 || z < 0)
            return false;
        return true;
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
        int nodeSize = 32, bool ignoreBoundry = false, float crossRoadProb = 0.1f)
    {
        //Get half width, and direction perpendicular to path
        int hW = width / 2;
        Vec2i perp = Vec2i.GetPerpendicularDirection(dir);


  
        for(int l=0; l<length; l++)
        {
            /*
            Vec2i nextNodePos = start + new Vec2i(dir.x * (n + 1) * nodeSize, dir.z * (n + 1) * nodeSize);
            if (!InBounds(nextNodePos, ignoreBoundry))
                break;
                */
            //Get length based position
            //Check for boundry & obsticles, then for nodes
            Vec2i v1 = start + new Vec2i(dir.x * l, dir.z * l);

            Vec2i nextNode = start + new Vec2i(dir.x * (l + nodeSize), dir.z * (l + nodeSize));
            

            if (!InBounds(v1, ignoreBoundry) || !InBounds(nextNode, ignoreBoundry))
                break;
            if (!IsTileFree(v1.x, v1.z))
                break;

            //Get node coordinates
            int nodeX = v1.x / nodeSize;
            int nodeZ = v1.z / nodeSize;

            //If we are at a node, and it isn't the first in the path
            if (l > 0 && l % nodeSize == 0)
            {

                //If this path is hitting another node -> 
                //We have reached this paths end
                //TODO - maybe make a small probablility of continuing to produce more cross roads
                if (nodeMap[nodeX, nodeZ] && GenerationRandom.Random() > crossRoadProb)
                    return;
                //Place a node here
                nodeMap[nodeX, nodeZ] = true;
                //
                allNodePos.Add(new Vec2i(nodeX * nodeSize, nodeZ * nodeSize));


                //If instead, this is the firstnode, but no node is placed
            }
            else if (l == 0 && !nodeMap[nodeX, nodeZ])
            {
                //We place it
                nodeMap[nodeX, nodeZ] = true;
                //And add to random selection if required
                if (InBounds(v1))
                    allNodePos.Add(v1);
            }
            //If we are able to build this path, we 
            //iterate over the width
            for (int w = -hW; w <= hW; w++)
            {
                Vec2i v = v1 + new Vec2i(perp.x * w, perp.z * w);
                if (!InBounds(v, ignoreBoundry))
                    return;
                //Define the path to exist at this point
                Path[v.x, v.z] = true;

            }
        }
    }
    
    /// <summary>
    /// Draws all paths on map based on data in <see cref="Path"/>
    /// </summary>
    private void DrawPath()
    {

        for (int x = 0; x < TileSize; x++)
        {
            for (int z = 0; z < TileSize; z++)
            {
                if (Path[x, z])
                    SetTile(x, z, Tile.STONE_PATH);
            }
        }
        foreach(Vec2i v in AllNodes)
        {
            SetTile(v.x, v.z, Tile.TEST_BLUE);
        }
    }
    #endregion

    #region buildingPlacement
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
        buildings.Sort((a, b) => {
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
            if (size >= 48)
                largePlots.Add(r);
            else if (size >= 32)
                mediumPlots.Add(r);
            // Tile t = Tile.TEST_YELLOW;
            // DrawPlot(r, t);
        }

        foreach (BuildingPlan bp in buildings)
        {
            if (largePlots.Count > 0)
            {
                Plot p = GenerationRandom.RandomFromList(largePlots);
                GenBuildingInPlot(bp, p);
                largePlots.Remove(p);
            }
            else if (mediumPlots.Count > 0)
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

    /// <summary>
    /// Attempts to place generate a building based on <paramref name="bp"/> in the plot specified
    /// </summary>
    /// <param name="bp"></param>
    /// <param name="plot"></param>
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
        if (entrance.x == -1)
        {
            pos = new Vec2i(plot.Bounds.X, plot.Bounds.Y + GenerationRandom.RandomIntFromSet(0, plot.Bounds.Height - b.Height));
        }
        else if (entrance.x == 1)
        {
            pos = new Vec2i(plot.Bounds.X + (plot.Bounds.Width - b.Width), plot.Bounds.Y + GenerationRandom.RandomIntFromSet(0, plot.Bounds.Height - b.Height));
        }
        else if (entrance.z == -1)
        {
            pos = new Vec2i(plot.Bounds.X + GenerationRandom.RandomIntFromSet(0, plot.Bounds.Width - b.Width), plot.Bounds.Y);
        }
        else if (entrance.z == 1)
        {
            pos = new Vec2i(plot.Bounds.X + GenerationRandom.RandomIntFromSet(0, plot.Bounds.Width - b.Width), plot.Bounds.Y + (plot.Bounds.Height - b.Height));
        }
        Recti r = AddBuilding(b, vox, pos);
        if (r != null)
            BuildingPlots.Add(r);
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
    /// <returns>The number of buildings placed in this plot, between 0 and 4 (incusive)</returns>
    private int GenMultipleBuildingsInPlot(List<BuildingPlan> bps, int startIndex, Plot plot)
    {

        //Represents 4 points for 4 corners of plot
        bool[,] isCornerPossible = new bool[2, 2];
        bool[] sideHasPath = new bool[4];
        //Iterate all entrances
        foreach (Vec2i v in plot.EntranceSides)
        {
            if (v.x == -1)
            {
                sideHasPath[0] = true;
                isCornerPossible[0, 0] = true;
                isCornerPossible[0, 1] = true;
            }
            else if (v.x == 1)
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
            }
            else if (v.z == 1)
            {
                sideHasPath[3] = true;
                isCornerPossible[0, 1] = true;
                isCornerPossible[1, 1] = true;
            }
        }
        //Buildings sorted by the corner they have been placed in


        Recti[,] bounds = new Recti[2, 2];
        int j = 0;
        for (int x = 0; x < 2; x++)
        {
            for (int z = 0; z < 2; z++)
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
                    if (x == 0 && z == 0)
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
                    foreach (Recti ri in bounds)
                    {
                        if (ri != null)
                            if (ri.Intersects(curBound))
                                intersect = true;
                    }
                    if (intersect)
                        continue;
                    bounds[x, z] = curBound;

                    BuildingGenerator.BuildingGenerationPlan curCorn = new BuildingGenerator.BuildingGenerationPlan()
                    {
                        BuildingPlan = bps[startIndex + j],
                        MaxWidth = maxWidth,
                        MaxHeight = maxHeight,
                        EntranceSide = entranceSide,
                        DesiredSize = new Vec2i(desWidth, desHeight)
                    };

                    Building build = BuildingGenerator.CreateBuilding(GenerationRandom, out BuildingVoxels vox, curCorn);
                    Recti r = AddBuilding(build, vox, pos);
                    if (r != null)
                        BuildingPlots.Add(r);
                    j++;
                }
            }
        }
        return j;

    }

    /// <summary>
    /// Decides the remaining buildings that are required for this settlement, based on its type and on the 
    /// <see cref="SettlementShell.RequiredBuildings"/>. Will attempt to place each building in a shared plot
    /// </summary>
    private void PlaceRemainingBuildings()
    {
        List<BuildingPlan> buildings = new List<BuildingPlan>();
        if (Shell.RequiredBuildings != null)
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
        for (int i = 0; i < Shell.Type.GetSize() * 6; i++)
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


        //Iterate each building 
        for (int i = 0; i < buildings.Count; i++)
        {
            if (plots.Count == 0)
            {
                PlaceSmallPaths(10, 1);
                plots = FindPlots();

                plots.Sort((a, b) => {
                    return Mathf.Min(b.Bounds.Width, b.Bounds.Height).CompareTo(Mathf.Min(a.Bounds.Width, a.Bounds.Height));
                });

                Debug.Log("Run out of plots - creating more succesful? " + plots.Count);
                
            }
            if(plots.Count == 0)
                break; 

            Plot p = plots[0];
            plots.RemoveAt(0);

            if (buildings[i].MinSize <= p.Bounds.Width / 2)
            {
                //Attempt to place multiple buildings in this plot
                int jump = GenMultipleBuildingsInPlot(buildings, i, p);
                if (jump == 0)
                {
                    //if we fail, place a single building
                    GenBuildingInPlot(buildings[i], p);
                }
                else
                {
                    //if we don't fail, incriment iterator to correct place
                    i += (jump - 1);
                }
            }

        }

    }

    #endregion

    #region details

    private void GenerateDetails()
    {

        List<Vec2i> freeAreas = new List<Vec2i>(100);
        int areaSize = 2;

        int boundry = 2 * World.ChunkSize;
        int areaMapSize = (TileSize - 2 * boundry) / areaSize;

        for(int x= boundry; x<TileSize - boundry; x+=areaSize)
        {
            for (int z = boundry; z < TileSize - boundry; z+=areaSize)
            {

                int areaX = (x - boundry) / areaSize;
                int areaZ = (z - boundry) / areaSize;

                bool isFree = true;
                for(int i=0; i<areaSize; i++)
                {
                    for (int j = 0; j < areaSize; j++)
                    {
                        if (GetTile(x + i, z + j) != 0)
                            isFree = false;
                    }
                }
                if (isFree)
                {
                    freeAreas.Add(new Vec2i(x, z));
                }                    
            }
        }

        for(int i=0; i<20; i++)
        {
            Vec2i pos = GenerationRandom.RandomFromList(freeAreas);
            freeAreas.Remove(pos);
            Anvil anvil = new Anvil();
            anvil.SetPosition(pos);
            AddObject(anvil);

        }



    }

    
    /// <summary>
    /// Calculates all available plots on the map, based on current map state, building map, and paths
    /// </summary>
    /// <param name="pathCornerSearch"></param>
    /// <returns></returns>

    private float GetSquareWallLength(float theta)
    {
        theta = ((theta + 45) % 90 - 45) * Mathf.Deg2Rad;
        return 1 / Mathf.Cos(theta);
    }
    public SettlementWall GenerateWall(int points = 20)
    {
        List<Vec2i> wall = new List<Vec2i>();
        Debug.Log("TEST:" + EntranceSide);
        Vec2i entrancePerp = Vec2i.GetPerpendicularDirection(EntranceSide);

        int entranceHalfWidth = 3;
        Vec2i start = Entrance - entrancePerp * entranceHalfWidth;
        Vec2i end = Entrance + entrancePerp * entranceHalfWidth;
        Debug.Log(start + "_" + end);
        //Displacement from middle to start and end point
        Vector2 midToStart = start - Middle;
        Vector2 midToEnd = end - Middle;

        wall.Add(start);

        float startTheta = Vector2.Angle(Vector2.up, midToStart);
        float startEndThetaDelta = Vector2.Angle(midToEnd, midToStart);
        Debug.Log(startTheta);
        Debug.Log("2" + startEndThetaDelta);

        float tTheta = 360 - startEndThetaDelta;
        float dTheta = tTheta / points;
        Debug.Log("t" + tTheta);
        for(int t=0; t< points; t++)
        {
            float theta = (t * dTheta + startTheta) % 360;

            float rawRad = GetSquareWallLength(theta);
            int maxRad = (int)((World.ChunkSize * Shell.Type.GetSize() / 2f) * rawRad) - 2;
            int minRad = (int)((World.ChunkSize * (Shell.Type.GetSize() - 1) / 2f) * rawRad);
            int rad = GenerationRandom.RandomInt(minRad, maxRad);

            int x = (int)( Middle.x + Mathf.Sin(theta * Mathf.Deg2Rad) * rad);
            int z = (int)(Middle.z + Mathf.Cos(theta * Mathf.Deg2Rad) * rad);
            wall.Add(new Vec2i(x, z));

        }
        wall.Add(end);




        return new SettlementWall(wall);
    }
    #endregion

    private List<Plot> FindPlots(int pathCornerSearch = 7)
    {
        List<Plot> plots = new List<Plot>(); 
        //Nodes already checked
        bool[,] nodeSkip = new bool[MapNodeSize, MapNodeSize];
        //iterate settlement nodes
        for (int x=1; x<MapNodeSize-1; x++)
        {
            for (int z = 1; z < MapNodeSize-1; z++)
            {
                //If we don't wish to skip this node
                if (!nodeSkip[x, z] && !BuildingMap[x,z])
                {
                    //After starting, we can now skip
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
                    //
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
                                //Ensure node will be skipped next time
                                    
                            }
                            if (!valid)
                                break;
                        }
                        for(int x_=0; x_<size; x_++)
                        {
                            for(int z_=0; z_<size; z_++)
                            {
                                nodeSkip[x + x_, z + z_] = true;
                            }
                        }
                        
                        size = i;
                        if (!valid)
                            break;
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
                        continue;
                    Plot p = new Plot(bounds, sides.ToArray());
                    plots.Add(p);

                }


                

                
            }
        }


        return plots;
    }
     
    /// <summary>
    /// Adds the building to the settlement
    /// </summary>
    /// <param name="b"></param>
    /// <param name="vox"></param>
    /// <param name="pos"></param>
    /// <returns></returns>
    public Recti AddBuilding(Building b, BuildingVoxels vox, Vec2i pos)
    {

        if (!IsAreaFree(pos.x, pos.z, b.Width, b.Height))
            return null;


        int nodeX = Mathf.FloorToInt(((float)pos.x) / NodeSize);
        int nodeZ = Mathf.FloorToInt(((float)pos.z) / NodeSize);
        int nodeWidth = Mathf.CeilToInt(((float)b.Width) / NodeSize);
        int nodeHeight = Mathf.CeilToInt(((float)b.Height) / NodeSize);
        for(int x=nodeX; x<nodeX+nodeWidth; x++)
        {
            for (int z = nodeZ; z < nodeZ + nodeHeight; z++)
            {
                BuildingMap[x, z] = true;
            }
        }

        int height = (int)FlattenArea(pos.x - 1, pos.z - 1, b.Width + 2, b.Height + 2);
        Recti bo = new Recti(pos.x-1, pos.z-1, b.Width+2, b.Height+2);
        foreach(Recti bound in BuildingPlots)
        {
            if (bo.Intersects(bound))
                return null;

        }
        BuildingPlots.Add(bo);

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
        foreach (WorldObjectData obj in b.GetBuildingExternalObjects())
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

        if (b.BuildingSubworld != null)
            AddObject(b.ExternalEntranceObject as WorldObjectData, true);
        //    World.Instance.AddSubworld(b.BuildingSubworld);

        b.CalculateSpawnableTiles(vox);
        Buildings.Add(b);
        //PathNodes.Add(b.Entrance);
        return bo;
    }

    #region util
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

    public bool InBounds(Vec2i v, bool ignoreBoundry = false)
    {
        if (ignoreBoundry)
            return v.x >= 0 && v.x < TileSize && v.z >= 0 && v.z < TileSize;
        return v.x >= World.ChunkSize && v.x < TileSize - World.ChunkSize && v.z >= World.ChunkSize && v.z < TileSize - World.ChunkSize;
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
    #endregion


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
