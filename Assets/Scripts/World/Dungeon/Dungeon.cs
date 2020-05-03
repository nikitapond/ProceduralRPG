using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
[System.Serializable]
public class Dungeon : Subworld
{

    public List<Entity> DungeonEntities;
    public List<Entity> EnteredEntities;
    public DungeonBoss Boss;
    private Key Key;
    public bool IsLocked { get; private set; }
    public Dungeon(ChunkData[,] subChunks, Vec2i subentra, Vec2i worldEntrance, List<Entity> dungeonEntity, DungeonBoss boss, SimpleDungeonKey key=null) : base(subChunks, subentra, worldEntrance)
    {
        DungeonEntities = dungeonEntity;
        Boss = boss;
        Vec2i entranceChunk = World.GetChunkPosition(SubworldEntrance);
        int local = WorldObject.ObjectPositionHash(SubworldEntrance);
        if (!(subChunks[entranceChunk.x, entranceChunk.z].GetObject(SubworldEntrance.x, SubworldEntrance.z) is DungeonEntrance))
        {
            //If the entrance isn't a dungeon entrance.
          //  DungeonEntrance de = new CaveDungeonEntrance(subentra, this, new WorldObjectMetaData(direction:new Vec2i(0,-1)));
            //SubworldChunks[entranceChunk.x, entranceChunk.z].SetObject(SubworldEntrance.x, SubworldEntrance.z, de);
        }
        Key = key;
        if (Key == null)
        {
            IsLocked = false;
        }
        else
            IsLocked = true;
    }


    public bool RequiresKey()
    {
        if (Key == null)
            return false;
        return Key != null && IsLocked==true;
    }
    public Key GetKey()
    {
        return Key;
    }

    public void SetKey(Key key)
    {
        Key = key;
    }



    public bool EntityHasEntered(Entity entity)
    {
        if (EnteredEntities == null)
            return false;
        return EnteredEntities.Contains(entity);
    }
    public void EntityLeave(Entity entity)
    {
        EnteredEntities.Remove(entity);
        entity.SetPosition(WorldEntrance);
    }
    public void EntityEnter(Entity entity)
    {

        if (RequiresKey() && IsLocked)
        {
            if(entity.Inventory.ContainsItemStack(Key) == null)
            {
                Debug.Log("Do not have required key");
                return;
            }
        }

        if (EnteredEntities == null)
            EnteredEntities = new List<Entity>();



        EnteredEntities.Add(entity);
        entity.SetPosition(SubworldEntrance);
    }
    public override string ToString()
    {
        return "dungeon";
    }
}