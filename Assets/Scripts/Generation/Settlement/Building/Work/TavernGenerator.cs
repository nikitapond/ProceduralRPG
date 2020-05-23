using UnityEngine;
using UnityEditor;

public class TavernGenerator
{
    public static Tavern GenerateTavern(GenerationRandom genRan, Tavern tavern, out BuildingVoxels vox)
    {
        vox = new BuildingVoxels(tavern.Width, World.ChunkHeight, tavern.Height);
        BuildingGenerator.BuildBoundingWallRect(vox, tavern.Width, tavern.Height, 6, Voxel.wood);
        BuildingGenerator.ChooseEntrancePoint(genRan, vox, tavern);

        Tile[,] tileMap = new Tile[tavern.Width, tavern.Height];

        BuildingGenerator.SetTiles(tileMap, 0, 0, tavern.Width, tavern.Height, Tile.WOOD_FLOOR);
        tavern.SetBuilding(tileMap);
        BuildingGenerator.AddWindow(genRan, vox, tavern, autoReattempt: true);
        BuildingGenerator.AddWindow(genRan, vox, tavern, autoReattempt: true);
        BuildingGenerator.AddWindow(genRan, vox, tavern, autoReattempt: true);
        BuildingGenerator.AddWindow(genRan, vox, tavern, autoReattempt: false);
        BuildingGenerator.AddWindow(genRan, vox, tavern, autoReattempt: false);
        FirePlace fp = new FirePlace();
        BuildingGenerator.PlaceObjectAgainstWall(genRan, fp, 0, vox, tavern, 0, true, 4, true, 20);
        BuildingGenerator.PlaceObjectAgainstWall(genRan, new WallTorch(), 1.5f, vox, tavern, 0, true, 2);
        NPCJob[] jobs = new NPCJob[] { new NPCJobMerchant(tavern, "Tavern Keep"), new NPCJobMerchant(tavern, "Tavern Keep") };
        tavern.SetWorkBuildingData(new WorkBuildingData(jobs));
        BuildingGenerator.AddRoof(genRan, vox, tavern, Voxel.thatch);
        return tavern;
    }
}