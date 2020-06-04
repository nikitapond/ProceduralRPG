using UnityEngine;
using UnityEditor;

public class GameGenerator2
{

    public int Seed { get; private set; }

    public TerrainGenerator2 TerGen;
    public GridPlacement GridPlacement;
    public KingdomGenerator2 KingdomGen;
    public SettlementGenerator2 SettlementGen;
    public GameGenerator2(int seed)
    {
        Seed = seed;
    }


    public void GenerateWorld()
    {
        TerGen = new TerrainGenerator2(this, Seed);
        TerGen.Generate();
        

        GridPlacement = new GridPlacement(this);
        GridPlacement.Generate();

        KingdomGen = new KingdomGenerator2(this);
        KingdomGen.ClaimKingdomChunks();

        SettlementGen = new SettlementGenerator2(this);
        SettlementGen.DecideSettlementPlacement(KingdomGen.Kingdoms);

    }


    public static bool InBounds(Vec2i v)
    {
        return v.x >= 0 && v.z >= 0 && v.x < World.WorldSize && v.z < World.WorldSize;
    }
}