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

        BuildingGenerator.BuildBoundingWallRect(vox, house.Width, house.Height, 6, Voxel.wood);
        //BuildingGenerator.ConnectBoundingWall(vox, house.BoundingWall, Voxel.wood, 5);
        BuildingGenerator.SetTiles(floor, 0, 0, house.Width-1, house.Height-1, Tile.STONE_FLOOR);
        house.SetBuilding(floor);
        
        BuildingGenerator.ChooseEntrancePoint(genRan, vox, house);
        BuildingGenerator.AddWindow(genRan, vox, house, size: genRan.RandomInt(1, 4), autoReattempt: true);
        BuildingGenerator.AddWindow(genRan, vox, house, size: genRan.RandomInt(1, 4), autoReattempt: true);
        BuildingGenerator.AddWindow(genRan, vox, house, size: genRan.RandomInt(1, 4), autoReattempt: false);
        BuildingGenerator.AddWindow(genRan, vox, house, size: genRan.RandomInt(1, 4), autoReattempt: false);
        BuildingGenerator.AddWindow(genRan, vox, house, size: genRan.RandomInt(1, 4), autoReattempt: false);
        house.InsideWallPoints = BuildingGenerator.FindInsideWallBoundryPoints(vox, house);
        
        /*foreach (Vec2i v in house.InsideWallPoints)
        {
            //house.AddObjectReference(new Chest().SetPosition(v));
            WorldObjectData bed = new Bed().SetPosition(v);
            bed.SetRotation(Vec2i.Angle(Vec2i.Forward, BuildingGenerator.GetWallPointDirection(house, v)));
            if(!house.ObjectIntersects(bed))
                house.AddObjectReference(bed);
        }*/
        
        BuildingGenerator.PlaceObjectAgainstWall(genRan, new DoubleBed(), 0, vox, house, .3f, distToEntr: 4, attemptsCount: 40);
        BuildingGenerator.PlaceObjectAgainstWall(genRan, new Chest(), 0, vox, house, 0.1f, distToEntr: 3, attemptsCount: 40);

        for (int i=0; i < house.BoundingWall.Length; i++)
        {
            BuildingGenerator.PlaceObjectAgainstWall(genRan, new WallTorch(), 1.5f, vox, house, 0f, requireWallBacking:true);
        }
        
        //BuildingGenerator.AddRoof(genRan, vox, house, Voxel.thatch);

        BuildingSubworldBuilder b = new BuildingSubworldBuilder(house, vox);
        b.CreateSubworld();
        //ChunkData[,] subChunks = new ChunkData[1, 1];


        return house;

    }



}