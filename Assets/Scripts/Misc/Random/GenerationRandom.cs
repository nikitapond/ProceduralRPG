﻿using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
/// <summary>
/// A holder class that allows easy use of various random functions
/// </summary>
[System.Serializable]
public class GenerationRandom
{

    public static GenerationRandom RNG = new GenerationRandom(System.DateTime.Now.Millisecond);

    private System.Random random;
    public GenerationRandom(int seed)
    {
        random = new System.Random(seed);
    }

    /// <summary>
    /// Returns a float between 0.0 (inclusive) and 1 (exclusive)
    /// </summary>
    /// <returns></returns>
    public float Random()
    {
        return (float)random.NextDouble();
    }

    /// <summary>
    /// Returns a float between min (inclusive) and max (exclusive)
    /// </summary>
    /// <returns></returns>
    public float Random(float min, float max)
    {
        return min + (max - min) * (float)random.NextDouble();
    }

    public float[] RandomFloatArray(float min, float max, int count)
    {
        float[] ran = new float[count];
        for(int i=0; i<count; i++)
        {
            ran[i] = Random(min, max);
        }
        return ran;
    }

    public int RandomSign()
    {
        return RandomInt(0, 2)==0?-1:1;
    }

    /// <summary>
    /// Returns true 'percent'/100 of the time.
    /// </summary>
    /// <param name="percent"></param>
    /// <returns></returns>
    public bool PercentageChance(float percent)
    {
        if (percent / 100 > Random())
            return true;
        return false;
    }


    /// <summary>
    /// Returns an integer between min (inclusive) and max-1
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    public int RandomInt(int min, int max)
    {
        return random.Next(min, max);
    }

    public int RandomIntFromSet(params int[] param)
    {
        return RandomFromArray(param);
    }

    public bool RandomBool()
    {
        return random.Next(0, 2) == 0 ? true : false;
    }

    public Color RandomColor(float alpha=1)
    {
        return new Color(Random(), Random(), Random(), alpha);
    }

    public Vec2i RandomQuadDirection()
    {
        return Vec2i.QUAD_DIR[RandomInt(0, 4)];
    }
    public Vec2i RandomVec2i(int mins, int maxs)
    {
        return new Vec2i(RandomInt(mins, maxs), RandomInt(mins, maxs));
    }

    public Vec2i RandomVec2i(int minX, int maxX, int minZ, int maxZ)
    {
        return new Vec2i(RandomInt(minX, maxX), RandomInt(minZ, maxZ));
    }

    public float PerlinNoise3D(Vector3 v, float scale=1)
    {
        return PerlinNoise3D(v.x*scale, v.y * scale, v.z * scale);
    }
    public float PerlinNoise3D(float x, float y, float z)
    {
        float xy = Mathf.PerlinNoise(x, y);
        float xz = Mathf.PerlinNoise(x, z);
        float yz = Mathf.PerlinNoise(y, z);
        float yx = Mathf.PerlinNoise(y, x);
        float zx = Mathf.PerlinNoise(z, x);
        float zy = Mathf.PerlinNoise(z, y);

        return (xy + xz + yz + yx + zx + zy) / 6;
    }

    /// <summary>
    /// Randomly finds a position on a circle of specified radius about the middle point
    /// </summary>
    /// <param name="mid"></param>
    /// <param name="radius"></param>
    /// <returns></returns>
    public Vector3 RandomPositionOnRadius(Vector3 mid, float radius)
    {
        float theta = Random(0, Mathf.PI * 2);

        float x = mid.x + Mathf.Cos(theta);
        float z = mid.z + Mathf.Sin(theta);
        return new Vector3(x, mid.y, z);
    }

    /// <summary>
    /// Generates a random vector3 with all coordinates within specified bounds
    /// </summary>
    /// <param name="mins"></param>
    /// <param name="maxs"></param>
    /// <returns></returns>
    public Vector3 RandomVector3(float mins, float maxs)
    {
        return new Vector3(Random(mins, maxs), Random(mins, maxs), Random(mins, maxs));
    }
    public Vector3 RandomVector3(float minX, float maxX, float minY, float maxY, float minZ, float maxZ)
    {
        return new Vector3(Random(minX, maxX), Random(minY, maxY), Random(minZ, maxZ));
    }

    public Vector3[] RandomVector3Array(float min, float max, int count)
    {
        Vector3[] ran = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            ran[i] = RandomVector3(min, max);
        }
        return ran;
    }

    public Vec3i RandomVec3i(int mins, int maxs)
    {
        return new Vec3i(RandomInt(mins, maxs+1), RandomInt(mins, maxs+1), RandomInt(mins, maxs+1));

    }
    public Vec3i RandomVec3i(int minX, int maxX, int minY, int maxY, int minZ, int maxZ)
    {
        return new Vec3i(RandomInt(minX, maxX), RandomInt(minY, maxY), RandomInt(minZ, maxZ));
    }

    /// <summary>
    /// Generates a random vector2 with all coordinates within specified bounds
    /// </summary>
    /// <param name="mins"></param>
    /// <param name="maxs"></param>
    /// <returns></returns>
    public Vector2 RandomVector2(float mins, float maxs)
    {
        return new Vector2(Random(mins, maxs), Random(mins, maxs));
    }

    public T RandomFromList<T>(List<T> arr)
    {
        if (arr.Count == 0)
            return default(T);
        if (arr.Count == 1)
            return arr[0];
        return arr[RandomInt(0, arr.Count)];
    }
    public T RandomFromArray<T>(T[] arr)
    {
        if (arr.Length == 0)
            return default(T);
        if (arr.Length == 1)
            return arr[0];
        return arr[RandomInt(0, arr.Length)];
    }

    /// <summary>
    /// Returns a number of a gaussian distribution, with a mean of 0 and 
    /// a standard deviation of 1.
    /// 
    /// 
    /// Not my code, copied:
    /// https://stackoverflow.com/questions/5817490/implementing-box-mueller-random-number-generator-in-c-sharp
    /// </summary>
    /// <returns></returns>
    public float GaussianFloat()
    {
        float u, v, S;

        do
        {
            u = (float)(2.0 * random.NextDouble() - 1.0);
            v = (float)(2.0 * random.NextDouble() - 1.0);
            S = u * u + v * v;
        }
        while (S >= 1.0);

        float fac = Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);
        return u * fac;
    }

    /// <summary>
    /// Returns a gaussian distribution with the specified mean and standard distribution
    /// <see cref="GaussianFloat()"/>
    /// </summary>
    /// <param name="mean"></param>
    /// <param name="std"></param>
    /// <returns></returns>
    public float GaussianFloat(float mean, float std)
    {
        return GaussianFloat() * std + mean;
    }

    public Item RandomItemFromInventory(Inventory inv)
    {
        ItemStack itst = RandomFromList(inv.GetItems());
        return itst.Item;
    }

    public T RandomItemFromInventoryOfType<T>(Inventory inv) where T : Item
    {
        
        List<T> valid = new List<T>();
        foreach(ItemStack itst in inv.GetItems())
        {
            if (itst.Item is T item)
                valid.Add(item);
        }
        if (valid.Count == 0)
            return null;
        return RandomFromList(valid);
    }


    public EquiptableItem RandomEquiptmentForSlot(Inventory inv, EquiptmentSlot slot)
    {
        List<EquiptableItem> valid = new List<EquiptableItem>();
        foreach(ItemStack itst in inv.GetItems())
        {
            Item it = itst.Item;
            if(it is EquiptableItem equipt)
            {
                if (equipt.EquiptableSlot == slot)
                    valid.Add(equipt);
            }
        }
        return RandomFromList(valid);
    }
}