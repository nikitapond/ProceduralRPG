using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class BarracksGenerator
{

    public static Barracks GenerateBarracks(GenerationRandom genRan, Barracks barr, out BuildingVoxels vox, BuildingGenerationPlan plan)
    {

        vox = new BuildingVoxels(barr.Width, World.ChunkHeight, barr.Height);
        ChooseWallBounds(genRan, barr);
        BuildingGenerator.ConnectBoundingWall(vox, barr.BoundingWall, Voxel.stone);
        Tile[,] tileMap = new Tile[barr.Width, barr.Height];
        BuildingGenerator.ChooseEntrancePoint(genRan, vox, barr, plan);
        BuildingGenerator.SetTiles(tileMap, 0, 0, barr.Width/2, barr.Height - 1, Tile.STONE_FLOOR);
        BuildingGenerator.SetTiles(tileMap, barr.Width / 2, 0, barr.Width / 2 -1, barr.Height - 1, Tile.DIRT);

        List<NPCJob> jobs = new List<NPCJob>();

        for (int x=2; x<barr.Height; x += 3) {

            Vector3 pos = new Vector3(barr.Width - 2, 0, x);
            float rotation = Vec2i.Angle(Vec2i.Forward,BuildingGenerator.GetWallPointDirection(barr, new Vec2i(barr.Width - 2, x)));
            TrainingDummy obj = new TrainingDummy().SetPosition(pos).SetRotation(rotation) as TrainingDummy;
            if (BuildingGenerator.AddObject(barr, vox, obj))
            {
                jobs.Add(new NPCJobSoldier(barr));   
            }
        
        }

        

        WorkBuildingData wbd = new WorkBuildingData(jobs.ToArray());
        barr.SetWorkBuildingData(wbd);
        barr.SetBuilding(tileMap);

        return barr;
    }

    private static void ChooseWallBounds(GenerationRandom genRan, Barracks bar)
    {
        Vec2i[] wallPoints = new Vec2i[4];
        wallPoints[0] = new Vec2i(0, 0);
        wallPoints[1] = new Vec2i(bar.Width / 2, 0);
        wallPoints[2] = new Vec2i(bar.Width / 2, bar.Height-1);
        wallPoints[3] = new Vec2i(0, bar.Height - 1);

        bar.BoundingWall = wallPoints;
    }

    


}