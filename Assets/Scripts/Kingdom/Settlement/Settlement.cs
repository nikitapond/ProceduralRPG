using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class Settlement
{

    public SettlementPathNode IMPORTANT;

    public int SettlementID { get; private set; }
    public int KingdomID { get; private set; }
    public string Name { get; private set; }

    public Vec2i Centre { get; private set; }
    public Vec2i BaseCoord { get; private set; }
    public Recti SettlementBounds { get; private set; }
    public Vec2i[] SettlementChunks { get; private set; }

    public int TileSize { get; private set; }

    public List<Building> Buildings { get; private set; }
    //public List<NPC> SettlementNPCs { get; private set; }
    public List<int> SettlementNPCIDs { get; private set; }
    public List<int> SettlementLeaderNPCIDs { get; private set; }
    public SettlementType SettlementType { get; private set; }
    public SettlementPathFinder SettlementPathFinder { get; private set; }

    public List<Vec2i> TEST_PATH_NODES;
    public List<SettlementPathNode> TEST_NODES;

    public SettlementPathNode[,] tNodes;

    public WorldMapLocation WorldMapLocation { get; private set; }

    public float[,] PathNodes;

    public Settlement(Kingdom kingdom, string name, SettlementBuilder builder)
    {
        IMPORTANT = builder.ENTR_NODE;
        Name = name;
        KingdomID = kingdom.KingdomID;
        TileSize = builder.TileSize;
        Centre = builder.Centre;
        BaseCoord = builder.BaseTile;
        SettlementBounds = new Recti(BaseCoord.x, BaseCoord.z, TileSize, TileSize);
        SettlementChunks = builder.SettlementChunks;
        Buildings = builder.Buildings;
        SettlementNPCIDs = new List<int>();
        SettlementLeaderNPCIDs = new List<int>();
        //SettlementNPCs = new List<NPC>();
        //setBuild = builder;

        PathNodes = builder.PathNodes;
        SettlementType = builder.SettlementType;
        foreach (Building b in Buildings)
        {
            b.SetSettlement(this);
        }

        SettlementPathFinder = builder.SettlementPathFinder;
    }

    public void SetWorldMapLocation(WorldMapLocation wml)
    {
        WorldMapLocation = wml;
    }
    public void AfterApplyToWorld()
    {
        Debug.Log("Setting set: " + BaseCoord, Debug.SETTLEMENT_GENERATION);
        foreach (Building b in Buildings)
        {
            b.GetSpawnableTiles();
            b.AfterApplyToWorld();
        }
    }
    public Kingdom GetKingdom()
    {
        return GameManager.WorldManager.World.GetKingdom(KingdomID);
    }
    public void SetKingdomID(int id)
    {
        KingdomID = id;
    }
    public void SetSettlementID(int id)
    {
        SettlementID = id;
    }

    public override string ToString()
    {
        return "Settlement: " + Centre + " - Type: " + this.SettlementType;
    }
    public void AddNPC(NPC npc)
    {
        SettlementNPCIDs.Add(npc.ID);
        //SettlementNPCs.Add(npc);
    }
    public void AddLeader(NPC npc)
    {
        SettlementLeaderNPCIDs.Add(npc.ID);
    }
}