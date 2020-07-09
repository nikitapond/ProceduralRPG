using UnityEngine;
using UnityEditor;

public class FarmGenerator 
{

    public static Farm GenerateVegFarm(GenerationRandom genRan, Farm farm, out BuildingVoxels vox, BuildingGenerationPlan plan)
    {
        vox = new BuildingVoxels(farm.Width, World.ChunkHeight, farm.Height);

        Tile[,] tiles = new Tile[farm.Width, farm.Height];
        BuildingGenerator.SetTiles(tiles, 0, 0, farm.Width-1, farm.Height-1, Tile.TEST_MAGENTA);

        farm.SetBuilding(tiles);
        BuildingGenerator.BuildBoundingWallRect(vox, farm.Width, farm.Height, 2, Voxel.stone);

        BuildingGenerator.ChooseEntrancePoint(genRan, vox, farm, plan, false);


        return farm;
    }

    public static Farm GenerateWheatFarm(GenerationRandom genRan, Farm farm, out BuildingVoxels vox, BuildingGenerationPlan plan)
    {
        vox = new BuildingVoxels(farm.Width, World.ChunkHeight, farm.Height);

        Tile[,] tiles = new Tile[farm.Width, farm.Height];
        BuildingGenerator.SetTiles(tiles, 0, 0, farm.Width - 1, farm.Height - 1, Tile.TEST_BLUE);

        farm.SetBuilding(tiles);
        BuildingGenerator.BuildBoundingWallRect(vox, farm.Width, farm.Height, 2, Voxel.stone);

        BuildingGenerator.ChooseEntrancePoint(genRan, vox, farm, plan, false);


        return farm;
    }

}