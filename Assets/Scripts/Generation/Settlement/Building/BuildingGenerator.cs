using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class BuildingGenerator
{

    public const int NORTH_ENTRANCE = 0;
    public const int EAST_ENTRANCE = 1;
    public const int SOUTH_ENTRANCE = 2;
    public const int WEST_ENTRANCE = 3;

    /// <summary>
    /// Takes a building plan and generates a full building from it.
    /// TODO - Currently messy, fix
    /// </summary>
    /// <param name="plan"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="entrance"></param>
    /// <returns></returns>
    public static Building CreateBuilding(GenerationRandom genRan, out BuildingVoxels vox, BuildingPlan plan, int width=-1, int height=-1, int entrance = 0)
    {
        if (width == -1)
            width = MiscMaths.RandomRange(plan.MinSize, plan.MaxSize);
        else if (width < plan.MinSize)
            width = plan.MinSize;
        else if (width > plan.MaxSize)
            width = plan.MaxSize;

        if (height == -1)
            height = MiscMaths.RandomRange(plan.MinSize, plan.MaxSize);
        else if (height < plan.MinSize)
            height = plan.MinSize;
        else if (height > plan.MaxSize)
            height = plan.MaxSize;

        if(plan == Building.BLACKSMITH)
        {
            return BlacksmithGenerator.GenerateBlacksmith(genRan, new Blacksmith(width, height), out vox);
        }

        /*
        if(plan == Building.HOUSE)
        {
            House h = new House(width, height);
            h.SetBuilding(buildingBase, buildingObjects);
            return GenerateHouse(h, BuildingStyle.stone);
        }
        if(plan == Building.BLACKSMITH)
        {
            Blacksmith b = new Blacksmith(width, height);
            b.SetBuilding(buildingBase, buildingObjects);
            return GenerateBlacksmith(b, BuildingStyle.stone);
        }
        if(plan == Building.MARKET)
        {
            MarketPlace market = new MarketPlace(width, height);
            market.SetBuilding(buildingBase, buildingObjects);
            return GenerateMarket(market, BuildingStyle.stone);
        }
        if(plan == Building.BARACKS)
        {
            Baracks baracks = new Baracks(width, height);
            baracks.SetBuilding(buildingBase, buildingObjects);
            return GenerateBaracks(baracks);
        }
        /*
        if(plan == Building.MARKET)
        {
            MarketPlace market = new MarketPlace(width, height);
            market.SetBuilding(buildingBase, buildingObjects);
            return GenerateMarket(market, BuildingStyle.stone);
        }
        if(plan == Building.BARACKSCITY)
        {
            Baracks bar = new Baracks(width, height);
            bar.SetBuilding(buildingBase, buildingObjects);
            return GenerateCityBaracks(bar);
        }
        if (plan == Building.BARACKSCITY || plan == Building.BARACKSTOWN)
        {
            
        }*/

        return HouseGenerator.GenerateHouse(genRan, new House(width, height), out vox);
        //return GenerateHouse(out vox, width, height);
        
    }


    /// <summary>
    /// Places a roof over the specified building.
    /// The roof will only cover the tile specified by 'roofTileID'. If this is null, 
    /// then a roof over the total building will be created
    /// </summary>
    /// <param name="vox"></param>
    /// <param name="build"></param>
    /// <param name="roofTileID"></param>
    public static void AddRoof(BuildingVoxels vox, Building build, Voxel roofVox, int roofStartY=4, Tile roofTileReplace=null)
    {
        if(roofTileReplace == null)
        {
            for(int x=0; x<build.Width; x++)
            {
                for(int z=0; z<build.Height; z++)
                {
                    vox.SetVoxel(x, roofStartY, z, roofVox);
                }
            }
        }
    }

    public static bool AddWindow(GenerationRandom genRan, BuildingVoxels vox, Building build, int wallIndex = -1, int size = 2, int height=1,bool autoReattempt = true)
    {
        //if default index, we choose one randomly
        if (wallIndex == -1)
            wallIndex = genRan.RandomInt(0, build.BoundingWall.Length);
        int wp1 = (wallIndex + 1) % build.BoundingWall.Length;

        Vec2i dif = build.BoundingWall[wp1] - build.BoundingWall[wallIndex];

        int absX = Mathf.Abs(dif.x);
        int signX = (int)Mathf.Sign(dif.x);


        int absZ = Mathf.Abs(dif.z);
        int signZ = (int)Mathf.Sign(dif.z);


        Vec2i dir = new Vec2i(absX==0?0:signX, absZ == 0 ? 0 : signZ);



        int len = absX + absZ;
        //if the selected wall has a length that is not large enough to house the window, 
        if (len < size + 4)
        {
            if (autoReattempt)
                return AddWindow(genRan, vox, build, wp1, size, height, autoReattempt);
            //Then we do not build a window
            return false;
        }


        int dx = dir.x == 0 ? 0 : genRan.RandomInt(1, absX - size-2);
        int dz = dir.z == 0 ? 0 : genRan.RandomInt(1, absZ - size-2);
        Vec2i dPos = dir*(dx+dz);
        Vec2i start = build.BoundingWall[wallIndex];

        Vec2i cur = start + dPos;

        //If the building has an entrance, we iterate all window points
        //to make sure we aren't breaking a door
        if (build.Entrance != null)
        {

            //If we are too close to the door
            if(cur.QuickDistance(build.Entrance) < (size+1) * (size+1)){
                if (autoReattempt)
                    return AddWindow(genRan, vox, build, wp1, size, height, autoReattempt);
                return false;
            }
            else
            {
                for (int i = 0; i < size; i++)
                {
                    cur = start + dPos + (dir * i);
                    if (cur.x > 0 && cur.z > 0 && cur.x < build.Width && cur.z < build.Height)
                    {
                        if (vox.GetVoxel(cur.x, 2, cur.z) == Voxel.glass)
                        {
                            if (autoReattempt)
                                return AddWindow(genRan, vox, build, wp1, size, height, autoReattempt);
                            return false;
                        }
                    }
                    else
                    {
                        if (autoReattempt)
                            return AddWindow(genRan, vox, build, wp1, size, height, autoReattempt);
                        return false;
                    }
                    
                    

                }
            }

        }
        cur = start + dPos;

        Vector3 position = (cur - dir).AsVector3() + Vector3.up*1.75f;
        Vector3 scale = new Vector3(0.5f, 2, -(dir.x + dir.z)*size);
        float rotation = Mathf.Acos(dir.z)*Mathf.Rad2Deg;
        GlassWindow window = new GlassWindow(position, scale, rotation);
        build.AddObjectReference(window);
        
        for (int i = 0; i < size; i++)
        {
            
            for (int y=2; y<2+height; y++)
            {

                vox.SetVoxel(cur.x, y, cur.z, Voxel.none);
            }
            
        }

       

        return true;
    }

    public static void ChooseEntrancePoint(GenerationRandom genRan, BuildingVoxels vox, Building build)
    {


        int doorIndex = genRan.RandomInt(0, build.BoundingWall.Length);
        int dp1 = (doorIndex + 1) % build.BoundingWall.Length;


        Vec2i dif = build.BoundingWall[dp1] - build.BoundingWall[doorIndex];
        Vec2i entr = build.BoundingWall[doorIndex] + dif / 2;
        build.SetEntrancePoint(entr);

        for(int y=0; y<3; y++)
        {
            vox.SetVoxel(entr.x, y, entr.z, Voxel.none);
        }

    }

    public static void ConnectBoundingWall(BuildingVoxels vox, Vec2i[] bounds, Voxel voxel, int wallHeight=5)
    {


        for(int i=0; i<bounds.Length; i++)
        {
            Vec2i a = bounds[i];
            Vec2i b = bounds[(i + 1) % bounds.Length];
            int x = a.x;
            int z = a.z;

            int xDir = (int)Mathf.Sign(b.x - a.x);
            int zDir = (int)Mathf.Sign(b.z - a.z);

            while (!(x == b.x && z == b.z))
            {
                for(int y=0; y<wallHeight; y++)
                {
                    vox.SetVoxel(x, y, z, voxel);
                }
                
                //data[x, z] = copy.Copy(globalPos + new Vec2i(x, z));



                int dx = (int)Mathf.Abs(b.x - x);
                int dz = (int)Mathf.Abs(b.z - z);

                if (dx > dz)
                {
                    x += xDir;
                }
                else if (dz > dx)
                {
                    z += zDir;

                }
                else
                {
                    x += xDir;
                    z += zDir;
                }

            }
        }


    }
    //public static void ChooseRandomEntrance()
    public static void BuildBoundingWallRect(BuildingVoxels vox, int width, int depth, int wallHeight, Voxel wallVox)
    {
        for(int y=0; y<wallHeight; y++)
        {

            for(int x=0; x<width; x++)
            {
                vox.SetVoxel(x, y, 0, wallVox);
                vox.SetVoxel(x, y, depth-1, wallVox);
            }
            for(int z=0; z<depth; z++)
            {
                vox.SetVoxel(0, y, z, wallVox);
                vox.SetVoxel(width - 1, y, z, wallVox);
            }


        }
    }

    /// <summary>
    /// Sets all tiles in 'tileMap' enclosed by the region (x,z)->(x+width,z+depth)
    /// to the tile specified by 'toPlace'
    /// </summary>
    public static void SetTiles(Tile[,] tileMap, int x, int z, int width, int depth, Tile toPlace)
    {
        for(int x_=x; x_<width+x; x_++)
        {
            for(int z_=z; z_<z+depth; z_++)
            {
                tileMap[x_, z_] = toPlace;
            }
        }
    }


    public static bool AddObject(Building build, BuildingVoxels vox, WorldObjectData obj, bool force=false)
    {

        if (force)
        {
            build.AddObjectReference(obj);
            return true;
        }
        else
        {
            //Iterate all objects in the building and check for intersection.
            foreach(WorldObjectData obj_ in build.GetBuildingObjects())
            {
                //If they intersect, then we cannot place this object.
                if (obj_.Intersects(obj))
                    return false;
            }

            //Find the integer bounds
            Recti bounds = obj.CalculateIntegerBounds();
            int yMin = (int)obj.Position.y;
            int yMax = yMin + (int)obj.Size.y;
            //Iterate the voxel position of the object bounds.
            for(int x=bounds.X; x<bounds.X + bounds.Width; x++)
            {
                for(int z=bounds.Y; z<bounds.Y + bounds.Height; z++)
                {
                    for(int y=yMin; y<yMax; y++)
                    {
                        //If any single voxel is non-none, then we cannot place the object here.
                        if (vox.GetVoxel(x, y, z) != Voxel.none)
                            return false;
                    }
                }
            }
            build.AddObjectReference(obj);
            return true;

        }

    }

}