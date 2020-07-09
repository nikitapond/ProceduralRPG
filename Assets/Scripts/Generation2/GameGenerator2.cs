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
    public EntityGenerator EntityGenerator;


    private List<WorldLocation> WorldEventLocations;
    private Dictionary<Vec2i, ChunkData> PreGeneratedChunks;
    public World World;

    public GameGenerator2(int seed)
    {
        Seed = seed;
        World = new World();
        WorldManager.Instance.SetWorld(World);
        WorldEventLocations = new List<WorldLocation>();
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

        
        Debug.BeginDeepProfile("SettleShellGen");
        SettlementGen = new SettlementGenerator2(this);
        SettlementGen.GenerateAllSettlementShells();
        SettlementGen.GenerateAllTacticalLocationShells();
        SettlementGen.CalculateTactialAndSettlementData();
        //SettlementGen.GenerateAllSettlements(KingdomGen.Kingdoms);
        Debug.EndDeepProfile("SettleShellGen");


        Debug.BeginDeepProfile("StructShellGen");

        StructureGen = new ChunkStructureGenerator2(this);
        StructureGen.GenerateAllShells();
        Debug.EndDeepProfile("StructShellGen");

        Debug.BeginDeepProfile("SetChunksGen");
        PreGeneratedChunks = SettlementGen.GenerateAllSettlementChunks();
        Debug.EndDeepProfile("SetChunksGen");

        EntityGenerator = new EntityGenerator(this, EntityManager.Instance);

        return;
        FillWorldEventLocations();
        World.ChunkBases2 = TerGen.ChunkBases;

        Debug.BeginDeepProfile("WorldEventInit");
        WorldEventManager.Instance.Init(TerGen.ChunkBases, GridPlacement, WorldEventLocations);
        Debug.EndDeepProfile("WorldEventInit");
    }


    /// <summary>
    /// Starts the generation of chunk regions for the world.
    /// Initially generates regions near to the specified midpoint.
    /// All remaining regions are then generated on a seperate thread 
    /// with those closest to the initial point being generated first
    /// </summary>
    /// <param name="midpoint">The region coordinate to centre inital region generation about</param>
    public ChunkRegionGenerator GenerateChunks(Vec2i midpoint)
    {
        Debug.BeginDeepProfile("start_region_gen");

        //Create the generator and start the initial generation
        ChunkRegionGenerator crg = new ChunkRegionGenerator(this, PreGeneratedChunks);
        crg.GenStartRegion(midpoint);

        Debug.EndDeepProfile("start_region_gen");

        return crg;
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
            if(cs is WorldLocation)
                WorldEventLocations.Add(cs);
        }
    }

    public static bool InBounds(Vec2i v)
    {
        return v.x >= 0 && v.z >= 0 && v.x < World.WorldSize && v.z < World.WorldSize;
    }
}