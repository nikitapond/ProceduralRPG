using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class RiverGenerator2
{
    public volatile int COUNT;

    public static Object LockSafe;
    public static readonly int SCALE = 4;
    public static readonly Vec2i[] DIRS = new[] {
    new Vec2i(SCALE, 0), // to right of tile
    new Vec2i(0, -SCALE), // below tile
    new Vec2i(-SCALE, 0), // to left of tile
    new Vec2i(0, SCALE), // above tile
    new Vec2i(SCALE, SCALE), // diagonal top right
    new Vec2i(-SCALE, SCALE), // diagonal top left
    new Vec2i(SCALE, -SCALE), // diagonal bottom right
    new Vec2i(-SCALE, -SCALE) // diagonal bottom left
  };


    public float GetValAtChunk(Vec2i v)
    {
        int lx = Mathf.FloorToInt((float)v.x / SCALE);
        int lz = Mathf.FloorToInt((float)v.z / SCALE);
        return CostMap[lx, lz];
    }


    private GameGenerator2 GameGen;
    private GenerationRandom GenRan;

    private float[,] CostMap;



    public RiverGenerator2(GameGenerator2 gameGen)
    {
        GameGen = gameGen;
        GenRan = new GenerationRandom(gameGen.Seed);
        GeneratePathfindingMap(gameGen.TerGen.ChunkBases);
    }

   


    public void GenerateRivers(int count)
    {
        for(int i=0; i<count; i++)
        {
            int x = (int)GenRan.GaussianFloat(World.WorldSize / 2, World.WorldSize / 8);
            int z = (int)GenRan.GaussianFloat(World.WorldSize / 2, World.WorldSize / 8);
            GenerateRiver(new Vec2i(x, z));
        }
    }

    private void GenerateRiver(Vec2i start)
    {

        Vec2i end = null;
        for(int i=1; i<100; i++)
        {
            if (end != null)
                break;
            for (int j = 0; j < DIRS.Length; j++)
            {
                Vec2i pos = start + DIRS[j] * i;
                if (GameGenerator2.InBounds(pos))
                {
                    if (GameGen.TerGen.ChunkBases[pos.x, pos.z].Biome == ChunkBiome.ocean)
                    {
                        end = pos;
                        break;
                    }
                }
            }
        }
        if (end == null)
            return;






        LineI li = new LineI(start, end);


        List<Vec2i> basic = li.ConnectPoints();

        float val1 = GenRan.Random(0.1f, 0.001f);
        float val2 = GenRan.Random(0.1f, 0.001f);
        int val3 = GenRan.RandomInt(8, 32);
        int val4 = GenRan.RandomInt(8, 32);
        for (int i=0; i<basic.Count; i++)
        {
            basic[i] += new Vec2i((int)(val3 * Mathf.Sin(i * val1)), (int)(val4 * Mathf.Sin(i * val2)));
        }
        for(int i=0; i<basic.Count-1; i++)
        {
            Vec2i v1 = basic[i];
            Vec2i v2 = basic[i + 1];
            LineI i2 = new LineI(v1, v2);
            foreach(Vec2i v in i2.ConnectPoints())
            {
                GameGen.TerGen.ChunkBases[v.x, v.z].SetChunkFeature(new ChunkRiverNode(v));

            }
        }
        return;
        
        

        
    }

    /// <summary>
    /// Turns the array of chunk bases into a cost map
    /// </summary>
    private void GeneratePathfindingMap(ChunkBase2[,] map)
    {
        CostMap = new float[map.GetLength(0), map.GetLength(1)];

        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int z = 0; z < map.GetLength(1); z++)
            {
                CostMap[x, z] = Mathf.Pow(map[x, z].Height, 1);
                //Debug.Log(string.Format("x: {0}, z: {1}, cost: {2}",x,z,CostMap[x,z]));
        
            }
        }
    }

    public Dictionary<Vec2i, Vec2i> cameFrom = new Dictionary<Vec2i, Vec2i>(2000);
    public Dictionary<Vec2i, float> costSoFar = new Dictionary<Vec2i, float>(2000);
    public Vec2i Target { get; private set; }
    private Vec2i start;

    public List<Vec2i> GeneratePath(Vec2i start, Vec2i end, bool debug = false)
    {
        

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
        return Mathf.Abs(a.x - b.x)/SCALE + Mathf.Abs(a.z - b.z)/ SCALE;
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
        return CostMap[v.z, v.z];
    }


    public bool InBounds(Vec2i v)
    {
        return v.x >= 0 && v.z >= 0 && v.x < CostMap.GetLength(0) && v.z < CostMap.GetLength(1);

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
