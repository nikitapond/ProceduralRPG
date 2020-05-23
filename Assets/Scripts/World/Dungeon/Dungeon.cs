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
        Vec2i entranceChunk = World.GetChunkPosition(ExternalEntrancePos);
        int local = WorldObject.ObjectPositionHash(ExternalEntrancePos);
        
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
        entity.SetPosition(InternalEntrancePos);
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
        entity.SetPosition(ExternalEntrancePos);
    }
    public override string ToString()
    {
        return "dungeon";
    }
}