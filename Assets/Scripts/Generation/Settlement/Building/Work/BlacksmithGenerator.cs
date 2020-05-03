using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlacksmithGenerator
{

    public static Blacksmith GenerateBlacksmith(GenerationRandom genRan, Blacksmith smith, out BuildingVoxels vox)
    {
        vox = new BuildingVoxels(smith.Width, World.ChunkHeight, smith.Height);
        ChooseWallBounds(genRan, smith);
        BuildingGenerator.ConnectBoundingWall(vox, smith.BoundingWall, Voxel.stone);
        Tile[,] tileMap = new Tile[smith.Width, smith.Height];
        //Make the whole floor stone
        BuildingGenerator.SetTiles(tileMap, 0, 0, smith.Width, smith.Height, Tile.STONE_FLOOR);

        Vec2i outSideMin = null;
        Vec2i outSideMax = null;

        //Find the outdoor part and make the floor dirt
        for(int i=0; i<smith.BoundingWall.Length; i++)
        {
            Vec2i p = smith.BoundingWall[i];
            //If this boundry point does not lie on any of the edges, then it is indented into the 
            //building. This means this point defines the outside region of the building.
            if(p.x != 0 && p.x != smith.Width-1 && p.z!=0 && p.z != smith.Height - 1)
            {
                //We get the 2 neigboring wall points, as these define the outside region
                Vec2i nm1 = smith.BoundingWall[(i - 1) % smith.BoundingWall.Length];
                Vec2i np1 = smith.BoundingWall[(i + 1) % smith.BoundingWall.Length];

                int minX = Mathf.Min(p.x, nm1.x, np1.x);
                int minZ = Mathf.Min(p.z, nm1.z, np1.z);
                int maxX = Mathf.Max(p.x, nm1.x, np1.x);
                int maxZ = Mathf.Max(p.z, nm1.z, np1.z);
                BuildingGenerator.SetTiles(tileMap, minX+1, minZ+1, maxX - minX, maxZ - minZ, Tile.DIRT);
                outSideMin = new Vec2i(minX, minZ);
                outSideMax = new Vec2i(maxX, maxZ);
                break;
            }
        }
        smith.SetBuilding(tileMap);
        PlaceOutsideObjects(genRan, smith, vox, outSideMin, outSideMax);

        BuildingGenerator.ChooseEntrancePoint(genRan, vox, smith);
        BuildingGenerator.AddWindow(genRan, vox, smith);
        BuildingGenerator.AddWindow(genRan, vox, smith);
        BuildingGenerator.AddWindow(genRan, vox, smith, autoReattempt:false);
        BuildingGenerator.AddWindow(genRan, vox, smith, autoReattempt: false);

        return smith;
    }


    private static void PlaceOutsideObjects(GenerationRandom genRan, Blacksmith smith, BuildingVoxels vox, Vec2i outMin, Vec2i outMax)
    {

        List<WorldObjectData> toPlace = new List<WorldObjectData>(new WorldObjectData[] { new Anvil(), new Anvil() });
        bool isFinished = false;
        while (!isFinished)
        {
            WorldObjectData toPlaceCur = toPlace[0];
            toPlace.RemoveAt(0);
            Vector3 pos = genRan.RandomVector3(outMin.x, outMax.x, 0, 0, outMin.z, outMax.z);
            toPlaceCur.SetPosition(pos);
            while (!BuildingGenerator.AddObject(smith, vox, toPlaceCur))
            {
                pos = genRan.RandomVector3(outMin.x, outMax.x, 0, 0, outMin.z, outMax.z);
                toPlaceCur.SetPosition(pos);
            }
            if (toPlace.Count == 0)
                isFinished = true;


        }
    }

    private static void ChooseWallBounds(GenerationRandom genRan, Blacksmith smith)
    {
        //Define a base rectangular wall
        List<Vec2i> wallPoints = new List<Vec2i>();
        wallPoints.Add(new Vec2i(0, 0));
        wallPoints.Add(new Vec2i(smith.Width-1, 0));
        wallPoints.Add(new Vec2i(smith.Width-1, smith.Height-1));
        wallPoints.Add(new Vec2i(0, smith.Height-1));



        //Choose which point index we wish to move
        int deltaPointI = genRan.RandomInt(0, wallPoints.Count);
        //We find which direction and amount the point has to move to not leave the bounds
        int xDir = (wallPoints[deltaPointI].x == 0 ? 1 : -1) * genRan.RandomInt(smith.Width / 4, smith.Width - smith.Width / 3);
        int zDir =(wallPoints[deltaPointI].z == 0 ? 1 : -1) * genRan.RandomInt(smith.Height/4, smith.Height - smith.Height/3);
        Vec2i newPoint = new Vec2i(xDir, zDir) + wallPoints[deltaPointI];

        //wallPoints.Insert(deltaPointI-1, newPoint);
        //We find the direction from the previous corner to this one, 
        //we use this to translate the point accordingly
        Vec2i nm1_to_n = wallPoints[deltaPointI] - wallPoints[(deltaPointI - 1 + wallPoints.Count) % wallPoints.Count];
        Vec2i np1_to_n = wallPoints[(deltaPointI + 1) % wallPoints.Count] - wallPoints[deltaPointI];

        if (nm1_to_n.x == 0)
        {
            wallPoints[deltaPointI].z = newPoint.z;
        }
        else
        {
            wallPoints[deltaPointI].x = newPoint.x;
        }

       // Vec2i np1_to_n = wallPoints[(deltaPointI + 1) % wallPoints.Count] - wallPoints[deltaPointI];

        Vec2i newPoint2 = new Vec2i(newPoint.x, newPoint.z);
        if (np1_to_n.x == 0)
        {
            newPoint2.x = wallPoints[(deltaPointI + 1) % wallPoints.Count].x;
        }
        else
        {
            newPoint2.z = wallPoints[(deltaPointI + 1) % wallPoints.Count].z;
        }
        wallPoints.Insert((deltaPointI + 1), newPoint);
        wallPoints.Insert((deltaPointI + 2), newPoint2);

        smith.BoundingWall = wallPoints.ToArray();
  
    }

}
