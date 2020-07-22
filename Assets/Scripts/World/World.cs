using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
/// <summary>
/// Holds all the details the world needs to function.
/// </summary>
public class World
{

    public static World Instance;

    public static readonly int ChunkHeight = 16;
    public readonly static int ChunkSize = 16; //Chunk size in tiles
    public readonly static int WorldSize = 1024; //World size in chunks
    public readonly static int RegionSize = 32;
    public readonly static int RegionCount = WorldSize / RegionSize;


    private Object LocationAddLock; //Lock for thread safe adding settlements
   // public Dictionary<int, Settlement> WorldSettlements { get; private set; }
    public Dictionary<int, Kingdom> WorldKingdoms { get; private set; }
    /// <summary>
    /// Contains all subworlds, with the key being their ID = WorldPosition.Hash
    /// </summary>
    public Dictionary<int, Subworld> WorldSubWorlds { get; private set; }
    /// <summary>
    /// Stores subworlds ordered by the chunk their world entrances exists in
    /// </summary>
    private Dictionary<Vec2i, List<int>> OrderedSubworlds;


    public Dictionary<int, ChunkStructure> WorldChunkStructures { get; private set; }
    /// <summary>
    /// A dictionary containing all world locations
    /// Key = hash of item position
    /// </summary>
    public Dictionary<int, WorldLocation> WorldLocations { get; private set; }



    public ChunkBase[,] ChunkBases;

    public ChunkBase2[,] ChunkBases2;

    public WorldMap WorldMap { get; private set; }
    public World()
    {
        LocationAddLock = new Object();
        WorldKingdoms = new Dictionary<int, Kingdom>();
        WorldSubWorlds = new Dictionary<int, Subworld>();
        OrderedSubworlds = new Dictionary<Vec2i, List<int>>();
        WorldChunkStructures = new Dictionary<int, ChunkStructure>();
        WorldLocations = new Dictionary<int, WorldLocation>();
        Instance = this;
    }

    public void CreateWorldMap()
    {
        WorldMap = new WorldMap(this);
    }
    public void SetChunkBases(ChunkBase[,] cb)
    {
        ChunkBases = cb;
    }

    public void LoadWorld(GameLoadSave gls)
    {
        Debug.Log(gls.WorldKingdoms + " - " + gls.WorldSettlements);
        WorldKingdoms = gls.WorldKingdoms;
        //TODO - fix load save
        //WorldLocation = gls.WorldKingdoms
    }




    public void WorldSave(GameLoadSave gls)
    {
        gls.WorldKingdoms = WorldKingdoms;
        //TODO - fix world save
       // gls.WorldSettlements = WorldSettlements;
    }


    public void AddLocation(WorldLocation location)
    {
        int id = location.LocationID;
        lock (LocationAddLock)
        {
            WorldLocations.Add(id, location);
            if(location is Settlement)
            {
                (location as Settlement).GetKingdom().AddSettlement(location as Settlement);
            }
        }
        
    }
    public void AddLocationRange(List<WorldLocation> locations)
    {
        lock (LocationAddLock)
        {
            foreach(WorldLocation wl in locations)
            {
                WorldLocations.Add(wl.LocationID, wl);
            }
        }
    }



    public int AddSubworld(Subworld subworld)
    {
        int id = subworld.SubworldID;
        WorldSubWorlds.Add(id, subworld);
        //get the chunk position
        Vec2i cPos = World.GetChunkPosition(subworld.ExternalEntrancePos);

        if (!OrderedSubworlds.ContainsKey(cPos))
            OrderedSubworlds.Add(cPos, new List<int>());
        //Add ID to relevent chunk position
        OrderedSubworlds[cPos].Add(id);
        return id;
    }

    public List<int> GetSubworldIDsInChunk(Vec2i cPos)
    {
        if(OrderedSubworlds.TryGetValue(cPos, out List<int> ids))
        {
            return ids;
        }
        return null;
    }
    public List<Subworld> GetSubworldsInChunk(Vec2i cPos)
    {
        List<int> ids = GetSubworldIDsInChunk(cPos);
        if (ids == null)
            return null;
        List<Subworld> subs = new List<Subworld>(ids.Count);
        foreach(int id in ids)
        {
            subs.Add(GetSubworld(id));
        }
        return subs;
    }

    public int AddChunkStructure(ChunkStructure cStruct)
    {
        int id = WorldChunkStructures.Count;
        WorldChunkStructures.Add(id, cStruct);
        cStruct.SetID(id);
        return id;
    }
    public ChunkStructure GetChunkStructure(int id)
    {
        WorldChunkStructures.TryGetValue(id, out ChunkStructure value);
        return value;
    }
    public Subworld GetSubworld(int id)
    {
        if (WorldSubWorlds.TryGetValue(id, out Subworld sw))
            return sw;
        if (id == -1)
            return null;
        Debug.LogError("Subworld with ID " + id + " not found");
        return null;
    }

    public Settlement GetSettlement(int id)
    {
        //Check if we have this location id
        if (WorldLocations.TryGetValue(id, out WorldLocation loc))
            //Cast to settlement, will return null if this isn't a settlement
            return loc as Settlement;
        return null;
    }


    public int AddKingdom(Kingdom kingdom)
    {
        int place = WorldKingdoms.Count + 1;
        WorldKingdoms.Add(place, kingdom);
        kingdom.SetKingdomID(place);
        return place;
    }
    public Kingdom GetKingdom(int id)
    {
        return id == -1 ? null : WorldKingdoms[id];
    }




    public static Vec2i GetRegionCoordFromChunkCoord(Vec2i chunkCoord)
    {
        return new Vec2i(Mathf.FloorToInt((float)chunkCoord.x / World.RegionSize), Mathf.FloorToInt((float)chunkCoord.z / World.RegionSize));
    }



    /// <summary>
    /// Finds all empty points around a given world object.
    /// Object must be an instance
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public Vec2i[] EmptyTilesAroundWorldObject(WorldObjectData obj)
    {
        Vec2i instPos = Vec2i.FromVector3(obj.Position);
        if (instPos == null)
        {
            Debug.Error("Provided world object has no instance: " + obj.ToString());
            return null;
        }
        return EmptyTilesAroundPoint(instPos);
        /*
        //Check if this object is part of a multicell object.
        if(obj.HasMetaData && (obj.MetaData.IsParent || obj.MetaData.Parent!=null))
        {
            WorldObject parent = obj.MetaData.IsParent ? obj : obj.MetaData.Parent; //Get the valid parent
            List<Vec2i> toTry = new List<Vec2i>(parent.MetaData.MultiCellWidth*2 + parent.MetaData.MultiCellHeight*3);
            //Iterate all boundary tiles
            for(int x=-1; x<parent.MetaData.MultiCellWidth+1; x++)
            {
                toTry.Add(instPos + new Vec2i(x, -1));
                toTry.Add(instPos + new Vec2i(x, parent.MetaData.MultiCellHeight+1));
            }
            for (int z = 0; z < parent.MetaData.MultiCellHeight; z++)
            {
                toTry.Add(instPos + new Vec2i(-1, z));
                toTry.Add(instPos + new Vec2i(parent.MetaData.MultiCellWidth+1, z));

            }
            List<Vec2i> freeTiles = new List<Vec2i>(parent.MetaData.MultiCellWidth * 2 + parent.MetaData.MultiCellHeight * 3);
            foreach (Vec2i v_ in toTry)
            {
                if (GetWorldObject(v_) == null)
                    freeTiles.Add(v_);
            }
            return freeTiles.ToArray();
        }
        else
        {
            //If no meta data, this must be a single tile object, so return free points around the isntance pos
            return EmptyTilesAroundPoint(instPos);
        }*/
    }

    public Vec2i[] EmptyTilesAroundPoint(Vec2i v)
    {
        List<Vec2i> tiles = new List<Vec2i>();
        Vec2i[] toTry = new Vec2i[] { new Vec2i(1, 0) + v, new Vec2i(0, 1) + v, new Vec2i(-1, 0) + v, new Vec2i(0, -1) + v };
        foreach(Vec2i v_ in toTry)
        {
            if (GetWorldObject(v_) == null)
                tiles.Add(v_);
        }

        return tiles.ToArray();
    }

    public WorldObjectData InventoryObjectNearPoint(Vec2i v)
    {
        Vec2i[] toTry = new Vec2i[] { new Vec2i(1, 0) + v, new Vec2i(0, 1) + v, new Vec2i(-1, 0) + v, new Vec2i(0, -1) + v , v,
                                      new Vec2i(1, 1) + v, new Vec2i(1, -1) + v, new Vec2i(-1, 1) + v, new Vec2i(-1, -1) + v};
        
        foreach(Vec2i v_ in toTry)
        {
            if (v_.x < 0 || v_.z < 0)
                continue;
            WorldObjectData obj = GetWorldObject(v_);
            if(obj != null && obj is IInventoryObject)
                return obj;
        }
        return null;
    }

    public WorldObjectData GetWorldObject(Vec2i pos)
    {
        return null;
        //ChunkData c = GameManager.WorldManager.CRManager.GetChunk(GetChunkPosition(pos));
        //return c.GetObject(pos.x, pos.z);
    }

    

  

    public static Vec2i GetRegionPosition(Vector3 position)
    {
        return new Vec2i((int)(position.x / (RegionSize*World.ChunkSize)), (int)(position.z / (RegionSize*World.ChunkSize)));
    }
    public static Vec2i GetChunkPosition(Vector3 position)
    {
        return new Vec2i((int)(position.x / World.ChunkSize), (int)(position.z / World.ChunkSize));
    }
    public static Vec2i GetChunkPosition(float x, float z)
    {
        return new Vec2i((int)(x / World.ChunkSize), (int)(z / World.ChunkSize));

    }
    public static Vec2i GetChunkPosition(Vec2i position)
    {
        return new Vec2i((int)((float)position.x / World.ChunkSize), (int)((float)position.z / World.ChunkSize));
    }

}