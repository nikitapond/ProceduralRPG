using UnityEngine;
using UnityEditor;
[System.Serializable]
public class ChunkRegion
{
    public static int REGION_SIZE = World.RegionSize;
    public int X { get; private set; }
    public int Z { get; private set; }
    public ChunkData[,] Chunks { get; private set; }

    public bool Generated { get; private set; }
    public ChunkRegion(int x, int z, ChunkData[,] chunks)
    {
        X = x;
        Z = z;
        Chunks = chunks;
        if (chunks != null)
            Generated = true;
        else
            Generated = false;
    }
    public ChunkRegion(int x, int z)
    {
        X = x;
        Z = z;
        Generated = false;

    }



    // public static ChunkR
}