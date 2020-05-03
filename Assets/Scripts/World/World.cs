using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
/// <summary>
/// Holds all the details the world needs to function.
/// </summary>
public class World
{
    public static readonly int ChunkHeight = 16;
    public readonly static int ChunkSize = 16; //Chunk size in tiles
    public readonly static int WorldSize = 256; //World size in chunks
    public readonly static int RegionSize = 32;
    public readonly static int RegionCount = WorldSize / RegionSize;


    private Object SettlementAddLock; //Lock for thread safe adding settlements
    public Dictionary<int, Settlement> WorldSettlements { get; private set; }
    public Dictionary<int, Kingdom> WorldKingdoms { get; private set; }
    public Dictionary<int, Subworld> WorldSubWorlds { get; private set; }
    public Dictionary<int, ChunkStructure> WorldChunkStructures { get; private set; }

    public ChunkBase[,] ChunkBases;


    public WorldMap WorldMap { get; private set; }
    public World()
    {
        SettlementAddLock = new Object();
        WorldSettlements = new Dictionary<int, Settlement>();
        WorldKingdoms = new Dictionary<int, Kingdom>();
        WorldSubWorlds = new Dictionary<int, Subworld>();
        WorldChunkStructures = new Dictionary<int, ChunkStructure>();
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
        WorldSettlements = gls.WorldSettlements;

    }
    public void WorldSave(GameLoadSave gls)
    {
        gls.WorldKingdoms = WorldKingdoms;
        gls.WorldSettlements = WorldSettlements;
    }

    public int AddSettlement(Settlement settlement)
    {
        lock (SettlementAddLock)
        {
            int place = WorldSettlements.Count;
            while (WorldSettlements.ContainsKey(place))
            {
                place++;
            }

            WorldSettlements.Add(place, settlement);
            settlement.SetSettlementID(place);
            Debug.Log("Settlement " + settlement.ToString() + " has ID " + place, Debug.ENTITY_TEST);
            return place;
        }        
    }

    public int AddSubworld(Subworld subworld)
    {
        int id = WorldSubWorlds.Count;
        WorldSubWorlds.Add(id, subworld);
        subworld.SetSubworldID(id);
        return id;
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
        return WorldSubWorlds[id];
    }

    public Settlement GetSettlement(int id)
    {
        return id == -1 ? null : WorldSettlements[id];
    }


    public int AddKingdom(Kingdom kingdom)
    {
        int place = WorldKingdoms.Count;
        WorldKingdoms.Add(place, kingdom);
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