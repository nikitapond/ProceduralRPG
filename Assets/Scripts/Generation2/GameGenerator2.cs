using UnityEngine;
using UnityEditor;

public class GameGenerator2
{

    public int Seed { get; private set; }

    public TerrainGenerator2 TerGen;
    public GridPlacement GridPlacement;
    public KingdomGenerator2 KingdomGen;
    public SettlementGenerator2 SettlementGen;
    public ChunkStructureGenerator2 StructureGen;


    public World World;

    public GameGenerator2(int seed)
    {
        Seed = seed;
        World = new World();
    }


    public void GenerateWorld()
    {
        Debug.BeginDeepProfile("TerGen");
        TerGen = new TerrainGenerator2(this, Seed);
        TerGen.Generate();
        Debug.EndDeepProfile("TerGen");

        Debug.BeginDeepProfile("GridPlacement");
        GridPlacement = new GridPlacement(this);
        GridPlacement.Generate();
        Debug.EndDeepProfile("GridPlacement");

        Debug.BeginDeepProfile("KingdomGen");
        KingdomGen = new KingdomGenerator2(this);
        KingdomGen.ClaimKingdomChunks();
        Debug.EndDeepProfile("KingdomGen");

        Debug.BeginDeepProfile("SettleGen");
        SettlementGen = new SettlementGenerator2(this);
        SettlementGen.GenerateAllSettlements(KingdomGen.Kingdoms);
        Debug.EndDeepProfile("SettleGen");

        StructureGen = new ChunkStructureGenerator2(this);
        StructureGen.Generate();


        World.ChunkBases2 = TerGen.ChunkBases;

        Debug.BeginDeepProfile("WorldEventInit");
        WorldEventManager.Instance.Init(TerGen.ChunkBases, GridPlacement, SettlementGen.Settlements, StructureGen.ChunkStructures);
        Debug.EndDeepProfile("WorldEventInit");
    }


    public static bool InBounds(Vec2i v)
    {
        return v.x >= 0 && v.z >= 0 && v.x < World.WorldSize && v.z < World.WorldSize;
    }
}