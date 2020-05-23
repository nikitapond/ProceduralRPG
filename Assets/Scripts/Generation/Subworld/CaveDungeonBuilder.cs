using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class CaveDungeonBuilder : SubworldBuilder
{


    public static Vec3i[] AllDirections;

    private float[,,] Densities;
    GenerationRandom GenRan;

    public Vec2i LocalPosition;

    public CaveDungeonBuilder(Vec2i worldEntrance, Vec2i chunkSize) : base(worldEntrance, chunkSize)
    {

        

        if (AllDirections == null)
        {
            List<Vec3i> allDir = new List<Vec3i>();
            for(int x=-1; x<=1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        if (x == 0 && y == 0 && z == 0)
                            continue;
                        allDir.Add(new Vec3i(x, y, z));
                    }
                }
            }
            AllDirections = allDir.ToArray();
        }
    }
    public override void Generate(GenerationRandom ran)
    {
        GenRan = ran;

        LocalPosition = new Vec2i(5, 5);

        // Densities = new float[TileSize.x, World.ChunkHeight, TileSize.z];
        for (int x = 0; x < TileSize.x; x++)
        {
            for (int z = 0; z < TileSize.z; z++)
            {
                float scale = 0.1f;
                int height = (int)(8 * Mathf.PerlinNoise(x * scale, z * scale));
                int thingy = (int)Mathf.Clamp(8 * Mathf.PerlinNoise(x * scale, z * scale), 0, 3);
               
                for(int y=0; y<height; y++)
                {
                    SetVoxelNode(x, y, z, new VoxelNode(Voxel.stone));

                }
            }
        }
        SubworldEntrance = new Vec2i(5, 5);
        Exit = new TrapDoor();
        (Exit as WorldObjectData).SetPosition(new Vec2i(5,5));
        AddObject((Exit as WorldObjectData), true);
        return;
    }



    public CaveRoom GenerateRoom(Vec3i position, Vector3 size)
    {
        CaveRoom cr = new CaveRoom(position, size, new Vec3i(0, 0, 0), new Vec3i(0, 0, 0));
        List<Vec3i> points = cr.AllPointsInside();
        float perlinLim = Mathf.Clamp(GenRan.GaussianFloat(0.4f, 0.2f), 0.1f, 0.5f);
        foreach (Vec3i v in points)
        {

            float perlin = GenRan.PerlinNoise3D(v, 0.1f);
            if(perlin < 0.6f)
                ClearVoxelNode(v.x, v.y, v.z);
            
        }

        return cr;
    }


    private List<CaveRoom> OrderRooms(List<CaveRoom> rooms)
    {
        List<CaveRoom> orderedRooms = new List<CaveRoom>();
        orderedRooms.Add(rooms[0]);
        rooms.Remove(rooms[0]);
        int curCount = rooms.Count;
        CaveRoom curRoom = orderedRooms[0];

        for (int i = 0; i < curCount-1; i++)
        {
            int currentIndexDistance = -1;
            int currentRoomIndex = -1;

            for (int j = 0; j < rooms.Count; j++)
            {

                if (currentRoomIndex == -1)
                {
                    currentRoomIndex = i;
                    currentIndexDistance = curRoom.Position.SquareDist(rooms[j].Position);
                }
                else
                {
                    int jDist = curRoom.Position.SquareDist(rooms[j].Position);
                    if (jDist < currentIndexDistance)
                    {
                        currentIndexDistance = jDist;
                        currentRoomIndex = j;
                    }

                }
            }
            orderedRooms.Add(rooms[currentRoomIndex]);
            curRoom = rooms[currentRoomIndex];

            rooms.RemoveAt(currentRoomIndex);

        }
        return orderedRooms;
    }
    /// <summary>
    /// Generates a tunnel of width 1 as a guide to generate the rest of 
    /// the dungeon
    /// </summary>
    private void GeneratePath(Vec3i startPosition, int radius=3)
    {
        //We define the list that stores the tunnel
        List<Vec3i> tunnel = new List<Vec3i>();
        //We initiate it with a start position, and a second position (this helps define the initial direction)
        tunnel.Add(startPosition);
        //The second position is chosen as the direction away from the boundry of the dungeon

        int xDir = startPosition.x < TileSize.x / 2 ? 1 : GenRan.RandomInt(-1, 1);
        int zDir = xDir == 0 ? (startPosition.z < TileSize.x / 2 ? 1 : -1) : GenRan.RandomInt(-1, 2);
        Vec3i startDirection = new Vec3i(xDir, 0, zDir);

        tunnel.Add(startPosition + startDirection);

        startPosition = startPosition + startDirection;


        for (int i = 0; i < 100; i++)
        {
            startPosition = ChooseNextPosition(tunnel, radius);



        }
        foreach(Vec3i v in tunnel)
        {
            Tunnel(v.x, v.y, v.z, 2);
        }


    }

    public bool InBounds(Vec3i v)
    {
        return v.x >= 0 && v.x < TileSize.x && v.y >= 0 && v.y < World.ChunkHeight && v.z >= 0 && v.z < TileSize.z;
    }



    private Vec3i ChooseNextPosition(List<Vec3i> tunnel, int radius=3)
    {
        Vec3i basePoint = tunnel[tunnel.Count - 1];
        int length = GenRan.RandomInt(3, 8);
        Vec3i direction = basePoint - tunnel[tunnel.Count - 2];
        
        if (GenRan.Random() < 0.5f)
        {
            int axis = GenRan.RandomInt(0, 3);
            if (axis == 0)
                direction.x = direction.x == 0 ? GenRan.RandomInt(-1, 2) : 0;
            if (axis == 1)
                direction.y = direction.y == 0 ? (int)Mathf.Clamp(GenRan.GaussianFloat(0, 0.2f),-1,1) : 0;
            if (axis == 2)
                direction.z = direction.z == 0 ? GenRan.RandomInt(-1, 2) : 0;
        }

        int xEnd = basePoint.x + direction.x * length;
        int yEnd = basePoint.y + direction.y * length;
        int zEnd = basePoint.z + direction.z * length;
        if (xEnd < radius)        
            direction.x = GenRan.RandomInt(0, 2);
        else if (xEnd >= TileSize.x - radius)
            direction.x = GenRan.RandomInt(-1, 1);

        if (yEnd < radius)
            direction.y = GenRan.RandomInt(0, 2);
        else if (yEnd >= World.ChunkHeight - radius)
            direction.y = GenRan.RandomInt(-1, 1);

        if (zEnd < radius)
            direction.z = GenRan.RandomInt(0, 2);
        else if (zEnd >= TileSize.z - radius)
            direction.z = GenRan.RandomInt(-1, 1);




        for (int i=0; i< length; i++)
        {
            tunnel.Add(basePoint + direction * (i + 1));
            
        }

        return tunnel[tunnel.Count - 1];

    }


    private void ConnectRooms(Vec3i a, Vec3i b)
    {
        List<Vec3i> path = new List<Vec3i>();
        float[] costs = new float[AllDirections.Length];

        bool isComplete = false;

        Vec3i current = a;
        path.Add(current);
        while (!isComplete)
        {

            int minIndex = -1;

            for(int i=0; i<AllDirections.Length; i++)
            {

                Vec3i point = current + AllDirections[i];
                if (!path.Contains(point))
                {
                    costs[i] = NodeCost(point, b);
                }
                else
                {
                    costs[i] = Mathf.Infinity;
                }
                if (minIndex == -1)
                    minIndex = i;
                else if (costs[i] < costs[minIndex])
                    minIndex = i;
                    
            }

            Vec3i next = current + AllDirections[minIndex];
            path.Add(next);
            current = next;
            ClearVoxelNode(current.x,current.y,current.z);


            if (current == b)
                isComplete = true;
        }


    }

    private float NodeCost(Vec3i node, Vec3i destination)
    {

        float distCost = node.SquareDist(destination);
        float voxCost = Densities[node.x, node.y, node.z] * 32;

        return distCost + voxCost;

    }



    private List<CaveRoom> GenerateRooms(int roomCount = 4)
    {


        List<CaveRoom> rooms = new List<CaveRoom>();

        for(int i=0; i<roomCount; i++)
        {


            
            Vector3 size = GenRan.RandomVector3(8, 32, 3, 7, 8, 32);
            Vec3i roomPos = GenRan.RandomVec3i((int)size.x + 1, (int)(TileSize.x - size.x - 1), (int)size.y + 1, (int)(World.ChunkHeight - size.y - 1),
                                                (int)size.z + 1, (int)(TileSize.z - size.z - 1));
            Debug.Log(roomPos + "_" + size);
            rooms.Add(new CaveRoom(roomPos, size, new Vec3i(0, 0, 0), new Vec3i(0, 0, 0)));
            List<Vec3i> vs = rooms[rooms.Count - 1].AllPointsInside();
            Debug.Log(vs.Count + " nodes total");
            foreach (Vec3i v in vs)
            {
                //if(InBounds(v))
                    ClearVoxelNode(v.x, v.y, v.z);
                Densities[v.x, v.y, v.z] = 0;
            }
            

        }
        return rooms;


    }



    private void Tunnel(int x, int y, int z, int radius)
    {

        for(int x_=-radius; x_<=radius; x_++)
        {
            for (int y_ = -radius; y_ <= radius; y_++)
            {
                for (int z_ = -radius; z_ <= radius; z_++)
                {
                    if(x_*x_+y_*y_+z_*z_ < radius * radius)
                    {
                        if(InBounds(new Vec3i(x + x_, y + y_, z_ + z)))
                            ClearVoxelNode(x + x_, y + y_, z_ + z);
                    }
                }
            }
        }
 

    }


}




public struct CaveRoom
{

    public Vec3i Position;
    public Vector3 Size;
    public Vec3i Entrance, Exit;

    public CaveRoom(Vec3i xyz, Vector3 abc, Vec3i entrance, Vec3i exit)
    {
        Position = xyz;
        Size = abc;
        Entrance = entrance;
        Exit = exit;
    }
}
public static class CaveRoomHelper
{
    public static bool PointInside(this CaveRoom room, Vector3 point, float noise=1)
    {

        float x_a = (room.Position.x - point.x) * (room.Position.x - point.x) / (room.Size.x * room.Size.x);
        float y_a = (room.Position.y - point.y) * (room.Position.y - point.y) / (room.Size.y * room.Size.y);
        float z_a = (room.Position.z - point.z) * (room.Position.z - point.z) / (room.Size.z * room.Size.z);

        return x_a + y_a + z_a <= noise;
    }
    public static bool PointInside(this CaveRoom room, Vec3i point, float noise = 1)
    {

        float x_a = (room.Position.x - point.x)* (room.Position.x - point.x) / (room.Size.x * room.Size.x);
        float y_a = (room.Position.y - point.y)* (room.Position.y - point.y) / (room.Size.y * room.Size.y);
        float z_a = (room.Position.z - point.z)* (room.Position.z - point.z) / (room.Size.z * room.Size.z);

        return x_a + y_a + z_a <= noise;
    }
    public static List<Vec3i> AllPointsInside(this CaveRoom room)
    {
        List<Vec3i> inside = new List<Vec3i>(100);

        

        int check = 0;
        int valid = 0;
        for (int x = (int)-room.Size.x; x <= room.Size.x; x++)
        {
            for (int y = (int)-room.Size.y; y <= room.Size.y; y++)
            {
                for (int z = (int)-room.Size.z; z <= room.Size.z; z++)
                {

                    Vec3i pos = new Vec3i(room.Position.x + x, room.Position.y + y, room.Position.z + z);
                


                    check++;
                    if (room.PointInside(pos))
                    {

                        valid++;
                        inside.Add(pos);
                    }
                        
                }
            }
       }
        Debug.Log("Chcked " + check);
        Debug.Log("Valid " + valid);
        return inside;
    }

}