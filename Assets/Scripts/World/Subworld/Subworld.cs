using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
/// <summary>
/// A subworld is a seperate world that the player (or entities) can enter.
/// It contains a 2d array of chunks, which are all loaded on entrance.
/// A subworld also contains a list of entities, all of which are loaded when entering the subworld.
/// </summary>
[System.Serializable]
public class Subworld
{


    public ChunkData[,] SubworldChunks { get; protected set; }

    private Recti SubworldBounds;

    /// <summary>
    /// Where in the world the sbuworld entrance/exit is
    /// </summary>
    public Vec2i ExternalEntrancePos { get; protected set; }
    /// <summary>
    /// Local coordinate of the position inside the subworld.
    /// i.e, the position the player/entities teleport to when entering this subworld
    /// </summary>
    public Vec2i InternalEntrancePos { get; protected set; }

    public ISubworldEntranceObject Entrance; //The object that is used to exit the subworld, its ID must be set to this ID.
    public ISubworldEntranceObject Exit; //The object that is used to exit the subworld, its ID must be set to this ID.

    public List<Entity> Entities { get; private set; }
    

    public Vec2i ChunkSize { get; private set; }
    public int SubworldID { get; private set; }

   


    public Subworld(ChunkData[,] subChunks, Vec2i internalEntrance, Vec2i externalEtrnace)
    {
        SubworldChunks = subChunks;
        ExternalEntrancePos = externalEtrnace;
        InternalEntrancePos = internalEntrance;
        ChunkSize = new Vec2i(subChunks.GetLength(0), subChunks.GetLength(1));
        //Entities = new List<Entity>();

    }

    public void SetSubworldBounds(Recti bounds)
    {
        SubworldBounds = bounds;
    }

    public Recti GetSubworldBounds()
    {
        if (SubworldBounds == null)
            SubworldBounds = new Recti(new Vec2i(0, 0), ChunkSize * World.ChunkSize);
        return SubworldBounds;
    }

    /// <summary>
    /// Returns true if the ChunkRegionManager should unload the world when entering this subworld,
    /// returns false if the world should be kept loaded.
    /// ............
    /// Designed sich that small subworlds do not require the world to be unloaded, resulting in quicker loading times
    /// </summary>
    /// <returns></returns>
    public virtual bool ShouldUnloadWorldOnEnter()
    {
        if (SubworldChunks.GetLength(0) * SubworldChunks.GetLength(1) <= 12)
            return false;
        return true;
    }

    public bool HasEntities { get { return Entities != null; } }

    public void AddEntity(Entity entity)
    {
        if (Entities == null)
            Entities = new List<Entity>();
        Entities.Add(entity);
    }
    public void RemoveEntity(Entity entity)
    {
        Entities?.Remove(entity);
    }

    public void SetExternalEntrancePos(Vec2i v)
    {
        ExternalEntrancePos = v;
        SetSubworldID(v.GetHashCode());
    }

    public void SetSubworldID(int id)
    {
        if (Entrance == null)
        {
            throw new System.Exception("Can only set ID of subworld once entrance is set");
        }
        SubworldID = id;
        Entrance.SetSubworld(this);


    }

    public void SetWorldEntrance(Vec2i ent)
    {
        InternalEntrancePos = ent;
    }

    public ChunkData GetChunkSafe(int x, int z)
    {
        if (SubworldChunks.GetLength(0) <= x || x < 0)
            return null;
        if (SubworldChunks.GetLength(1) <= z || z < 0)
            return null;
        return SubworldChunks[x, z];
    }

    public override bool Equals(object obj)
    {
        if (obj == null)
            return false;
        Subworld ot = obj as Subworld;
        if (ot == null)
            return false;
        return ot.SubworldID == SubworldID;
    }


    public override string ToString()
    {
        return SubworldID.ToString() + ":" + (World.Instance.GetSubworld(SubworldID)==null);
    }
}