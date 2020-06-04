using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Threading;
/// <summary>
/// Entity path finder is an object that allows us to run thread safe 
/// path finding. We keep a series of EntityPathFinder objects in a cache,
/// only loading in data for chunk that are relevent
/// </summary>
[System.Serializable]
public class EntityPathFinder
{



    private static readonly Vec2i[] DIRS = new[] {
    new Vec2i(1, 0), // to right of tile
    new Vec2i(0, -1), // below tile
    new Vec2i(-1, 0), // to left of tile
    new Vec2i(0, 1), // above tile
    /*new Vec2i(1, 1), // diagonal top right
    new Vec2i(-1, 1), // diagonal top left
    new Vec2i(1, -1), // diagonal bottom right
    new Vec2i(-1, -1) // diagonal bottom left*/
  };
    private Object LockSafe;
    private PathFinder Parent;

    private Thread WorkThread;

    private List<Vec2i> CurrentPath;

    private Dictionary<Vec2i, float[,]> ChunkPathVals;
    public bool IsRunning { get; private set; }
    public volatile bool IsComplete_;

    public bool IsComplete()
    {
        return IsComplete_;
    }
    public bool FoundPath { get; private set; }

    private volatile bool ForceStop_;

    public Dictionary<Vec2i, Vec2i> cameFrom = new Dictionary<Vec2i, Vec2i>(2000);
    public Dictionary<Vec2i, float> costSoFar = new Dictionary<Vec2i, float>(2000);

    private Vec2i start;
    public Vec2i Target { get; private set; }
    private bool NewTarget;

    public EntityPathFinder(PathFinder parent)
    {
        ChunkPathVals = new Dictionary<Vec2i, float[,]>();
        Parent = parent;
        if(parent == null)
        {
            Debug.Log("Parent is null??");
        }
    }

    public void FindPath(Vec2i start, Vec2i end)
    {
        WorkThread = new Thread(() => InternalFindPath(start, end));
        WorkThread.Start();
    }

    public void ForceStop()
    {
        ForceStop_ = true;
    }


    public void SetNewTarget(Vec2i target)
    {
        NewTarget = true;
        Target = target;
    }

    private void InternalFindPath(Vec2i start, Vec2i end, bool debug=false)
    {
        LockSafe = PathFinder.LockSafe;
        IsRunning = true;
        this.start = start;
        this.Target = end;
        if (start == end)
        {
            CurrentPath = new List<Vec2i>();
            CurrentPath.Add(start);
            Finished(CurrentPath, true);
            return;
        }

        //TODO - FIX THIS???
        cameFrom.Clear();
        costSoFar.Clear();



        List<Vec2i> wholePath = new List<Vec2i>();

        if (debug)
        {
            Debug.Log("[PathFinder] Attempting to find path from " + start + " to " + end);
        }


        if(Vec2i.QuickDistance(start, end) < World.ChunkSize * World.ChunkSize * 2)
        {
            List<Vec2i> p = GenerateNormalPath(start, end, debug);
            if(p != null && p.Count > 0)
            {
                Finished(p, true);
                return;
            }
        }

        //To include settlement path finding, we must check the start and end points to see if they are in settlements
        ChunkData startChunk = GameManager.WorldManager.CRManager.GetChunk(World.GetChunkPosition(start));

        if (startChunk == null)
        {
            if (debug)
                Debug.Log("[pathFinder] Start chunk is null");
            Finished(null, false);
            return;
        }



        Settlement startSet = startChunk.GetSettlement();

        //To include settlement path finding, we must check the start and end points to see if they are in settlements
        ChunkData endChunk=null;
        try
        {
            endChunk = GameManager.WorldManager.CRManager.GetChunk(World.GetChunkPosition(end));
        }
        catch
        {
            Finished(null, false);
        }

        if (endChunk == null)
        {
            if (debug)
                Debug.Log("[pathFinder] End chunk is null");
            Finished(null, false);

            return;
        }
        Settlement endSet = startChunk.GetSettlement();

        //if neither start nor end are in a settlement, we generate a normal path
        if (startSet == null && endSet == null)
        {
            if (debug)
                Debug.Log("[PathFinder] Attempting to find normal path");
            CurrentPath = GenerateNormalPath(start, end);
            Finished(CurrentPath, !(CurrentPath == null || CurrentPath.Count == 0));

            return;
        }

        //If we start in a settlement
        if (startSet != null)
        {

            //We first walk to the nearest node inside the settlement
            Vec2i spnStart = startSet.SettlementPathFinder.GetNearestNode(start);
            Vec2i startNode = spnStart * World.ChunkSize + startSet.BaseCoord;
            if (debug)
            {
                Debug.Log("[PathFiner] Nearest node has local coords: " + spnStart + " at pos " + startNode );
                Debug.Log("[PathFinder] Attmpting to find path from " + start + " to SPN - " + (spnStart * World.ChunkSize + startSet.BaseCoord));

            }

            List<Vec2i> path1 = GenerateNormalPath(start, startNode, debug);
            if (path1 == null)
            {
                if (debug)
                    Debug.Log("[PathFinder] Path to settlement not found");
    
                Finished(wholePath, false);
                return;
            }
            if (debug)
                Debug.Log("[PathFinder] Initial path of length " + path1.Count + " found");
            wholePath.AddRange(path1);

            //We find the end node
            Vec2i spnEnd = startSet.SettlementPathFinder.GetNearestNode(end);

            if (debug)
                Debug.Log("[PathFinder] Finding settlement path from node " + spnStart + " to " + spnEnd);
            //And generate the path to it
            List<Vec2i> settlementPath;
            lock (LockSafe) {



                settlementPath  = startSet.SettlementPathFinder.ConnectNodes(spnStart, spnEnd);
            }
            if (settlementPath == null)
            {
                if (debug)
                    Debug.Log("[PathFinder]No path between settlement points found");
                Finished(wholePath, false);
                return;
            }else if (debug)
            {
                Debug.Log("[PathFinder] Path from settlement node " + spnStart + " to " + spnEnd + " found of length " + settlementPath.Count);
            }

            wholePath.AddRange(settlementPath);
            Vec2i endNode = spnEnd * World.ChunkSize + startSet.BaseCoord;
            List<Vec2i> path3 = GenerateNormalPath(endNode, end, debug);
            if (path3 == null)
            {
                if (debug)
                    Debug.Log("[PathFinder] Settlement end node to end point not found");
                Finished(wholePath, false);

                return;
            }
            if (debug)
            {
                Debug.Log("[PathFinder] Path from setNode at "+ endNode + " to " + end + " found of length " + wholePath.Count);
            }
            wholePath.AddRange(path3);
            Finished(wholePath, true);
            return;
        }
        else
        {
            //If the start point is not in a settlement, we do the same, but with the end settlement

            //We first walk to the nearest node inside the settlement
            Vec2i spnStart = endSet.SettlementPathFinder.GetNearestNode(start);
            List<Vec2i> path1 = GenerateNormalPath(start, spnStart * World.ChunkSize + endSet.BaseCoord);
            if (path1 == null)
            {
                Finished(path1, false);
                return;
            }
            if (ForceStop_)
            {
                Finished(null, false);
                return;
            }
                
            wholePath.AddRange(path1);

            //We find the end node
            Vec2i spnEnd = endSet.SettlementPathFinder.GetNearestNode(end);
            //And generate the path to it
            List<Vec2i> settlementPath = endSet.SettlementPathFinder.ConnectNodes(spnStart, spnEnd);
            if (settlementPath == null)
            {
                Finished(wholePath, false);

                return;
            }
            if (ForceStop_)
            {
                Finished(null, false);
                return;
            }

            wholePath.AddRange(settlementPath);

            List<Vec2i> path3 = GenerateNormalPath(spnEnd * World.ChunkSize + endSet.BaseCoord, end, debug);
            if (path3 == null)
            {
                Finished(wholePath, false);
                return;
            }
            wholePath.AddRange(path3);

            Finished(wholePath, true);
            return;
        }

        List<Vec2i> path = GenerateNormalPath(start, end);

        Finished(path, !(CurrentPath == null || CurrentPath.Count == 0));




    }

    public List<Vec2i> GetPath()
    {
        if (!IsComplete_)
            return null;
        return CurrentPath;
    }

    private void Finished(List<Vec2i> finalPath, bool foundPath)
    {
        CurrentPath = finalPath;
        ForceStop_ = false;
        IsRunning = false;
        IsComplete_ = true;
        FoundPath = foundPath;
    }


    public List<Vec2i> GenerateNormalPath(Vec2i start, Vec2i end, bool debug = false)
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
            if (ForceStop_)
                return null;
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

    float NodeValue(Vec2i v)
    {
        Vec2i cPos = World.GetChunkPosition(v);
        float[,] val = null;
        if(!ChunkPathVals.TryGetValue(cPos, out val)){
            val = LoadChunk(cPos);
            if(val == null)
            {
                Debug.Log("[PathFinder] Could not load chunk " + cPos);
                return Mathf.Infinity;
            }
            ChunkPathVals.Add(cPos, val);
            
        }
        return val[v.x % World.ChunkSize, v.z % World.ChunkSize];
        try
        {
            return val[v.x % World.ChunkSize, v.z % World.ChunkSize];
        }catch(System.Exception e)
        {
            int rowLength = val.GetLength(0);
            int colLength = val.GetLength(1);
            string final = "";
            for (int i = 0; i < rowLength; i++)
            {
                for (int j = 0; j < colLength; j++)
                {
                    final += val[i, j] + " ";
                }
                final += "\n";
            }
            Debug.Log(final);
            Debug.Log(v + "_" + v.x % World.ChunkSize + "_" + v.z % World.ChunkSize + "_" + rowLength + "_" + colLength);
        }
        return Mathf.Infinity;
    }

    float[,] LoadChunk(Vec2i cPos)
    {
        return Parent.LoadChunk(cPos);
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

    public bool InBounds(Vec2i v)
    {
        
        return Parent.Bounds.ContainsPoint(v);
        return v.x > 0 && v.x < World.WorldSize * World.ChunkSize && v.z > 0 && v.z < World.WorldSize * World.ChunkSize;
        //return (x <= v.x) && (v.x < width) && (y <= id.y) && (id.y < height);
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
            return (float)NodeValue(b) * Mathf.Sqrt(2f);
        }
        return (float)NodeValue(b);
    }

}
/// <summary>
/// Class holds onto the 
/// </summary>
[System.Serializable]
public class PathFinder
{

    public volatile int COUNT;

    public static Object LockSafe;

    public static readonly int RegionTileSize = World.RegionSize * World.ChunkSize;
    public static readonly Vec2i[] DIRS = new[] {
    new Vec2i(1, 0), // to right of tile
    new Vec2i(0, -1), // below tile
    new Vec2i(-1, 0), // to left of tile
    new Vec2i(0, 1), // above tile
    new Vec2i(1, 1), // diagonal top right
    new Vec2i(-1, 1), // diagonal top left
    new Vec2i(1, -1), // diagonal bottom right
    new Vec2i(-1, -1) // diagonal bottom left
  };
    public static int PathFinderSize = World.ChunkSize * (WorldManager.LoadChunkRadius + 1);
    public static int WorldSize = World.ChunkSize * World.WorldSize;
    //private float[,] TileValues;
    private Dictionary<Vec2i, float[,]> RegionTileValues;

    private float[,] CurrentSubworldTiles;
    private Subworld Subworld;
    private Vec2i SubworldSize;
    public Recti Bounds { get; private set; }

    public World World { get; private set; }
    public PathFinder(World world)
    {
        //TileValues = new float[PathFinderSize, PathFinderSize];
        World = world;
        RegionTileValues = new Dictionary<Vec2i, float[,]>();
        LockSafe = new Object();
    }


    public void SetPlayerPosition(Vec2i pos)
    {
        int minX = pos.x - PathFinderSize;
        if (minX < 0)
            minX = 0;

        int minZ = pos.z - PathFinderSize;
        if (minZ < 0)
            minZ = 0;
        /*
        int maxX = pos.x + PathFinderSize;
        if (maxX >= WorldSize)
            maxX = WorldSize-1;

        int maxZ = pos.z + PathFinderSize;
        if (maxZ >= WorldSize)
            maxZ = WorldSize - 1;
            */
        Bounds = new Recti(minX, minZ, 2 * PathFinderSize, 2 * PathFinderSize); 
    }

    public float[,] LoadChunk(Vec2i cPos)
    {
        lock (LockSafe)
        {
            Vec2i rPos = World.GetRegionCoordFromChunkCoord(cPos);
            float[,] region;
            if (RegionTileValues.TryGetValue(rPos, out region))
            {
                float[,] chunk = new float[World.ChunkSize, World.ChunkSize];

                Vec2i offset = new Vec2i(cPos.x % World.RegionSize, cPos.z % World.RegionSize) * World.ChunkSize;
                for (int x = 0; x < World.ChunkSize; x++)
                {
                    for (int z = 0; z < World.ChunkSize; z++)
                    {
                        chunk[x, z] = region[x + offset.x, z + offset.z];
                    }
                }
                COUNT++;
                return chunk;
            }
            return null;
        }
    }

    /// <summary>
    /// Generates a map of moveable tiles based on the given region
    /// </summary>
    /// <param name="chunkRegion"></param>
    public void LoadRegion(ChunkRegion chunkRegion)
    {
        Debug.Log("[PathFinding] Chunk Region " + chunkRegion.X + "," + chunkRegion.Z + " loading");
        //Get region position & generate empty array
        Vec2i rPos = new Vec2i(chunkRegion.X, chunkRegion.Z);
        float[,] tileData = new float[RegionTileSize, RegionTileSize];
        //Iterate whole region and fill array as required
        //TODO - see if there is a quicker way to do this...
        for (int cx = 0; cx < World.RegionSize; cx++)
        {
            for (int cz = 0; cz < World.RegionSize; cz++)
            {
                for (int x = 0; x < World.ChunkSize; x++)
                {
                    for (int z = 0; z < World.ChunkSize; z++)
                    {
                        ChunkData c = chunkRegion.Chunks[cx, cz];
                        if (c == null)
                        {
                            Debug.Error("Chunk Region " + chunkRegion.ToString() + " has null chunk with local: " + cx + "," + cz);
                            continue;
                        }
                        float val = tileData[cx * World.ChunkSize + x, cz * World.ChunkSize + z] = c.GetTile(x, z).SpeedMultiplier;

                        //TODO - update this bad boy.

                        /*
                        if (c.Objects != null && c.GetObject(x, z) != null)
                        {
                            WorldObjectData wod = c.GetObject(x, z);
                            if (wod.IsCollision)
                                val = Mathf.Infinity;
                        }*/

                        tileData[cx * World.ChunkSize + x, cz * World.ChunkSize + z] = val;
                    }
                }
            }
        }
        RegionTileValues.Add(rPos, tileData);
    }

    public void LoadSubworld(Subworld subworld)
    {
  
    }
    public void LeaveSubworld()
    {
        CurrentSubworldTiles = null;
        Subworld = null;
        SubworldSize = null;
    }

    public void UnloadRegion(Vec2i v)
    {
        if (RegionTileValues.ContainsKey(v))
        {
            RegionTileValues.Remove(v);
        }
    }

}
