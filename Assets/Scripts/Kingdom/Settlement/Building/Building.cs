﻿using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BuildingPlan
{
    public string Name { get; private set; }
    public int MinSize { get; private set; }
    public int MaxSize { get; private set; }
    public BuildingPlan(string buildingName, int minSize, int maxSize)
    {
        Name = buildingName;
        MinSize = minSize;
        MaxSize = maxSize;
    }

    public override bool Equals(object obj)
    {
        if (obj == null)
            return false;
        BuildingPlan bp = obj as BuildingPlan;
        if (bp == null)
            return false;
        return bp.Name == Name;
        
    }
    public static bool operator ==(BuildingPlan a, BuildingPlan b)
    {
        if (System.Object.ReferenceEquals(a, null))
        {
            if (System.Object.ReferenceEquals(b, null))
            {
                return true;
            }
            return false;
        }
        return a.Equals(b);
    }
    public static bool operator !=(BuildingPlan a, BuildingPlan b)
    {
        return !(a == b);
    }
}
[System.Serializable]
public abstract class Building
{

    public static BuildingPlan VILLAGEHALL = VillageHall.BuildingPlan;
    public static BuildingPlan HOLD = Hold.BuildingPlan;
    public static BuildingPlan CITYCASTLE = Castle.BuildingPlanSmall;
    public static BuildingPlan CAPTIALCASTLE = Castle.BuildingPlanBig;

    public static BuildingPlan BLACKSMITH = Blacksmith.BuildingPlan;
    public static BuildingPlan MARKET = MarketPlace.BuildingPlan;
    public static BuildingPlan HOUSE = House.BuildingPlan;
    public static BuildingPlan BARACKS = Barracks.BuildingPlanCity;
    public static BuildingPlan TAVERN = Tavern.BuildingPlan;

    public static BuildingPlan WHEATFARM = Farm.WheatFarm;
    public static BuildingPlan SILKFARM = Farm.SilkFarm;
    public static BuildingPlan VEGFARM = Farm.VegFarm;
    public static BuildingPlan CATTLEFARM = Farm.CattleFarm;
    public static BuildingPlan SHEEPFARM = Farm.SheepFarm;

    public static BuildingPlan WOODCUTTER = WoodCutter.BuildingPlan;

    public static BuildingPlan IRONMINE = Mine.IronMine;
    public static BuildingPlan SILVERMINE = Mine.SilverMine;
    public static BuildingPlan GOLDMINE = Mine.GoldMine;

    public static BuildingPlan BAKERY = Bakery.BuildingPlan;
    public static BuildingPlan LUMBERMILL = LumberMill.BuildingPlan;
    public static BuildingPlan SMELTER = Smelter.BuildingPlan;
    public int SettlementID { get; private set; }

    public Inventory Inventory { get; private set; }

    public Tile[,] BuildingTiles { get; private set; }
    private List<WorldObjectData> InternalObjects;
    private List<WorldObjectData> ExternalObjects;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public float BaseValue { get { return Width * Height; } }
    public float ValueModifier { get; private set; }
    public float Value { get { return BaseValue * ValueModifier; } }

    public Subworld BuildingSubworld;

    public bool HasSubworld { get { return BuildingSubworld != null; } }

    public ISubworldEntranceObject ExternalEntranceObject;
    public ISubworldEntranceObject InternalEntranceObject;

    public Vec2i[] BoundingWall;

    public Vec2i[] InsideWallPoints;

    

    public Vec2i WorldPosition { get; private set; }

    private Recti WorldBounds;
    private List<Vec2i> SpawnableTiles;


    public Vec2i Entrance { get; private set; }
    public Building(int width, int height, Vec2i[] boundingWall=null)
    {
        Width = width;
        Height = height;
        Inventory = new Inventory();
        BuildingTiles = new Tile[width, height];
        InternalObjects = new List<WorldObjectData>();
        ExternalObjects = new List<WorldObjectData>();
        if (boundingWall == null)
        {
            BoundingWall = new Vec2i[] { new Vec2i(0, 0), new Vec2i(width-1, 0), new Vec2i(width-1, height-1), new Vec2i(0, height-1) };
        }
        else
        {
            BoundingWall = boundingWall;
        }
    }
    /// <summary>
    /// Called after the building has been added to the world.
    /// Clears all objects in building from memory, as they are now stored
    /// in ChunkData 
    /// </summary>
    public void AfterApplyToWorld()
    {
        BuildingTiles = null;
        InternalObjects = null;
    }

    /// <summary>
    /// Sets the global position of this building based on the position of the settlement
    /// in the world, as well as this buildings location within the settlement
    /// </summary>
    /// <param name="settlementBaseCoord">The base tile of the settlement in global coords</param>
    /// <param name="buildingSettlementCoord">The coordinate of this building in the settlement</param>
    public void SetPositions(Vec2i settlementBaseCoord, Vec2i buildingSettlementCoord)
    {
        WorldPosition = settlementBaseCoord + buildingSettlementCoord;
        Entrance = WorldPosition;
        if (BuildingSubworld != null)
        {

            BuildingSubworld.Entrance = ExternalEntranceObject;
            BuildingSubworld.Exit = InternalEntranceObject;
            BuildingSubworld.SetExternalEntrancePos(WorldPosition + BuildingSubworld.ExternalEntrancePos);
            

            ExternalEntranceObject.SetSubworld(BuildingSubworld);
            InternalEntranceObject.SetSubworld(BuildingSubworld);
        }    

      
        

    }

    public virtual TaskLocation GetBuildingGotoTaskLocation()
    {
        if (HasSubworld)
        {
            return new TaskLocation(BuildingSubworld, BuildingSubworld.InternalEntrancePos);
        }
        else
            return new TaskLocation(-1, GenerationRandom.RNG.RandomFromList(SpawnableTiles));
    }


    public List<WorldObjectData> GetBuildingExternalObjects()
    {
        return ExternalObjects;
    }
    public List<WorldObjectData> GetBuildingInternalObjects()
    {
        return InternalObjects;
    }

    public bool ObjectIntersects(WorldObjectData obj)
    {
        foreach(WorldObjectData obj_ in InternalObjects)
        {
            if (obj.Intersects(obj_))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Adds the specified WorldObject to the list containing
    /// all objects in this building.
    /// WARNING - this does not add the object to the array of objects itself
    /// </summary>
    public void AddInternalObject(WorldObjectData obj) {
        InternalObjects.Add(obj);
    }

    public void AddExternalObject(WorldObjectData obj)
    {
        ExternalObjects.Add(obj);
    }

    public void SetValueModifier(float v)
    {
        ValueModifier = v;
    }
    public void SetEntrancePoint(Vec2i v)
    {
        Entrance = v;
    }

    /// <summary>
    /// TODO - remove building objects as paramater
    /// </summary>
    /// <param name="buildingTiles"></param>
    /// <param name="buildingObjects"></param>
    public void SetBuilding(Tile[,] buildingTiles, WorldObjectData[,] buildingObjects=null)
    {
        BuildingTiles = buildingTiles;
    }

    public void SetSettlement(Settlement settle)
    {

        SettlementID = settle.SettlementID;

        
    }



    public Recti GetWorldBounds()
    {
        if(WorldBounds == null)
        {
            WorldBounds = new Recti(WorldPosition.x, WorldPosition.z, Width, Height);
        }

        return WorldBounds;
    }

    public void CalculateSpawnableTiles(BuildingVoxels vox)
    {
        SpawnableTiles = new List<Vec2i>();

        for (int x = 0; x < Width; x++)
        {
            for (int z = 0; z < Height; z++)
            {

                if (vox.GetVoxelNode(x, 0, z).Voxel != Voxel.none)
                    continue;
                bool canPlace = true;
                foreach(WorldObjectData obj in InternalObjects)
                {
                    if (obj.IntersectsPoint(new Vec2i(x, z))){
                        canPlace = false;
                        break;
                    }
                        
                }
                if (canPlace)
                {
                    if(BuildingSubworld != null)
                        SpawnableTiles.Add(new Vec2i(x, z));
                    else
                        SpawnableTiles.Add(new Vec2i(x, z) + WorldPosition);
                }
                    
            }
        }
    }

    public List<Vec2i> GetSpawnableTiles(bool force=false)
    {
        if (SpawnableTiles == null || force)
        {

            SpawnableTiles = new List<Vec2i>();
            Vec2i worldPos = WorldPosition;
            for (int x=0; x<Width; x++)
            {
                for(int z=0; z<Height; z++)
                {
                    
                    //if(BuildingObjects[x,z] == null)
                    //{
                        SpawnableTiles.Add(new Vec2i(worldPos.x + x, worldPos.z + z));
                    //}
                }
            }
        }

        return SpawnableTiles;
    }

    public override bool Equals(object obj)
    {
        if (obj == null)
            return false;
        if (!(obj is Building))
            return false;
        return WorldPosition.Equals((obj as Building).WorldPosition);
    }
}

public enum BuildingStyle
{
    stone, wood, brick, sandstone
}