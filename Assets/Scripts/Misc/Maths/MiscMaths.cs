using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class MiscMaths
{
    public static void SetRandomSeed(int i)
    {
        SysRandom = new System.Random(i);
    }

    private static System.Random SysRandom;
    public static int ThreadSafeRandomInt()
    {
        int val = -1;
        if (SysRandom == null)
            SysRandom = new System.Random(0);
        lock (SysRandom)
        {
            val = SysRandom.Next();
        }
        return val;
    }

    public static float DistanceSqr(Vector3 a, Vector3 b)
    {
        return (a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z) + (a.y - b.y) * (a.y - b.y);
    }

    public static float ThreadSafeRandom()
    {
        float val = -1;
        if (SysRandom == null)
            SysRandom = new System.Random(0);
        lock (SysRandom)
        {
            val = (float)SysRandom.NextDouble();
        }
        return val;
    }

    /// <summary>
    /// returns true if the distance between a and b is less than or equal
    /// to the given distance.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    public static bool WithinDistance(Vector3 a, Vector3 b, float distance)
    {

        return (a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) + (a.z - b.z) * (a.z - b.z) <= distance * distance;
        
    }

    public static int RandomRange(int min, int max, float val=-1)
    {
        if (val == -1)
            val = ThreadSafeRandom();
        return min + (int)((val) * (max - min));
    }
    public static int RandomRangeExcluding(int min, int max, List<int> exclude)
    {
        bool valid = false;
        int toOut = RandomRange(min, max);
        while (!valid)
        {
            if (!exclude.Contains(toOut))
                valid = true;
            toOut = RandomRange(min, max);
            
        }
        return toOut;
    }
}
public static class VectorHelper
{
    /// <summary>
    /// Takes a vector 3 and converts it to a vector2 with x and z components.
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public static Vector2 XZ(this Vector3 v)
    {
        return new Vector2(v.x, v.z);
    }

}