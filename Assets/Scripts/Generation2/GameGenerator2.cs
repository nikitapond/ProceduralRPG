using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class GameGenerator2
{



    public int Seed { get; private set; }

    public TerrainGenerator2 TerGen;
    public GridPlacement GridPlacement;
    public KingdomGenerator2 KingdomGen;
    public SettlementGenerator2 SettlementGen;
    public ChunkStructureGenerator2 StructureGen;

    private List<IWorldEventLocation> WorldEventLocations;

    public World World;

    public GameGenerator2(int seed)
    {
        Seed = seed;
        World = new World();
        WorldEventLocations = new List<IWorldEventLocation>();
    }


    public void GenerateWorld()
    {
        //We start by generating terrain
        //This generates all height map, ocean, lakes, and rivers
        //It also calculates biomes
        TerGen = new TerrainGenerator2(this, Seed);
        TerGen.Generate();
        GridPlacement = new GridPlacement(this);
        //We generate the initial grid points
        GridPlacement.GenerateInitialGridPoints();

        Debug.BeginDeepProfile("KingdomGen");
        KingdomGen = new KingdomGenerator2(this);
        //We decide the placement of each kingdom capital, and then claim territory
        KingdomGen.ClaimKingdomChunks();
        KingdomGen.GenerateKingdoms();
        Debug.EndDeepProfile("KingdomGen");

        
        Debug.BeginDeepProfile("SettleGen");
        SettlementGen = new SettlementGenerator2(this);
        SettlementGen.GenerateAllSettlementShells();
        SettlementGen.GenerateAllTacticalLocationShells();
        SettlementGen.CalculateTactialAndSettlementData();
        //SettlementGen.GenerateAllSettlements(KingdomGen.Kingdoms);
        Debug.EndDeepProfile("SettleGen");

        
        //StructureGen = new ChunkStructureGenerator2(this);


        return;
        FillWorldEventLocations();
        World.ChunkBases2 = TerGen.ChunkBases;

        Debug.BeginDeepProfile("WorldEventInit");
        WorldEventManager.Instance.Init(TerGen.ChunkBases, GridPlacement, WorldEventLocations);
        Debug.EndDeepProfile("WorldEventInit");
    }
    /// <summary>
    /// Iterates all settlements and chunk structures, adding them
    /// to the list <see cref="WorldEventLocations"/>
    /// </summary>
    private void FillWorldEventLocations()
    {
        foreach(Settlement s in SettlementGen.Settlements)
        {
            WorldEventLocations.Add(s);
        }
        foreach(ChunkStructure cs in StructureGen.ChunkStructures)
        {
            if(cs is IWorldEventLocation)
                WorldEventLocations.Add(cs);
        }
    }

    public static bool InBounds(Vec2i v)
    {
        return v.x >= 0 && v.z >= 0 && v.x < World.WorldSize && v.z < World.WorldSize;
    }
}