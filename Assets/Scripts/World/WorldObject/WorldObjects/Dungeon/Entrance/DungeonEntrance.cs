using UnityEngine;
using UnityEditor;
using System;

[System.Serializable]
public abstract class DungeonEntrance : WorldObjectData, IMultiTileObject
{

    public DungeonType DungeonType { get; protected set; }
    protected Dungeon Dungeon;
    public ChunkStructure ChunkStructure { get; protected set; }
    private IMultiTileObjectChild[,] Children;
    
    public DungeonEntrance(Vec2i worldPosition, DungeonType type, Dungeon dungeon = null, WorldObjectMetaData meta=null) : base(worldPosition, meta, new Vec2i(4,4))
    {
        Dungeon = dungeon;
        DungeonType = type;


    }
 
    public void SetChunkStructure(ChunkStructure entranceStructure)
    {
        ChunkStructure = entranceStructure;
    }
    public void SetDungeon(Dungeon dungeon)
    {
        Dungeon = dungeon;
    }
    public Dungeon GetDungeon()
    {
        return Dungeon;
    }




    //Dungeon entrance is a blank object, on instansiate we load in doors and surrounding rocks
    public override WorldObjects ObjID => WorldObjects.DUNGEON_ENTRANCE;

    public override string Name =>"Dungeon Entrance";


    public abstract bool CanEntityEnter(Entity entity);

    /// <summary>
    /// Called when an entity interacts with the
    /// entrance. Makes the entity enter the dungeon
    /// If the entity is the player, we load the dungeon in
    /// </summary>
    /// <param name="entity"></param>
    public void OnEntityEnter(Entity entity)
    {
        Debug.Log("OMGOMGOMG");
        if (!CanEntityEnter(entity))
            return;
        //Check if the entity is already inside the dungeon, 
        //If it has, we must leave the dungeon
        if (Dungeon.EntityHasEntered(entity))
        {
            Dungeon.EntityLeave(entity);
            if(entity is Player)
            {
                GameManager.WorldManager.LeaveSubworld();
            }
            return;
        }
        Dungeon.EntityEnter(entity);
        if(entity is Player)
        {
            GameManager.WorldManager.EnterSubworld(Dungeon.SubworldID);
        }
    }

    public IMultiTileObjectChild[,] GetChildren()
    {
        if(Children == null)
        {
            Children = new IMultiTileObjectChild[Size.x, Size.z];
            for(int x=0; x<Size.x; x++)
            {
                for (int z = 0; z < Size.z; z++)
                {
                    Children[x, z] = new EmptyObjectBase(WorldPosition + new Vec2i(x, z), parent:this);
                }
            }
        }
        return Children;
    }
}

