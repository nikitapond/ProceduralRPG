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
    public static Building CreateBuilding(BuildingPlan plan, int width=-1, int height=-1, int entrance = 0)
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

        Tile[,] buildingBase = new Tile[width, height];
        WorldObjectData[,] buildingObjects = new WorldObjectData[width, height];

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

        //Default for now
        House ht = new House(width, height);
        ht.SetBuilding(buildingBase, buildingObjects);
        return GenerateHouse(ht, BuildingStyle.stone);
        
    }

    /// <summary>
    /// Generates the basic walls, floor, and entrance for any building.
    /// Picks the exact position of entrance, and returns it
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="buildingObjects">Building objects to be filled with walls</param>
    /// <param name="buildTiles"></param>
    /// <param name="entranceID"></param>
    /// <param name="style"></param>
    /// <returns></returns>
    public static Vec2i GenerateWallsFloorAndEntrance(int width, int height, WorldObjectData[,] buildingObjects, 
        Tile[,] buildTiles, int entranceID, BuildingStyle style, int entraceDis=-1, WorldObjectData wallType =null, Tile tileType=null)
    {

        int entWidth = 2;
        if(entraceDis == -1)
        {
            if (entranceID == NORTH_ENTRANCE || entranceID == SOUTH_ENTRANCE)
                entraceDis = MiscMaths.RandomRange(1, width - 1);
            else
                entraceDis = MiscMaths.RandomRange(1, height - 1);
        }

        //Decide the position of the entrance
        Vec2i entrance = null;
        if (entranceID == NORTH_ENTRANCE)
            entrance = new Vec2i(entraceDis, height-1);
        else if (entranceID == SOUTH_ENTRANCE)
            entrance = new Vec2i(entraceDis, 0);
        else if (entranceID == EAST_ENTRANCE)
            entrance = new Vec2i(width-1, entraceDis);
        else if (entranceID == WEST_ENTRANCE)
            entrance = new Vec2i(0, entraceDis);
        
        
        //Assign correct wall type if none given
        if(wallType == null)
        {
            switch (style)
            {
                case BuildingStyle.stone:
                    wallType = new BrickWall(new Vec2i(0,0));
                    break;
                case BuildingStyle.wood:
                    //TODO - Add
                    wallType = new BrickWall(new Vec2i(0, 0));
                    break;
            }
        }//Asign correct tile type if none given
        if(tileType == null)
        {
            switch (style)
            {
                case BuildingStyle.stone:
                    tileType = Tile.STONE_FLOOR;
                    break;
                case BuildingStyle.wood:
                    tileType = Tile.WOOD_FLOOR;
                    break;
            }
        }
        //Iterate all points
        for(int x=0; x<width; x++)
        {
            for(int z=0; z < height; z++)
            {
                //Asign tile
                buildTiles[x, z] = tileType;
                if(x==0 || x==width-1 || z==0 || z == height - 1)
                {
                    //Asign wall if no entrance
                    if (!(entrance.x == x & entrance.z == z))
                    {
                        if (x == 3)
                        {
                            buildingObjects[x, z] = new BuildingWall(new Vec2i(0, 0), "brick", 5,
                                new GlassWindow(new Vec2i(0, 0), new Vec2i(0, 1)), 2);
                        }
                        else
                        {
                            buildingObjects[x, z] = new BuildingWall(new Vec2i(0,-1), "brick", 5);
                        }
                        
                    }
                        
                }
            }
        }
        //(buildingObjects[0, 0] as BuildingWall).AddRoof(new Roof(new Vec2i(0, 0), new Vec2i(width, height)));

        return entrance;
    }

    /// <summary>
    /// Adds given object to the array of building objects. 
    /// Checks if the designated spot is already filled, if so we do not place the object and returns false.
    /// If the object covers a single tile, we add it and return true
    /// If the object is multi tile, we check if it is placed within bounds, and if every child tile is free.
    /// if so, we return true, otherwise we return false and do not place the object
    /// </summary>
    /// <param name="current"></param>
    /// <param name="nObj"></param>
    /// <param name="x"></param>
    /// <param name="z"></param>
    public static bool AddObject(Building building, WorldObjectData nObj, int x, int z, bool nInstance=false)
    {
        //If not in bounds, return false
        if(!(x > 0 && x<building.Width && z>0 && z < building.Height))
        {
            return false;
        }
        //Check if this is a single tile object
        if(!(nObj is IMultiTileObject))
        {
            //Single tile objects, we only need to check the single tile
            if(building.BuildingObjects[x,z] == null)
            {
                //If the tile is empty, add object and return true
                building.BuildingObjects[x, z] = nObj;
                building.AddObjectReference(nObj);
                return true;
            }
            //If the tile is taken, we don't place the object
            return false;
            
        }
        if(!(x+nObj.Size.x < building.Width && z+nObj.Size.z < building.Height))
        {
            //If the bounds of the multiu tile object are too big, return false;
            return false;
        }
        //For multi tile objects, we iterate the whole size
        for(int i=0; i<nObj.Size.x; i++)
        {
            for (int j = 0; j < nObj.Size.z; j++)
            {
                //if any of the tiles is not null, we don't place and return false
                if (building.BuildingObjects[x + i, z + j] != null)
                    return false;
            }
        }
        //Set reference, then get chilren
        building.AddObjectReference(nObj);
        building.BuildingObjects[x, z] = nObj;
        IMultiTileObjectChild[,] children = (nObj as IMultiTileObject).GetChildren();
        //Iterate again to set children
        for (int i = 0; i < nObj.Size.x; i++)
        {
            for (int j = 0; j < nObj.Size.z; j++)
            {
                if (i == 0 && j == 0)
                    continue;

                //if any of the tiles is not null, we don't place and return false
                building.BuildingObjects[x + i, z + j] = (children[i, j] as WorldObjectData);
                    
            }
        }

        return true;
    }

    public static Vec2i AddEntrance(Building b, int entranceSide, int disp = -1)
    {

        if(disp == -1)
        {
            //If entrance is facing z axis, displacement in in x
            if (entranceSide == NORTH_ENTRANCE || entranceSide == SOUTH_ENTRANCE)
                disp = MiscMaths.RandomRange(1, b.Width - 1);
            else
                disp = MiscMaths.RandomRange(1, b.Height - 1);
        }
        
        Vec2i entrance = null;
        if (entranceSide == NORTH_ENTRANCE)
            entrance = new Vec2i(disp, b.Height - 1);
        else if (entranceSide == SOUTH_ENTRANCE)
            entrance = new Vec2i(disp, 0);
        else if (entranceSide == EAST_ENTRANCE)
            entrance = new Vec2i(b.Width - 1, disp);
        else if (entranceSide == WEST_ENTRANCE)
            entrance = new Vec2i(0, disp);
        b.BuildingObjects[entrance.x, entrance.z] = null; //TODO - Set to door of correct type
        
        return entrance;
    }

    public static House GenerateHouse(Building building, BuildingStyle style, WorldObject wallType = null, Tile tileType = null)
    {
        int width = building.Width;
        int height = building.Height;
        Vec2i entr = GenerateWallsFloorAndEntrance(width, height, building.BuildingObjects, building.BuildingTiles, 0, BuildingStyle.stone, tileType:Tile.TEST_RED);
        //AddObject(building.BuildingObjects, WorldObject.BED, 2, 3);
        building.SetEntrancePoint(entr);
        return building as House;
    }
    
    public static Blacksmith GenerateBlacksmith(Blacksmith building, BuildingStyle style = BuildingStyle.stone)
    {
        int width = building.Width;
        int height = building.Height;
        Vec2i entr = GenerateWallsFloorAndEntrance(width, height, building.BuildingObjects, building.BuildingTiles, 0, style, tileType:Tile.TEST_MAGENTA);
        building.SetEntrancePoint(entr);
        WorkEquiptmentData anvil = new Anvil(new Vec2i(1, 3));
        WorkEquiptmentData forge = new Anvil(new Vec2i(4,4));
        AddObject(building, anvil, 1,3);
        AddObject(building, forge, 4, 4);

        NPCJob[] jobs = new NPCJob[] { new NPCJobBlackSmith(building), 
                                       new NPCJobBlackSmith(building), 
                                       new NPCJobBlackSmith(building) };

        building.SetWorkBuildingData(new WorkBuildingData(jobs));
        AddEntrance(building, 0);
        //building.WorkEquiptment.Add(new WorkEquiptmentPlacement(anvil, new Vec2i(1, 3)));
        //building.WorkEquiptment.Add(new WorkEquiptmentPlacement(forge, new Vec2i(4, 4)));
        return building;
    }

    public static MarketPlace GenerateMarket(MarketPlace building, BuildingStyle style = BuildingStyle.stone)
    {
        int width = building.Width;
        int height = building.Height;
        Vec2i entr = GenerateWallsFloorAndEntrance(width, height, building.BuildingObjects, building.BuildingTiles, 0, style, tileType: Tile.TEST_YELLOW);
        building.SetEntrancePoint(entr);
        MarketStall s1 = new MarketStall(new Vec2i(3, 3));
        MarketStall s2 = new MarketStall(new Vec2i(building.Width - 3, building.Height - 3));
        AddObject(building, s1, 3, 3);
        AddObject(building, s2, building.Width - 3, building.Height - 3);
        NPCJob[] jobs = new NPCJob[] { new NPCJobMarketStall("Market runner", building, s1), new NPCJobMarketStall("Market runner", building, s2) };
        building.SetWorkBuildingData(new WorkBuildingData(jobs));

        return building;
    }

    public static Baracks GenerateBaracks(Baracks building, BuildingStyle style = BuildingStyle.stone)
    {

        int width = building.Width;
        int height = building.Height;

        Vec2i entr = GenerateWallsFloorAndEntrance(width, height, building.BuildingObjects, building.BuildingTiles, 0, style, tileType: Tile.TEST_PURPLE);
        building.SetEntrancePoint(entr);
        List<NPCJob> jobs = new List<NPCJob>();
        for(int x=2; x<building.Width; x+=3)
        {
            AddObject(building, new Bed(new Vec2i(2, 2)), x, 2);
            jobs.Add(new NPCJobSoldier(building));
        }
        NPCJob[] jobs_ = jobs.ToArray();
        building.SetWorkBuildingData(new WorkBuildingData(jobs_));

        return building;
    }

    /// <summary>
    /// Creates a castle, size must be 48x48
    /// </summary>
    /// <returns></returns>
    public static Castle GenerateCastle(int size)
    {
        Tile[,] buildingBase = new Tile[size, size];
        WorldObjectData[,] buildingObjects = new WorldObjectData[size, size];
        Castle c = new Castle(size, size);
        c.SetBuilding(buildingBase, buildingObjects);
        //c.Entrances.Add(new BuildingEntrance(new Vec2i(1, size / 2 - 2), new Vec2i(1, size / 2 + 2)));
        Vec2i entr = GenerateWallsFloorAndEntrance(size, size, c.BuildingObjects, c.BuildingTiles, 0, BuildingStyle.stone, entraceDis:size/2);
        
        
        AddEntrance(c, 0, entr.x - 1);
        AddEntrance(c, 0, entr.x - 2);

        AddEntrance(c, 0, entr.x + 1);
        AddEntrance(c, 0, entr.x + 2);

        return c;
    }

}