using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class ChunkStructureGenerator2 
{
    private GameGenerator2 GameGen;
    private GenerationRandom GenRan;

    private List<GridPoint> FreePoints;

    public List<ChunkStructure> ChunkStructures;
    private List<ChunkStructureBuilder> Builders;

    private WeightRandomList<GridPoint> DesireWeightedGridpoints;

    public ChunkStructureGenerator2(GameGenerator2 gameGen)
    {
        GameGen = gameGen;
        GenRan = new GenerationRandom(gameGen.Seed);
        ChunkStructures = new List<ChunkStructure>();
        Builders = new List<ChunkStructureBuilder>();
    }
    /// <summary>
    /// Generates all the chunk structures in the world
    /// </summary>
    public void Generate()
    {
        //Find all free points
        FindAllFreeGridPoints();
        GenerateBanditCamps(6);
        GenerateAllChunkStructures();
    }

    



    private void GenerateBanditCamps(int count)
    {

        for(int i=0; i<count; i++)
        {
            GridPoint gp = GetFreeGridPoint(4);

            if(gp != null)
            {
                ChunkStructure cStruct = new BanditCamp(gp.ChunkPos, new Vec2i(2, 2));

                ChunkStructures.Add(cStruct);
                gp.ChunkStructure = cStruct;
            }

        }

    }


    private void GenerateAllChunkStructures()
    {
        //TODO - move to thread

        //Iterate all structures
        foreach(ChunkStructure cStruct in ChunkStructures)
        {
            if(cStruct is BanditCamp)
            {
                BanditCampBuilder bcb = new BanditCampBuilder(cStruct, null);
                //bcb.Generate(GenRan);
                Builders.Add(bcb);
                //
            }
        }
    }


    /// <summary>
    /// Iterates all grid points in the world, and checks if they are free and valid
    /// If so, we add the point to the list of free points.
    /// </summary>
    private void FindAllFreeGridPoints()
    {

        FreePoints = new List<GridPoint>();
        for(int x=0; x<GridPlacement.GridSize; x++)
        {
            for (int z = 0; z < GridPlacement.GridSize; z++)
            {
                GridPoint gp = GameGen.GridPlacement.GridPoints[x, z];
                if (gp != null && !gp.HasSettlement && gp.IsValid)
                    FreePoints.Add(gp);
            }
        }
    }
    public GridPoint GetFreeGridPoint(int clearance, int attempts = 5)
    {
        if (attempts == -1)
            return null;
        attempts --;
        GridPoint testPoint = GenRan.RandomFromList(FreePoints);

        for(int x=-clearance; x<=clearance; x++)
        {
            for (int z = -clearance; z <= clearance; z++)
            {
                Vec2i p = testPoint.GridPos + new Vec2i(x, z);
                if (GridPlacement.InGridBounds(p))
                {
                    GridPoint gp = GameGen.GridPlacement.GridPoints[p.x, p.z];
                    if (gp == null)
                        continue;
                    if (gp.HasNonSettlementStructure)
                    {
                        return GetFreeGridPoint(clearance, attempts);
                    }
                }
            }
        }
        FreePoints.Remove(testPoint);
        testPoint.HasNonSettlementStructure = true;
        return testPoint;


    }



}