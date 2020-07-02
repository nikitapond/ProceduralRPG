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

    private WeightedRandomList<GridPoint> DesireWeightedGridpoints;

    public ChunkStructureGenerator2(GameGenerator2 gameGen)
    {
        GameGen = gameGen;
        GenRan = new GenerationRandom(gameGen.Seed);
        ChunkStructures = new List<ChunkStructure>();
        Builders = new List<ChunkStructureBuilder>();
    }

    public List<ChunkStructureShell> StructureShells;
    private WeightedRandomList<GridPoint> WeightedGridPoints;
    /// <summary>
    /// Decides the placement and type of each structure
    /// </summary>
    public void GenerateAllShells()
    {
        StructureShells = new List<ChunkStructureShell>();
        //Generate the lairs first, so that the grid points aren't free
        GenerateDragonLairShells();

        WeightedGridPoints = FindFreeGridPoints();
        GenerateMainDungeonShells();
        GenerateAncientTempleShells();
        GenerateBanditCampShells();
        GenerateVampireNestShells();
    }

    /// <summary>
    /// Finds the placement for the two dragon lairs
    /// Uses this to generate structure shell for the two lairs, and adds to the list
    /// </summary>
    private void GenerateDragonLairShells()
    {

        Vec2i evil = GameGen.TerGen.EvilDragonMountainPeak;
        Vec2i good = GameGen.TerGen.GoodDragonMountainPeak;

        GridPoint evilGP = GameGen.GridPlacement.GetNearestPoint(evil);
        GridPoint goodGP = GameGen.GridPlacement.GetNearestPoint(good);

        ChunkStructureShell evilShell = new ChunkStructureShell(evilGP, ChunkStructureType.evilDragonLair);
        ChunkStructureShell goodShell = new ChunkStructureShell(goodGP, ChunkStructureType.goodDragonLair);

        StructureShells.Add(evilShell);
        StructureShells.Add(goodShell);
    }

    /// <summary>
    /// Iterates through all grid points and checks for valid ones (on land, no settlement or tactloc).
    /// We then add them to a weighted random list, with locations surrounded by settlements having a lower weighting
    /// we then return this list for use
    /// </summary>
    /// <returns></returns>
    private WeightedRandomList<GridPoint> FindFreeGridPoints()
    {
        WeightedRandomList<GridPoint> gridPoints = new WeightedRandomList<GridPoint>();

        int radius = 2;

        for(int x=0; x<GridPlacement.GridSize; x++)
        {
            for (int z = 0; z< GridPlacement.GridSize; z++)
            {
                GridPoint gp = GameGen.GridPlacement.GridPoints[x, z];
                if (!gp.IsValid)
                    continue;

                if (gp.ChunkPos.QuickDistance(GameGen.TerGen.EvilDragonMountainPeak) < 100 * 100)
                    continue;

                if (gp.ChunkPos.QuickDistance(GameGen.TerGen.GoodDragonMountainPeak) < 60 * 60)
                    continue;

                float weight = 10;

                //If there is something here, then we ignore this point
                if (gp.Shell != null)
                    continue;
                Debug.Log("No shell");
                for(int rx=-radius; rx<=radius; rx++)
                {
                    if (weight <= 0)
                        break;
                    for (int rz = -radius; rz <= radius; rz++)
                    {
                        Vec2i testPos = new Vec2i(x + rx, z + rz);
                        //Distance from gridpoint to this point
                        float dist = Mathf.Sqrt(rx * rx + rz * rz);
                        if (GridPlacement.InGridBounds(testPos))
                        {
                            //Get near point
                            GridPoint gp2 = GameGen.GridPlacement.GridPoints[testPos.x, testPos.z];
                            if(gp2.Shell != null)
                            {
                                //If too close, then point not valid
                                if (dist <= 1)
                                {
                                    weight -= 5;
                                }
                                else
                                {
                                    weight -= 3f / dist;
                                }
                            }
                          
                                

                        }
                    }
                }
                if(weight > 0)
                {
                    gridPoints.AddElement(gp, weight);
                }
            }
        }

        return gridPoints;
    }

    /// <summary>
    /// Randomly chooses a gridpoint to place each of the required main dungeons
    /// </summary>
    private void GenerateMainDungeonShells()
    {
        ChunkStructureType[] mainGen = new ChunkStructureType[] { ChunkStructureType.kithenaCatacomb };
        foreach(ChunkStructureType t in mainGen)
        {
            //Get random item
            GridPoint gp = WeightedGridPoints.GetRandom(true);
            ChunkStructureShell shell = new ChunkStructureShell(gp, t);
            StructureShells.Add(shell);
        }
    }
    private void GenerateAncientTempleShells(int count = 6)
    {
        for(int i=0; i<count; i++)
        {
            GridPoint gp = WeightedGridPoints.GetRandom(true);
            ChunkStructureShell shell = new ChunkStructureShell(gp, ChunkStructureType.ancientTemple);
            StructureShells.Add(shell);
        }
    }
    private void GenerateBanditCampShells(int count = 8)
    {
        for (int i = 0; i < count; i++)
        {
            GridPoint gp = WeightedGridPoints.GetRandom(true);
            ChunkStructureShell shell = new ChunkStructureShell(gp, ChunkStructureType.banditCamp);
            StructureShells.Add(shell);
        }
    }
    private void GenerateVampireNestShells(int count = 5)
    {
        for (int i = 0; i < count; i++)
        {
            GridPoint gp = WeightedGridPoints.GetRandom(true);
            ChunkStructureShell shell = new ChunkStructureShell(gp, ChunkStructureType.vampireNest);
            StructureShells.Add(shell);
        }
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
/// <summary>
/// Simple data structure to hold onto details required for a chunk structure until 
/// it is fully generated
/// </summary>
public class ChunkStructureShell : Shell
{

    public ChunkStructureType Type { get; private set; }
    public ChunkStructureShell(GridPoint gp, ChunkStructureType type) : base (gp, -1)
    {
        Type = type;
    }

    public override Vec2i GetSize()
    {
        throw new System.NotImplementedException();
    }
}