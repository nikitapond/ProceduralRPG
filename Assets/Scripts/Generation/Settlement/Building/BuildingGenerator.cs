using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class BuildingGenerator
{
    /// <summary>
    /// Used to define how a building will look
    /// </summary>
    public struct BuildingStyle
    {
        public int test1;
        public bool test2;
    }

    public static readonly BuildingStyle DEFAULT = new BuildingStyle() { test1 = 1, test2 = true };

    public struct BuildingGenerationPlan
    {
        public BuildingPlan BuildingPlan;
        public Vec2i EntranceSide;
        public int MaxWidth;
        public int MaxHeight;
        public Vec2i DesiredSize;
    }


    public const int NORTH_ENTRANCE = 0;
    public const int EAST_ENTRANCE = 1;
    public const int SOUTH_ENTRANCE = 2;
    public const int WEST_ENTRANCE = 3;


    public static Building CreateBuilding(GenerationRandom genRan, out BuildingVoxels vox, BuildingGenerationPlan plan, BuildingStyle style = default)
    {
        //If default, set the correct default
        if (style.Equals(default(BuildingStyle)))
            style = DEFAULT;

        int maxWidth = plan.DesiredSize == null?Mathf.Min(plan.BuildingPlan.MaxSize, plan.MaxWidth): plan.DesiredSize.x;
        int maxHeight = plan.DesiredSize==null? Mathf.Min(plan.BuildingPlan.MaxSize, plan.MaxHeight) : plan.DesiredSize.z;
        int width = genRan.RandomInt(plan.BuildingPlan.MinSize, maxWidth);
        int height = genRan.RandomInt(plan.BuildingPlan.MinSize, maxHeight);

        if (plan.BuildingPlan == Building.BLACKSMITH)
        {
            Blacksmith smith = BlacksmithGenerator.GenerateBlacksmith(genRan, new Blacksmith(width, height), out vox);
            return smith;
        }
        if (plan.BuildingPlan == Building.BARACKS)
        {
            Barracks barr = BarracksGenerator.GenerateBarracks(genRan, new Barracks(width, height), out vox);
            return barr;
        }
        if (plan.BuildingPlan == Building.TAVERN)
        {
            return TavernGenerator.GenerateTavern(genRan, new Tavern(width, height), out vox);
        }

        House house = HouseGenerator.GenerateHouse(genRan, new House(width, height), out vox);
        return house;
        //return GenerateHouse(out vox, width, height);
    }


    /// <summary>
    /// Takes a building plan and generates a full building from it.
    /// TODO - Currently messy, fix
    /// </summary>
    /// <param name="plan"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="entrance"></param>
    /// <returns></returns>
    public static Building CreateBuilding(GenerationRandom genRan, out BuildingVoxels vox, BuildingPlan plan, Vec2i entrance=null, int width=-1, int height=-1)
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
            Blacksmith smith = BlacksmithGenerator.GenerateBlacksmith(genRan, new Blacksmith(width, height), out vox);
            return smith;
        }
        if(plan == Building.BARACKS)
        {
            Barracks barr = BarracksGenerator.GenerateBarracks(genRan, new Barracks(width, height), out vox);
            return barr;
        }
        if(plan == Building.TAVERN)
        {
            return TavernGenerator.GenerateTavern(genRan, new Tavern(width, height), out vox);
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
        House house = HouseGenerator.GenerateHouse(genRan, new House(width, height), out vox);
        return house;
        //return GenerateHouse(out vox, width, height);
        
    }


    protected static int WorldToChunk(int w)
    {
        return Mathf.FloorToInt((float)w / World.ChunkSize);
    }

    public static Vec2i GetWallPointDirection(Building build, Vec2i wallPoint)
    {
        int x = build.Width / 2 - wallPoint.x;
        int z = build.Height / 2 - wallPoint.z;
        if(Mathf.Abs(x) > Mathf.Abs(z))
        {
            //Debug.Log("x dir");
            return new Vec2i((int)Mathf.Sign(x), 0);
        }
        //Debug.Log("z dir");
        return new Vec2i(0, (int)Mathf.Sign(z));
    }

    public static Vec2i[] FindInsideWallBoundryPoints(BuildingVoxels vox, Building build, Tile groundTile = null)
    {
        Vec2i[] dirs = new Vec2i[] { new Vec2i(-1, 0), new Vec2i(1, 0), new Vec2i(0, 1), new Vec2i(0, -1) };
        List<Vec2i> wallPoints = new List<Vec2i>();
        for(int x=1; x<build.Width-1; x++)
        {
            for(int z=1; z<build.Height-1; z++)
            {
                //If the current tile is not defined as a ground tile, we do not include this point
                if (groundTile != null && build.BuildingTiles[x, z] != groundTile)
                    continue;

                for(int i=0; i<4; i++)
                {
                    int x_ = dirs[i].x + x;
                    int z_ = dirs[i].z + z;
                    Vec2i v_ = new Vec2i(x, z);
                    if (vox.GetVoxel(x_,0, z_) != Voxel.none && !wallPoints.Contains(v_))
                        wallPoints.Add(v_);
                }
                
                    
            }
        }
        return wallPoints.ToArray();

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genRan">The RNG used for this building</param>
    /// <param name="obj">The object to place in the building</param>
    /// <param name="height">The height above the ground the object should sit</param>
    /// <param name="vox">The voxel data of this building. Only required if 'requireWallBacking' is true</param>
    /// <param name="build">The building to place the object in</param>
    /// <param name="distFromWall">The distance from a wall the object should be placed</param>
    /// <param name="wallIndex">Defines the wall we should attempt to place this object on. The 'i'th wall defines the wall 
    /// connecting build.BoundingWall[i] -> build.BoundingWall[i+1]. If left as default value (-1), an index will be randomly generated</param>
    /// <param name="requireWallBacking">If true, then we will search for a point on the wall that is backed against a wall. 
    /// Used to stop certain objects being placed in front of windows</param>
    /// <param name="forcePlace">If true, then we do not check for intersection with other objects in the building</param>
    /// <param name="attemptsCount">The number of attempts made to place the object on this wall</param>
    /// <returns>True if object is placed sucesfully, false otherwise</returns>
    public static bool PlaceObjectAgainstWall(GenerationRandom genRan, WorldObjectData obj, float height, 
        BuildingVoxels vox, Building build, float distFromWall=0, bool requireWallBacking=false, int distToEntr = 2,
        bool forcePlace = false, int attemptsCount=5)
    {

        if (build.InsideWallPoints == null)
            build.InsideWallPoints = FindInsideWallBoundryPoints(vox, build);

        //Check attempts count
        attemptsCount--;
        if (attemptsCount == 0)
            return false;
        
        float wallDisp = genRan.Random();
        //Define the position as the start pos + lambda*dir
        //Select a random position from all wall points
        Vec2i pos = genRan.RandomFromArray(build.InsideWallPoints);
        
        //If too close to entrance, try another placement
        if (build.Entrance != null && pos.QuickDistance(build.Entrance) < distToEntr* distToEntr)
            return PlaceObjectAgainstWall(genRan, obj, height, vox, build, distFromWall, requireWallBacking, distToEntr, forcePlace, attemptsCount);
        //If the object is too close to any of the corners, try again
        foreach(Vec2i v in build.BoundingWall)
            if(v.QuickDistance(pos) < 2)
                return PlaceObjectAgainstWall(genRan, obj, height, vox, build, distFromWall, requireWallBacking, distToEntr, forcePlace, attemptsCount);
        
        Vec2i faceDirection = GetWallPointDirection(build, pos);

        //If we require a backing wall, we check the position 
        if (requireWallBacking)
        {
            Vec2i wPos = pos - faceDirection;
            for (int y = 0; y < 4; y++)
            {
                VoxelNode vn = vox.GetVoxelNode(wPos.x, y, wPos.z);
                //If there is no node (nothing set) then this position is not valid
                if (!vn.IsNode)
                    return PlaceObjectAgainstWall(genRan, obj, height, vox, build, distFromWall, requireWallBacking, distToEntr, forcePlace, attemptsCount);
                if(vn.Voxel == Voxel.glass || vn.Voxel == Voxel.none)
                    return PlaceObjectAgainstWall(genRan, obj, height, vox, build, distFromWall, requireWallBacking, distToEntr, forcePlace, attemptsCount);
            }
        }
        Vector3 finPos = pos.AsVector3() + faceDirection.AsVector3() * distFromWall + Vector3.up*height;
        float rotation = Vec2i.Angle(Vec2i.Forward, faceDirection);
        obj.SetPosition(finPos).SetRotation(rotation);
        if (!forcePlace)
        {
            foreach (WorldObjectData obj_ in build.GetBuildingExternalObjects())
            {
                if (obj.Intersects(obj_))
                    return PlaceObjectAgainstWall(genRan, obj, height, vox, build, distFromWall, requireWallBacking, distToEntr, forcePlace, attemptsCount);
            }
        }


        //obj.SetPosition(finPos);
        build.AddInternalObject(obj);
        return true;
    }

    /// <summary>
    /// Places a roof over the specified building.
    /// The roof will only cover the tile specified by 'roofTileID'. If this is null, 
    /// then a roof over the total building will be created
    /// </summary>
    /// <param name="vox"></param>
    /// <param name="build"></param>
    /// <param name="roofTileID"></param>
    public static void AddRoof(GenerationRandom genRan, BuildingVoxels vox, Building build, Voxel roofVox, int roofStartY=5, Tile roofTileReplace=null)
    {
        if(roofTileReplace == null)
        {
            Vec2i dir = genRan.RandomQuadDirection();
            dir.x = Mathf.Abs(dir.x);
            dir.z = Mathf.Abs(dir.z);

            Vec2i start = new Vec2i(build.Width / 2 * dir.x, build.Height / 2 * dir.z);
            Vec2i end = start + new Vec2i(dir.z*build.Width, dir.x*build.Height);
            LineI t = new LineI(start, end);
            float maxDist = Mathf.Max(t.Distance(new Vec2i(0, 0)), t.Distance(new Vec2i(build.Width - 1, 0)),
                                      t.Distance(new Vec2i(0, build.Height - 1)), t.Distance(new Vec2i(build.Width - 1, build.Height - 1)));

            int maxHeight = genRan.RandomInt(4, 6);
            float scale = maxHeight / (maxDist + 1);
            //float scale = 0.7f;

            for (int x=0; x<build.Width; x++)
            {
                for(int z=0; z<build.Height; z++)
                {
       
                    int height = (int)Mathf.Clamp((maxDist - t.Distance(new Vec2i(x, z)) + 1)*scale, 1, World.ChunkHeight - 1);
                    for(int y=0; y<height; y++)
                    {
                        vox.AddVoxel(x, Mathf.Clamp(roofStartY+y, 0, World.ChunkHeight-1), z, roofVox);
                    }
                    
                  
                }
            }
        }
    }

    public static bool AddWindow(GenerationRandom genRan, BuildingVoxels vox, Building build, int wallIndex = -1, int size = 2, int height=1,bool autoReattempt = true, int reattemptCount=3)
    {
        reattemptCount--;
        if (reattemptCount == 0)
            return false;
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
                return AddWindow(genRan, vox, build, wp1, size, height, autoReattempt, reattemptCount);
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
                    return AddWindow(genRan, vox, build, wp1, size, height, autoReattempt, reattemptCount);
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
                                return AddWindow(genRan, vox, build, wp1, size, height, autoReattempt, reattemptCount);
                            return false;
                        }
                    }
                    else
                    {
                        if (autoReattempt)
                            return AddWindow(genRan, vox, build, wp1, size, height, autoReattempt, reattemptCount);
                        return false;
                    }
                    
                    

                }
            }

        }
        cur = start + dPos;

        Vector3 position = (cur - dir).AsVector3() + Vector3.up;
        Vector3 scale = new Vector3(0.5f, 2, (dir.x + dir.z)*size);
        float rotation = Mathf.Acos(dir.z)*Mathf.Rad2Deg;
        //GlassWindow window = new GlassWindow(position, scale, rotation);
        //build.AddObjectReference(window);
        
        for (int i = -1; i <= size; i++)
        {

            cur = start + dPos + dir * i;

            for (int y=1; y<=2+height; y++)
            {
                if(i<0 || i>=size || y==1 || y == 2 + height)
                {
                    vox.AddVoxel(cur.x, y, cur.z, Voxel.glass);
                }
                else
                {
                    vox.ClearVoxel(cur.x, y, cur.z);
                    vox.SetVoxel(cur.x, y, cur.z, Voxel.glass);
                }
                
                //vox.Add(cur.x, y, cur.z, Voxel.none);
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
        Vec2i delta = new Vec2i(dif.x == 0 ? 0 : (int)Mathf.Sign(dif.x), dif.z == 0 ? 0 : (int)Mathf.Sign(dif.z));
        build.SetEntrancePoint(entr);
        //Debug.Log("entr delta " + delta);
        for(int y=0; y<4; y++)
        {
            vox.ClearVoxel(entr.x, y, entr.z);
        }
        Door interior = new Door();
        interior.SetPosition(entr);
        
        build.InternalEntranceObject = interior;
        AddObject(build, vox, interior, true);

        Door external = new Door();
        external.SetPosition(entr);
        build.ExternalEntranceObject = external;
        build.AddExternalObject(external);

    }




    public static void ConnectBoundingWall(BuildingVoxels vox, Vec2i[] bounds, Voxel voxel, int wallHeight=6)
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
                vox.SetVoxel(width-1, y, z, wallVox);
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
            build.AddInternalObject(obj);
            return true;
        }
        else
        {
            //Iterate all objects in the building and check for intersection.
            foreach(WorldObjectData obj_ in build.GetBuildingInternalObjects())
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
            build.AddInternalObject(obj);
            return true;

        }

    }

}

public class BuildingSubworldBuilder
{

    private Vec2i ChunkSize;
    private Vec2i Size;
    private int[,][,] Tiles;
    private List<WorldObjectData>[,] Objects;
    private ChunkVoxelData[,] Voxels;

    private Building Building;
    private BuildingVoxels BuildVox;
    public BuildingSubworldBuilder(Building building, BuildingVoxels vox)
    {
        Building = building;
        BuildVox = vox;
        Size = new Vec2i(building.Width, building.Height);
        int cWidth = Mathf.CeilToInt(((float)Size.x) / World.ChunkSize);
        int cHeight = Mathf.CeilToInt(((float)Size.z) / World.ChunkSize);
        if (cWidth < 1)
            cWidth = 1;
        if (cHeight < 1)
            cHeight = 1;
        ChunkSize = new Vec2i(cWidth, cHeight);

        Tiles = new int[cWidth, cHeight][,];
        Voxels = new ChunkVoxelData[cWidth, cHeight];
        Objects = new List<WorldObjectData>[cWidth, cHeight];
        for (int x=0; x<cWidth; x++)
        {
            for(int z=0; z<cHeight; z++)
            {
                Tiles[x, z] = new int[World.ChunkSize, World.ChunkSize];
                Voxels[x, z] = new ChunkVoxelData();
            }
        }

    }

    public void CreateSubworld()
    {

        for(int x=0; x<Size.x; x++)
        {
            for(int z=0; z<Size.z; z++)
            {
                int cx = WorldToChunk(x);
                int cz = WorldToChunk(z);
                int tx = x % World.ChunkSize;
                int tz = z % World.ChunkSize;

                Tiles[cx, cz][tx, tz] = Building.BuildingTiles[x, z]==null?0: Building.BuildingTiles[x, z].ID;
                for(int y=0; y< BuildVox.Height; y++)
                {
                    Voxels[cx, cz].SetVoxelNode(tx, y, tz, BuildVox.GetVoxelNode(x, y, z));
                }               
            }
        }
        foreach(WorldObjectData obj in Building.GetBuildingInternalObjects())
        {
            Vec2i v = Vec2i.FromVector3(obj.Position);
            int cx = WorldToChunk(v.x);
            int cz = WorldToChunk(v.z);
            if (Objects[cx, cz] == null)
                Objects[cx, cz] = new List<WorldObjectData>();
            //obj.SetPosition(obj.Position.Mod(World.ChunkSize));
            //Debug.Log(obj.Position);
            Objects[cx, cz].Add(obj);
        }
        ChunkData[,] chunks = new ChunkData[ChunkSize.x, ChunkSize.z];
        for(int x=0; x<ChunkSize.x; x++)
        {
            for (int z = 0; z < ChunkSize.z; z++)
            {
                chunks[x,z] = new ChunkData(x, z, Tiles[x, z], true, 0, null, Objects[x, z]);
                chunks[x, z].SetVoxelData(Voxels[x, z]);
            }
        }
        Subworld sw = new Subworld(chunks, Building.Entrance, Building.Entrance);
        Building.BuildingSubworld = sw;
    }
    protected static int WorldToChunk(int w)
    {
        return Mathf.FloorToInt((float)w / World.ChunkSize);
    }

}