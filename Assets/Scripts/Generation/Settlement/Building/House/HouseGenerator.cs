using UnityEngine;
using UnityEditor;

public class HouseGenerator
{


    public static House GenerateHouse(GenerationRandom genRan, House house, out BuildingVoxels vox)
    {

        return GenerateShack(genRan, house, out vox);


    }
   /// <summary>
   /// Generates the simplest type of house - a shack
   /// A shack is made of either (cobble?) stone or wood,
   /// it consists of only 1 room, with basic objects inside (bed, chair, table)
   /// </summary>
   /// <param name="house"></param>
   /// <param name="vox"></param>
   /// <returns></returns>
    public static House GenerateShack(GenerationRandom genRan, House house, out BuildingVoxels vox)
    {


        vox = new BuildingVoxels(house.Width, World.ChunkHeight, house.Height);
        Tile[,] floor = new Tile[house.Width, house.Height];
        
        BuildingGenerator.SetTiles(floor, 1, 1, house.Width-1, house.Height-1, Tile.STONE_FLOOR);
        BuildingGenerator.BuildBoundingWallRect(vox, house.Width, house.Height, 4, Voxel.wood);
        BuildingGenerator.ChooseEntrancePoint(genRan, vox, house);
        BuildingGenerator.AddWindow(genRan, vox, house, autoReattempt: false);
        BuildingGenerator.AddWindow(genRan, vox, house, autoReattempt:false);
        BuildingGenerator.AddWindow(genRan, vox, house, autoReattempt:false);
        house.SetBuilding(floor);
        BuildingGenerator.AddRoof(vox, house, Voxel.wood);
        return house;

    }



}