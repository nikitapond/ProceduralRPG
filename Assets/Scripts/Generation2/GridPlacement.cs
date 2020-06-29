using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class GridPlacement
{
    public static int GridPointSize = 32;
    public static int GridSize = World.WorldSize / GridPointSize - 1;
    private GameGenerator2 GameGen;
    private GenerationRandom GenRan;
    public GridPoint[,] GridPoints;

    private GridPathFinder GPF;

    public GridPlacement(GameGenerator2 gameGen)
    {
        GameGen = gameGen;
        GridPoints = new GridPoint[GridSize, GridSize];
        GenRan = new GenerationRandom(gameGen.Seed);
        GPF = new GridPathFinder(this);
    }

    public GridPoint GetNearestPoint(Vec2i cPos)
    {
        int gx = (int)Mathf.RoundToInt(cPos.x / (float)GridPointSize);
        int gz = (int)Mathf.RoundToInt(cPos.z / (float)GridPointSize);
        return GridPoints[gx, gz];
    }



    /// <summary>
    /// We generate all grid points for the world
    /// Each is equally spaced
    /// </summary>
    public void GenerateInitialGridPoints()
    {
        for(int x=0; x<GridSize; x++)
        {
            for(int z=0; z<GridSize; z++)
            {
                 /* Vec2i cPos = new Vec2i(Mathf.Clamp(x * GridPointSize + (int)GenRan.GaussianFloat(0, GridPointSize/6), 0, World.WorldSize-1),
                                         Mathf.Clamp(z * GridPointSize + (int)GenRan.GaussianFloat(0, GridPointSize/6), 0, World.WorldSize - 1));*/
                Vec2i cPos = new Vec2i(x * GridPointSize, z * GridPointSize);
                GridPoints[x, z] = new GridPoint(new Vec2i(x,z), cPos);
                if (GameGen.TerGen.ChunkBases[cPos.x, cPos.z].Biome != ChunkBiome.ocean)
                    GridPoints[x, z].IsValid = true;
                else
                    GridPoints[x, z].IsValid = false;
            }
        }
        for (int x = 0; x < GridSize; x++)
        {
            for (int z = 0; z < GridSize; z++)
            {
                if(GridPoints[x,z] != null)
                {
                    //We iterate all nearest neighbors of this grid point
                    for(int i=0; i< WorldEventPathFinder.DIRS.Length; i++)
                    {
                        int x_ = x + WorldEventPathFinder.DIRS[i].x;
                        int z_ = z + WorldEventPathFinder.DIRS[i].z;
                        Vec2i p = new Vec2i(x_, z_);
                        //Check this neighbor is in bounds, and is a valid grid point
                        if (GridPlacement.InGridBounds(p) && GridPoints[p.x,p.z]!=null && GridPoints[p.x,p.z].IsValid)
                        {
                            GridPoints[x, z].NearestNeighbors.Add(p);
                        }

                    }

                    
                }
            }
        }
        CalculateEnclosedBiomes();
    }


    private void CalculateEnclosedBiomes()
    {
        for (int x = 0; x < GridSize; x++)
        {
            for (int z = 0; z < GridSize; z++)
            {
                if (GridPoints[x, z] != null)
                {
                    Vec2i cPos = GridPoints[x, z].ChunkPos;
                    List<ChunkBiome>[] enc = new List<ChunkBiome>[16];
                    for (int i=1; i<16; i++)
                    {
                        enc[i] = new List<ChunkBiome>();
                        //Add all previous 
                        if (i != 1)
                            enc[i].AddRange(enc[i - 1]);
                        for(int j=i-1; j<i; j++)
                        {
                            Vec2i c1 = cPos + new Vec2i(i - 1, j);
                            ChunkBiome b1 = GameGen.TerGen.ChunkBases[c1.x, c1.z].Biome;
                            Vec2i c2 = cPos + new Vec2i(j, i-1);
                            ChunkBiome b2 = GameGen.TerGen.ChunkBases[c2.x, c2.z].Biome;

                            if (!enc[i].Contains(b1))
                                enc[i].Add(b1);
                            if (!enc[i].Contains(b2))
                                enc[i].Add(b2);
                        }
                        
                    }
                    GridPoints[x, z].EnclosedBiomes = enc;
                }
            }
        }
    }

    /// <summary>
    /// Searches near nodes to find the closest that has a road on it.
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    public GridPoint GetNearestRoadPoint(GridPoint a, int searchRadius = 10)
    {

        for(int i=0; i<searchRadius; i++)
        {
            for(int j=0; j<WorldEventPathFinder.DIRS.Length; j++)
            {
                Vec2i v = a.GridPos + WorldEventPathFinder.DIRS[j] * i;
                if(InGridBounds(v) && GridPoints[v.x, v.z] != null)
                {
                    if (GridPoints[v.x, v.z].HasRoad)
                    {
                        Debug.Log(string.Format("{0} has road", v));
                        return GridPoints[v.x, v.z];

                    }
                }
            }
        }
        Debug.Log("no road");

        return null;
    }

    public List<Vec2i> ConnectPointsGridPoints(GridPoint a, GridPoint b) { 
        List<Vec2i> rawPath = GPF.GeneratePath(a.GridPos, b.GridPos);
        for(int i=0; i<rawPath.Count; i++)
        {
            rawPath[i] = rawPath[i] * GridPointSize;
        }
        return rawPath;
    }


    public List<GridPoint> ConnectPoints(GridPoint a, GridPoint b, bool roadSearch=true)
    {
        List<Vec2i> path = GPF.GeneratePath(a.GridPos, b.GridPos);
        List<GridPoint> points = new List<GridPoint>(path.Count);
        foreach(Vec2i v  in path)
        {
            points.Add(GridPoints[v.x, v.z]);
        }
        return points;

        /*
        List<GridPoint> points = new List<GridPoint>();




        //If we are not looking for roads, we create a raw connection
        if (!roadSearch)
        {
            Vec2i targetPos = b.ChunkPos;
            GridPoint current = a;
            points.Add(a);
            
            bool pathFound = false;
            int attempts = 100;
            int a_ = 0;
            while (!pathFound && a_<attempts)
            {
                a_++;
                int minDist = -1;
                GridPoint nearestGrid = null;
                foreach(Vec2i gv in current.NearestNeighbors)
                {
                    Vec2i v = GridPoints[gv.x, gv.z].ChunkPos;
                    int dist = v.QuickDistance(targetPos);

                

                    if (minDist == -1 || dist < minDist)
                    {
                        minDist = dist;
                        nearestGrid = GridPoints[gv.x, gv.z];
                    }
                }

                points.Add(nearestGrid);
                
                current = nearestGrid;
                if (current.ChunkPos == b.ChunkPos)
                    return points;

                if (a_ > attempts)
                    return points;
            }
            return points;
        }


        GridPoint roadA = GetNearestRoadPoint(a);
        GridPoint roadB = GetNearestRoadPoint(b);



        //If a road is found near to A, we create a path to it
        if(roadA != null && roadB != null)
        {
            Debug.Log("a");
            points.AddRange(ConnectPoints(a, roadA, false));
            points.AddRange(ConnectPoints(roadA, roadB, false));
            points.AddRange(ConnectPoints(roadB, b, false));
        }
        else if(roadA != null && roadB == null)
        {
            Debug.Log("b");
            points.AddRange(ConnectPoints(a, roadA, false));
            points.AddRange(ConnectPoints(roadA, b, false));

        }else if(roadA == null && roadB != null)
        {
            Debug.Log("c");
            points.AddRange(ConnectPoints(a, roadB, false));
            points.AddRange(ConnectPoints(roadB, b, false));

        }else if(roadA == null && roadB == null)
        {
            points.AddRange(ConnectPoints(a, b, false));
            Debug.Log("d");

        }



        return points;*/
    }

    public static bool InGridBounds(Vec2i v)
    {
        return v.x >= 0 && v.z >= 0 && v.x < GridSize && v.z < GridSize;
    }

}
public class GridPoint
{
    public Shell Shell;



    public SettlementType SETYPE;
    public bool HasSet;

    public TacLocType TACTYPE;
    public bool HasTacLoc;

    public float Desirability;
    public bool IsCapital = false;

    public bool NearRiver = false;

    public Vec2i GridPos;
    public Vec2i ChunkPos;
    public bool IsValid;
    public bool HasNonSettlementStructure;
    public List<Vec2i> NearestNeighbors;
    public List<int> ConnectedRoad;
    public List<ChunkBiome>[] EnclosedBiomes;

    public ChunkRoad ChunkRoad;
    public bool HasRoad { get { return ChunkRoad != null; } }

    public Kingdom Kingdom { get; private set; }


    public ChunkStructure ChunkStructure;
    public SettlementEconomy Economy;

    //public SettlementType SetType;
    public bool HasSettlement { get { return Shell != null && Shell is SettlementShell; } }
    public GridPoint(Vec2i gridPos, Vec2i cPos)
    {
        GridPos = gridPos;
        ChunkPos = cPos;
        NearestNeighbors = new List<Vec2i>();
        ConnectedRoad = new List<int>();
    }
}



public class GridPathFinder
{
    public volatile int COUNT;

    public static Object LockSafe;

    public static readonly Vec2i[] DIRS = new[] {
    new Vec2i(1, 0), // to right of tile
    new Vec2i(0, -1), // below tile
    new Vec2i(-1, 0), // to left of tile
    new Vec2i(0, 1), // above tile
    new Vec2i(1, 1), // diagonal top right
    new Vec2i(-1, 1), // diagonal top left
    new Vec2i(1, -1), // diagonal bottom right
    new Vec2i(-1, -1) // diagonal bottom left*/
  };


    private GridPlacement GridPlacement;
    public GridPathFinder(GridPlacement gp)
    {
        GridPlacement = gp;
    }

    public Dictionary<Vec2i, Vec2i> cameFrom = new Dictionary<Vec2i, Vec2i>(2000);
    public Dictionary<Vec2i, float> costSoFar = new Dictionary<Vec2i, float>(2000);
    public Vec2i Target { get; private set; }
    private Vec2i start;

    public List<Vec2i> GeneratePath(Vec2i start, Vec2i end, bool debug = false)
    {
        if (debug)
            Debug.Log("[PathFinder] Finding normal path from " + start + " to " + end);

        cameFrom.Clear();
        costSoFar.Clear();
        var frontier = new PriorityQueue<Vec2i>();
        Target = end;
        this.start = start;
        frontier.Enqueue(start, 0f);

        cameFrom.Add(start, start); // is set to start, None in example
        costSoFar.Add(start, 0f);

        while (frontier.Count > 0f)
        {

            // Get the Location from the frontier that has the lowest
            // priority, then remove that Location from the frontier
            Vec2i current = frontier.Dequeue();

            // If we're at the goal Location, stop looking.
            if (current.Equals(Target))
            {
                break;
            }
            // Neighbors will return a List of valid tile Locations
            // that are next to, diagonal to, above or below current
            foreach (var neighbor in Neighbors(current))
            {

                // If neighbor is diagonal to current, graph.Cost(current,neighbor)
                // will return Sqrt(2). Otherwise it will return only the cost of
                // the neighbor, which depends on its type, as set in the TileType enum.
                // So if this is a normal floor tile (1) and it's neighbor is an
                // adjacent (not diagonal) floor tile (1), newCost will be 2,
                // or if the neighbor is diagonal, 1+Sqrt(2). And that will be the
                // value assigned to costSoFar[neighbor] below.
                float newCost = costSoFar[current] + Cost(current, neighbor);

                // If there's no cost assigned to the neighbor yet, or if the new
                // cost is lower than the assigned one, add newCost for this neighbor
                if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
                {

                    // If we're replacing the previous cost, remove it
                    if (costSoFar.ContainsKey(neighbor))
                    {
                        costSoFar.Remove(neighbor);
                        cameFrom.Remove(neighbor);
                    }

                    costSoFar.Add(neighbor, newCost);
                    cameFrom.Add(neighbor, current);
                    float priority = newCost + Heuristic(neighbor, Target);
                    frontier.Enqueue(neighbor, priority);
                }
            }
        }
        return FindPath();
    }


    private List<Vec2i> FindPath()
    {

        List<Vec2i> path = new List<Vec2i>(1000);
        Vec2i current = Target;

        while (!current.Equals(start))
        {
            if (!cameFrom.ContainsKey(current))
            {
                return new List<Vec2i>(100);
            }
            path.Add(current);
            current = cameFrom[current];
        }
        path.Reverse();
        return path;
    }

    static public float Heuristic(Vec2i a, Vec2i b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);
    }
    // Check the tiles that are next to, above, below, or diagonal to
    // this tile, and return them if they're within the game bounds and passable
    public IEnumerable<Vec2i> Neighbors(Vec2i id)
    {
        foreach (var dir in DIRS)
        {
            Vec2i next = new Vec2i(id.x + dir.x, id.z + dir.z);
            if (InBounds(next) && Passable(next))
            {
                yield return next;
            }
        }
    }

    float NodeValue(Vec2i v)
    {
        GridPoint gp = GridPlacement.GridPoints[v.x, v.z];

        if (gp == null)
        {

            return Mathf.Infinity;
        }
            
        if (gp.HasRoad)
        {
            if(gp.ChunkRoad.Type == ChunkRoad.RoadType.Dirt)
            {
                return 150;
            }
            else
            {
                return 0;
            }
        }
        return 50000;

    }


    public bool InBounds(Vec2i v)
    {
        return GridPlacement.InGridBounds(v);

    }

    // Everything that isn't a Wall is Passable
    public bool Passable(Vec2i id)
    {
        return (int)NodeValue(id) < System.Int32.MaxValue;
    }

    public float Cost(Vec2i a, Vec2i b)
    {
        if (Heuristic(a, b) == 2f)
        {
            return (float)NodeValue(b) * 2;
        }
        return (float)NodeValue(b);
    }


}