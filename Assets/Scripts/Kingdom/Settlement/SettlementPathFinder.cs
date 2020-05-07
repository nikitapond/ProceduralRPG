using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
[System.Serializable]
public class SettlementPathFinder
{

    public Vec2i BaseCoord { get; private set; }
    public float[,] PathNodes;
    private int Size;
    public SettlementPathFinder(Vec2i settlementBaseCoord, float[,] settlementPathNodes)
    {
        BaseCoord = settlementBaseCoord;
        PathNodes = settlementPathNodes;
        Size = PathNodes.GetLength(0)-1;

       // Debug.Log(Size);
    }

    /// <summary>
    /// Returns the (local) node position that is nearest to this position
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <returns></returns>
    public Vec2i GetNearestNode(Vec2i worldPosition)
    {
        //TODO - make this quicker than searching all points

        Vec2i localWorldPosition = worldPosition - BaseCoord;
        int localX = (int)((float)localWorldPosition.x / World.ChunkSize);
        int localZ = (int)((float)localWorldPosition.z / World.ChunkSize);

        if(InBounds(new Vec2i(localX, localZ)) && PathNodes[localX, localZ] > 0)
        {
            return new Vec2i(localX, localZ);
        }
        int minDistance = -1;
        Vec2i nearestNode = null;
        for(int x=0; x<Size; x++)
        {
            for (int z = 0; z < Size; z++)
            {
                int nodePosX = (x) * World.ChunkSize;
                int nodePosZ = (z) * World.ChunkSize;
                if(PathNodes[x, z] > 0)
                {
                    int dist = QuickDistance(nodePosX, nodePosZ, localWorldPosition);
                    if(minDistance == -1 || dist < minDistance)
                    {
                        minDistance = dist;
                        nearestNode = new Vec2i(x, z);
                    }
                }
            }
        }
        return nearestNode;


       
    }

    public Dictionary<Vec2i, Vec2i> cameFrom = new Dictionary<Vec2i, Vec2i>(2000);
    public Dictionary<Vec2i, float> costSoFar = new Dictionary<Vec2i, float>(2000);

    private Vec2i start;
    private Vec2i goal;

    static public float Heuristic(Vec2i a, Vec2i b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);
    }

    /// <summary>
    /// <para>
    /// Finds a path connecting the two nodes specified.
    /// The values 'start'and 'end' represent the local coordinates of the 
    /// nodes to path find between. Returns 'true' if a total path was found</para>
    /// <para>
    /// The outputted path is held in the out variable 'path'. If no path is found, this will 
    /// instead contain all checked points.
    /// </para>
    /// <para>The paramatater 'transform', true as default, causes the result to be scaled and 
    /// shifted to world coordinates ([x,z]*World.ChunkSize + BaseCoord).
    /// For path finding, this should be kept true.</para>
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="path"></param>
    /// <param name="transform"></param>
    /// <returns></returns>
    public bool ConnectNodes(Vec2i start, Vec2i end, out List<Vec2i> path, out float cost, bool transform = true)
    {
        cameFrom.Clear();
        costSoFar.Clear();
        var frontier = new PriorityQueue<Vec2i>();
        goal = end;
        this.start = start;
        frontier.Enqueue(start, 0f);

        cameFrom.Add(start, start); // is set to start, None in example
        costSoFar.Add(start, 0f);
        List<Vec2i> allPath = new List<Vec2i>(100);
        List<Vec2i> validPath = new List<Vec2i>();
        allPath.Add(start);
        validPath.Add(start);
        while (frontier.Count > 0f)
        {

            // Get the Location from the frontier that has the lowest
            // priority, then remove that Location from the frontier
            Vec2i current = frontier.Dequeue();

          //  float tCost = 0;
           // if(costSoFar.TryGetValue(current, out tCost))
            //{
                //if (tCost >= int.MaxValue)
                //    break;
            //}
            // If we're at the goal Location, stop looking.
            if (current.Equals(goal))
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
                if(newCost < int.MaxValue)
                {
                    validPath.Add(neighbor);
                }
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
                    float priority = newCost + Heuristic(neighbor, goal);
                    frontier.Enqueue(neighbor, priority);
                    allPath.Add(neighbor);
                }
                
            }
        }




        if(costSoFar.TryGetValue(end, out cost))
        {
            if (cost < int.MaxValue)
            {
                if (GameManager.DEBUG)
                {
                    Debug.Log("[SettlementPathFinding] Path cost of " + cost);
                }
                if (transform)
                    path = TransformPath(FindPath());
                else
                    path = FindPath();
                return true;
            }
            else
            {
                if (GameManager.DEBUG)
                {
                    Debug.Log("[SettlementPathFinding] Path cost of " + cost + " - not valid path");
                }
                

            }
            

        }
        if (transform)
            path = TransformPath(FindPath());
        else
            path = FindPath();
        //If we need to transform, we transform the path accordingly
        return false;
        //Check if the path has worked (is this valid?)
        if (cameFrom.ContainsKey(end) && cameFrom.ContainsKey(start))
        {
            if (transform)            
                path = TransformPath(FindPath());            
            else
                path = FindPath();
            return true;
        }

        if (transform)
            path = TransformPath(allPath);
        else
            path = allPath;
        //If we need to transform, we transform the path accordingly
        return false;
    }
    /// <summary>
    /// <para>
    /// Finds a path connecting the two nodes specified.
    /// The values 'start'and 'end' represent the local coordinates of the 
    /// nodes to path find between.</para>
    /// <para>
    /// The paramatater 'transform', true as default, causes the result to be scaled and 
    /// shifted to world coordinates ([x,z]*World.ChunkSize + BaseCoord).
    /// For path finding, this should be kept true.</para>
    /// <para>
    /// If 'transform' is false, the returned result will be in local PathNode coordinates.
    /// This is used in SettlementBuilder to help remove islands
    /// </para>
    /// 
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="scale"></param>
    /// <returns></returns>
    public List<Vec2i> ConnectNodes(Vec2i start, Vec2i end, bool transform=true)
    {
        cameFrom.Clear();
        costSoFar.Clear();
        var frontier = new PriorityQueue<Vec2i>();
        goal = end;
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
            if (current.Equals(goal))
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
                if (GameManager.DEBUG)
                    Debug.Log("[SettlementPathFinder] Cost for " + current + " to " + neighbor + " is " + Cost(current, neighbor) + " with total cost " + newCost);
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
                    float priority = newCost + Heuristic(neighbor, goal);
                    frontier.Enqueue(neighbor, priority);
                }
            }
        }
        //If we need to transform, we transform the path accordingly
        if (transform)
            return TransformPath(FindPath());
        return FindPath();
    }

    public List<Vec2i> TransformPath(List<Vec2i> initial)
    {
        if (initial == null)
            return null;
        for(int i=0; i<initial.Count; i++)
        {
            initial[i] = initial[i] * World.ChunkSize + BaseCoord;
        }
        return initial;
    }

    public List<Vec2i> FindPath()
    {

        List<Vec2i> path = new List<Vec2i>(1000);
        Vec2i current = goal;

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

    public static readonly Vec2i[] DIRS = new[] {
    new Vec2i(1, 0), // to right of tile
    new Vec2i(0, -1), // below tile
    new Vec2i(-1, 0), // to left of tile
    new Vec2i(0, 1), // above tile
    /*new Vec2i(1, 1), // diagonal top right
    new Vec2i(-1, 1), // diagonal top left
    new Vec2i(1, -1), // diagonal bottom right
    new Vec2i(-1, -1) // diagonal bottom left*/
  };


    // Everything that isn't a Wall is Passable
    public bool Passable(Vec2i id)
    {
        return (int)NodeValue(id) < 500;
    }


    private float NodeValue(Vec2i id)
    {
        if (PathNodes[id.x, id.z] < 1)
            return int.MaxValue;
        if (PathNodes[id.x, id.z] == 0)
            return 1000;
        
        return 200 - PathNodes[id.x, id.z];
    }

    // If the heuristic = 2f, the movement is diagonal
    public float Cost(Vec2i a, Vec2i b)
    {
        if (Heuristic(a, b) == 2f)
        {
            return (float)NodeValue(b) * Mathf.Sqrt(2f);
        }
        return (float)NodeValue(b);
    }
    public bool InBounds(Vec2i v)
    {

        return v.x >= 0 && v.z>=0 && v.x<Size && v.z < Size;
        //return (x <= v.x) && (v.x < width) && (y <= id.y) && (id.y < height);
    }


    private int QuickDistance(int localNodeX, int localNodeZ, Vec2i localWorldPosition)
    {
        return (localNodeX - localWorldPosition.x) * (localNodeX - localWorldPosition.x) + (localNodeZ - localWorldPosition.z) * (localNodeZ - localWorldPosition.z);
    }



    public List<Vec2i> GenerateSettlementPath(SettlementPathNode start, SettlementPathNode end, bool debug=false)
    {
        List<SettlementPathNode> result;
        if (SettlementPath(start, end, out result, debug:debug))
        {
            List<Vec2i> outRes = new List<Vec2i>(result.Count);
            foreach(SettlementPathNode spn in result)
            {
                outRes.Add(spn.Position + BaseCoord);
            }
            return outRes;
        }
        return null;
    }



    /// <summary>
    /// <para>Finds a path connecting the start and end node.
    /// Each node contains all nodes they are connected to.
    /// Returns true if a full path was found.
    /// Returns false if no path was found</para>
    /// the out path variable is the total path if one is found, or all total nodes checked.
    /// 
    /// 
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="path"></param>
    /// <param name="maxTest"></param>
    /// <param name="debug"></param>
    /// <returns></returns>
    public static bool SettlementPath(SettlementPathNode start, SettlementPathNode end, out List<SettlementPathNode> path, int maxTest = 500, bool debug=false)
    {


        List<SettlementPathNode> result = new List<SettlementPathNode>();

        List<SettlementPathNode> tested = new List<SettlementPathNode>(32);

        if (debug)
        {
            Debug.Log("[SetPathFind] Attempting path: " + start + " -> " + end);
        }

        bool found = false;
        SettlementPathNode current = start;
        result.Add(current);
        int index = 0;
        while (!found)
        {
            index++;

            if(index > maxTest)
            {
                path = result;
                return false;
            }
            int[] connectedVals = new int[4];
            int minVal = -1;
            int minNode = -1;
            if (debug)
            {
                Debug.Log("[SetPathFind] Current node: " + current.Position);
            }
            //Iterate 
            for (int i=0; i<4; i++)
            {

                if (debug)
                {
                    Debug.Log("[SetPathFind] Checking node: " + i);
                    if(current.Connected[i] != null)
                    {
                        Debug.Log("[SetPathFind] Not null: " + current.Connected[i]);

                    }
                }

                //If the node is null, then the value is max
               
                if (current.Connected[i] == null)
                {

                    if(debug)
                        Debug.Log("[SetPathFind] Node null -> inf val");
                    connectedVals[i] = int.MaxValue;

                }
                //If the connected node is our final node, we finish the algorithm
                else if(current.Connected[i].Position == end.Position)
                {
                    if (debug)
                        Debug.Log("[SetPathFind] Connected node is end");

                    result.Add(current.Connected[i]);
                    path = result;
                    return true;
                }
                else if(current.Connected[i].ConnectedCount < 2)
                { //If the connected node has less than two connections, it is an end node, and so goes no where
                    //We can set the connected value as high
                    if (debug)
                        Debug.Log("[SetPathFind] Node " + current.Connected[i] + " has few connections");
                    connectedVals[i] = int.MaxValue;
                }else if (tested.Contains(current.Connected[i]))
                {
                    if (debug)
                        Debug.Log("[SetPathFind] Node " + current.Connected[i] + "Alreaded tested");
                    //if we have already tested a discarded a point, max val
                    connectedVals[i] = int.MaxValue;
                }else if(result.Count > 1 && current.Connected[i] == result[result.Count - 1])
                {
                    if (debug)
                        Debug.Log("[SetPathFind] Node " + current.Connected[i] + " is previous point");
                    //If the point is the previous point, max val
                    connectedVals[i] = int.MaxValue;

                }else if(result.Count > 1 && result.Contains(current.Connected[i]))
                {
                    if (debug)
                        Debug.Log("[SetPathFind] Node " + current.Connected[i] + " is circle");
                    connectedVals[i] = int.MaxValue;

                }
                else 
                {

                    connectedVals[i] = NodeValue(current.Connected[i], end);
                    if (debug)
                        Debug.Log("[SetPathFind] Node " + current.Connected[i] + " value of " + connectedVals[i]);
                }
                if (minVal == -1)
                {
                    minVal = connectedVals[i];
                    minNode = i;
                    if (debug)
                    {
                        Debug.Log("[SetPathFind] Setting min val to " + minVal);
                    }
                    
                }
                else
                {
                    if(connectedVals[i] < minVal)
                    {
                        minVal = connectedVals[i];

                        minNode = i;
                        if (debug)
                        {
                            Debug.Log("[SetPathFind] Setting min val to " + minVal);
                    }
                    }
                }
            }

            //This means that no possible forward direction could be taken
            if(minVal > 1000000000)
            {
                //We take a step back              
                
                if(result.Count == 1)
                {
                    if (debug)
                    {
                        Debug.Log("[SetPathFind] No possible path");
                    }
                    //If we find no valid direction on the first node,
                    //then there is no path
                    path = tested;
                    return false;
                }
                if (debug)
                {
                    Debug.Log("[SetPathFind] Step back ");
                }
                //Ensure we don't check this one again
                tested.Add(current);
                //Move back 1 space
                current = result[result.Count - 1];
                //Remove from result
                result.RemoveAt(result.Count - 1);

            }
            else
            {
                if (debug)
                {
                    Debug.Log("[SetPathFind] Next node: " + current.Connected[minNode]);
                }
                result.Add(current.Connected[minNode]);
                current = current.Connected[minNode];
            }

        }
        path = tested;
        return false;

    }

    private static int NodeValue(SettlementPathNode node, SettlementPathNode end)
    {
        Vec2i disp = node.Position - end.Position;
        return disp.x * disp.x + disp.z * disp.z;
    }

    

}
[System.Serializable]
public class SettlementPathNode
{

    public static Vec2i[] DIRECTIONS = new Vec2i[] {new Vec2i(0,1), new Vec2i(1,0), new Vec2i(0,-1), new Vec2i(-1,0) };

    public static int GetDirection(Vec2i v)
    {
        if(v.x != 0)
        {
            v.x = (int)Mathf.Sign(v.x);
        }if(v.z != 0)
        {
            v.z = (int)Mathf.Sign(v.z);
        }
        if(v.x != 0 && v.z != 0)
        {
            throw new System.Exception("Supplied vector has more than one component");
        }
        if (v.x == 1)
            return EAST;
        if (v.x == -1)
            return WEST;
        if (v.z == 1)
            return NORTH;
        return SOUTH;
    }

    public static Vec2i GetDirection(int direction)
    {
        return DIRECTIONS[direction];
    }
    public static Vec2i GetPerpendicular(int direction)
    {
        return GetDirection((direction + 1) % 4);
    }
    public static int GetPerpendicularDirection(int direction)
    {
        return (direction + 1) % 4;
    }

    public static int OppositeDirection(int direction)
    {
        return (direction + 2) % 4;
    }

    public static readonly int NORTH = 0;
    public static readonly int EAST = 1;
    public static readonly int SOUTH = 2;
    public static readonly int WEST = 3;

    public bool IsMain;

    //public Vec2i Position { get;  }
    public Vec2i Position;
    public SettlementPathNode[] Connected { get; private set; }
    public int ConnectedCount { get; private set; }
    public SettlementPathNode(Vec2i nodePosition)
    {
        if(nodePosition == null)
        {
            Debug.LogError("[SettlementPathNode] Node position cannot be null");
        }if(nodePosition == new Vec2i(0, 0))
        {
            Debug.Log("HEREHERHEHRE");
        }
        Position = nodePosition;
        Connected = new SettlementPathNode[4] { null, null, null, null };

        ConnectedCount = 0;
    }

    public void AddConnection(int direction, SettlementPathNode node)
    {
        
        //If the direction was NOT null, and will be set to null, our connection count goes down
        if (Connected[direction] != null && node == null)
            ConnectedCount -= 1;
        else if (Connected[direction] == null && node != null)
            ConnectedCount += 1;
        if(node != null)
            Connected[direction] = node;
        
    }

    public List<SettlementPathNode> GetConnected()
    {
        List<SettlementPathNode> connected = new List<SettlementPathNode>();
        foreach (SettlementPathNode con in Connected)
            if (con != null)
                connected.Add(con);
        return connected;
    }

    /// <summary>
    /// Checks if this node is connected to any other
    /// </summary>
    /// <returns></returns>
    public bool HasConnection()
    {
        foreach (SettlementPathNode connected in Connected)
            if (connected != null)
                return true;
        return false;
    }

    public override string ToString()
    {
        if (Position == null)
            return "error";
        return Position.ToString();
    }

}