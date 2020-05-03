using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
public class MiscUtils
{
    public static T RandomFromArray<T>(List<T> arr, float ranVal)
    {
        if (arr.Count == 0)
            return default(T);
        if (arr.Count == 1)
            return arr[0];
        return arr[(int)((arr.Count - 1) * ranVal)];
    }
    public static T RandomFromArray<T>(T[] arr, float ranVal=-1)
    {
        if (ranVal == -1)
            ranVal = UnityEngine.Random.value;
        if (arr.Length == 0)
            return default(T);
        if (arr.Length == 1)
            return arr[0];
        return arr[(int)((arr.Length - 1) * ranVal)];
    }

    public static T[] GetValues<T>()
    {
        return (T[])Enum.GetValues(typeof(T));
    }
    public static string ListToString<T>(List<T> list)
    {
        string build = "(";
        for (int i = 0; i < list.Count - 1; i++)
            build += list[i].ToString() + ",";
        build += list[list.Count - 1] + ")";
        return build;
    }
    /// <summary>
    /// Calculates the Texture coordinates of a given sprite and returns
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static Rect GetSpriteTextureCoords(Sprite s)
    {
        return new Rect(s.rect.x/ s.texture.width, s.rect.y / s.texture.height, 
            s.rect.width / s.texture.width, s.rect.height / s.texture.height);
    }
}

public static class Vector3Extensions
{
    public static Vector3 Mod(this Vector3 v, int mod)
    {
        return new Vector3(v.x % mod, v.y % mod, v.z % mod);
    }
    public static SerializableVector3 Mod(this SerializableVector3 v, int mod)
    {
        return new SerializableVector3(v.x % mod, v.y % mod, v.z % mod);
    }
}