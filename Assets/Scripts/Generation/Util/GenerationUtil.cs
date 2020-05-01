using UnityEngine;
using UnityEditor;

public class GenerationUtil
{
    /// <summary>
    /// Draws a line of the object to copy from point a to point b (inclusive)
    /// </summary>
    /// <param name="data"></param>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="copy"></param>
    public static void ConnectPointsWithObject(WorldObjectData[,] data, Vec2i a, Vec2i b, WorldObjectData copy, Vec2i globalPos = null)
    {
        if (globalPos == null)
            globalPos = new Vec2i(0, 0);

        int x = a.x;
        int z = a.z;

        int xDir = (int)Mathf.Sign(b.x - a.x);
        int zDir = (int)Mathf.Sign(b.z - a.z);
       
        while (!(x==b.x && z==b.z))
        {
            data[x, z] = copy.Copy(globalPos + new Vec2i(x, z));

            

            int dx = (int)Mathf.Abs(b.x - x);
            int dz = (int)Mathf.Abs(b.z - z);

            if(dx > dz)
            {
                x+=xDir;
            }else if(dz > dx)
            {
                z+=zDir;

            }
            else
            {
                x+=xDir;
                z+=zDir;
            }

        }
        
        /*
        float dx = (b.x - a.x);
        float dz = (b.z - a.z);
        float mag = Mathf.Sqrt(dx * dx + dz * dz);
        dx /= mag;
        dz /= mag;

        int x = 0;
        int z = 0;

        float x_ = 0;
        float z_ = 0;


        for(int i=0; i<mag+1; i++)
        {

            int finX = Mathf.Clamp(a.x + x, 0, data.GetLength(0) - 1);
            int finZ = Mathf.Clamp(a.z + z, 0, data.GetLength(1) - 1);

            data[finX, finZ] = copy.Copy(globalPos + new Vec2i(finX, finZ));
            x_ += dx;
            z_ += dz;
            if(x_ > z_)
            {
                x++;
                x_ = 0;
            }else if(z_ > x_)
            {
                z++;
                z_ = 0;
            }
            else
            {
                x++;
                z++;
                x_ = 0;
                z_ = 0;
            }

        }
        */
    }

    public static void FillTriangleWithTile(int[,] tiles, Vec2i a, Vec2i b, Vec2i c, int tileID)
    {

        int minX = Mathf.Min(a.x, b.x, c.x)-1;
        int minZ = Mathf.Min(a.z, b.z, c.z)-1;
        int maxX = Mathf.Max(a.x, b.x, c.x)+1;
        int maxZ = Mathf.Max(a.z, b.z, c.z)+1;

        for(int x=minX; x<maxX; x++)
        {
            for(int z=minZ; z<maxZ; z++)
            {
                if (x < 0 || z < 0 || x >= tiles.GetLength(0) || z >= tiles.GetLength(1))
                    continue;
                if (PointInTriangle(new Vec2i(x, z), a, b, c))
                    tiles[x, z] = tileID;
            }
        }
    }

    /// <summary>
    /// Copies from https://stackoverflow.com/questions/2049582/how-to-determine-if-a-point-is-in-a-2d-triangle
    /// 
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="p3"></param>
    /// <returns></returns>
    private static float sign(Vec2i p1, Vec2i p2, Vec2i p3)
    {
        return (p1.x - p3.x) * (p2.z - p3.z) - (p2.x - p3.x) * (p1.z - p3.z);
    }

    public static bool PointInTriangle(Vec2i pt, Vec2i v1, Vec2i v2, Vec2i v3)
    {
        float d1, d2, d3;
        bool has_neg, has_pos;

        d1 = sign(pt, v1, v2);
        d2 = sign(pt, v2, v3);
        d3 = sign(pt, v3, v1);

        has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(has_neg && has_pos);
    }


    public static void FillBoundedShapeWithTile(int[,] tiles, WorldObjectData[,] objects, int x, int z, int tileID, bool underObjects=false)
    {

        if (x < 0 || z < 0 || x >= tiles.GetLength(0) || z >= tiles.GetLength(1))
            return;
        if (objects[x, z] != null) {
            if (underObjects)
                tiles[x, z] = tileID;
            return;
        }
            
        if (tiles[x, z] == tileID)
            return;

        tiles[x, z] = tileID;
        FillBoundedShapeWithTile(tiles, objects, x + 1, z, tileID, underObjects);
        FillBoundedShapeWithTile(tiles, objects, x - 1, z, tileID, underObjects);
        FillBoundedShapeWithTile(tiles, objects, x, z+1, tileID, underObjects);
        FillBoundedShapeWithTile(tiles, objects, x , z-1, tileID, underObjects);

    }



}