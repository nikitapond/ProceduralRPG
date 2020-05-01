using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
public enum ChunkBiome
{
    ocean, grassland, forrest, dessert
}
[System.Serializable]
public class ChunkData
{


    public int X { get; private set; }
    public int Z { get; private set; }
    public int[,] TileIDs { get; private set; }
    public float[,] Heights { get; private set; }
    public float BaseHeight { get; private set; }
    //public Tile[,] Tiles { get; private set; }
    public Dictionary<int, WorldObjectData> Objects { get; private set; }

    public string DEBUG = "";
    public bool IsLand { get; private set; }
    [SerializeField]
    private int SettlementID;
    [SerializeField]
    private int KingdomID;
    public ChunkData(int x, int z, int[,] tiles, bool isLand, float baseHeight = 5, float[,] heights=null, Dictionary<int, WorldObjectData> objects=null)
    {
        KingdomID = -1;
        SettlementID = -1;
        Heights = heights;
        X = x;
        Z = z;
        TileIDs = tiles;
        IsLand = isLand;

        Objects = objects;
        BaseHeight = baseHeight;
    }
    public ChunkData(int x, int z, Tile[,] tiles, bool isLand, Dictionary<int, WorldObjectData> objects=null)
    {
        KingdomID = -1;
        SettlementID = -1;

        X = x;
        Z = z;

        if (tiles == null)
            TileIDs = null;
        else
        {
            TileIDs = new int[World.ChunkSize, World.ChunkSize];
            for(int x_=0; x_<World.ChunkSize; x_++)
            {
                for(int z_=0; z_<World.ChunkSize; z_++)
                {
                    if (tiles[x_, z_] == null)
                    {
                        TileIDs[x_, z_] = 1; //Set to grass as default
                    }
                    else
                    {
                        TileIDs[x_, z_] = tiles[x_, z_].ID;
                    }
                }
            }
        }

        IsLand = isLand;

        Objects = objects;
    }
    public void SetTile(int x, int z, Tile tile)
    {
        TileIDs[x, z] = tile.ID;
        //if (tile == Tile.WATER)
         //   Heights[x, z] = ;
    }
    public Tile GetTile(int x, int z)
    {
        return Tile.FromID(TileIDs[x, z]);
    }

    public void SetControllingKingdom(Kingdom king)
    {
        KingdomID = king.KingdomID;
       
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

    
    public WorldObjectData GetObject(int x, int z)
    {
        if (Objects == null)
            return null;
        int hash = WorldObject.ObjectPositionHash(x, z);
        if (Objects.ContainsKey(hash))
            return Objects[hash];
        return null;
    }

    public void SetObject(int x, int z, WorldObjectData obj)
    {
        if (Objects == null)
            Objects = new Dictionary<int, WorldObjectData>();
        int hash = WorldObject.ObjectPositionHash(x, z);
        if (!Objects.ContainsKey(hash))
            Objects.Add(hash, obj);
        else
            Objects[hash] = obj;
    }


}