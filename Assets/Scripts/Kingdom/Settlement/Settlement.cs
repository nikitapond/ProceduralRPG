using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class Settlement : WorldLocation
{

    public SettlementPathNode IMPORTANT;

    public Vec2i GridPointPosition;

    public int SettlementID { get; private set; }
    public int KingdomID { get; private set; }
    public string Name { get; private set; }

    public Vec2i BaseChunk { get; private set; }
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

    public SettlementEconomy Economy { get; private set; }


    public Settlement(Kingdom kingdom, string name, SettlementBuilder builder) : base(builder.BaseChunk)
    {
        IMPORTANT = builder.ENTR_NODE;
        Name = name;
        KingdomID = kingdom.KingdomID;
        TileSize = builder.TileSize;
        BaseChunk = builder.BaseChunk;
        Centre = builder.BaseChunk + builder.ChunkSize/2;
        BaseCoord = builder.BaseTile;
        SettlementBounds = new Recti(BaseCoord.x, BaseCoord.z, TileSize, TileSize);
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

    public void SetEconomy(SettlementEconomy econ)
    {
        Economy = econ;
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
        if (Economy != null)
            Economy.SettlementID = id;
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

    public Vec2i RandomPathPoint()
    {
        bool isvalid = false;
        Vec2i pos = GameManager.RNG.RandomVec2i(0, PathNodes.GetLength(0)-1);
        while (!isvalid)
        {
            if (PathNodes[pos.x, pos.z] != 0)
                isvalid = true;
        }
        return BaseCoord + pos * World.ChunkSize;
    }

    public override void Tick()
    {
        if (Economy != null)
            Economy.Tick();
    }

    public override void GroupReturn(EntityGroup group)
    {
        Economy.EntityGroupReturn(group);
    }

    public static bool operator ==(Settlement a, Settlement b)
    {
        if (System.Object.ReferenceEquals(a, null))
        {
            if (System.Object.ReferenceEquals(b, null))
                return true;
            return false;
        }
        else if (System.Object.ReferenceEquals(b, null))
            return false;
   
        return a.SettlementID == b.SettlementID;
    }
    public static bool operator !=(Settlement a, Settlement b)
    {
        return !(a==b);
    }
}