using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
[System.Serializable]
public class ChunkData2
{
    public int X { get; private set; }
    public int Z { get; private set; }
    public int[,] TileIDs { get; private set; }
    public float[,] Heights { get; private set; }
    public float BaseHeight { get; private set; }

    public bool IsLand { get; private set; }

    private int SettlementID=-1;
    private int KingdomID=-1;


    public ChunkVoxelData VoxelData { get; private set; }

    public List<WorldObjectData2> WorldObjects;

    public ChunkData2(int x, int z, int[,] tiles, bool isLand, 
        float baseHeight = 5, float[,] heightMap=null, List<WorldObjectData2> objects=null)
    {
        X = x;
        Z = z;
        TileIDs = tiles;
        IsLand = isLand;
        BaseHeight = baseHeight;
        Heights = heightMap;
    }

    public void SetVoxelData(ChunkVoxelData cvd)
    {
        VoxelData = cvd;
    }

    public Tile GetTile(int x, int z)
    {
        return Tile.FromID(TileIDs[x, z]);
    }


    public Kingdom GetKingdom()
    {
        return GameManager.WorldManager.World.GetKingdom(KingdomID);
    }
    public void SetSettlement(Settlement set)
    {
        SettlementID = set.SettlementID;
    }
    public Settlement GetSettlement()
    {
        return GameManager.WorldManager.World.GetSettlement(SettlementID);
    }


    public override string ToString()
    {
        return "Chunk_" + X + "_" + Z; 
    }
}