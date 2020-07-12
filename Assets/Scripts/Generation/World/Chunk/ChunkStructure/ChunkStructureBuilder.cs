using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public abstract class ChunkStructureBuilder : BuilderBase
{

    public Dictionary<Subworld, ISubworldEntranceObject> Subworlds { get; private set; }
    protected List<Entity> Entities;
    public bool HasSubworlds { get { return Subworlds != null; } }
    public ChunkStructure Structure { get; private set; }
    public ChunkStructureBuilder(ChunkStructure structure, GameGenerator gameGen = null) : base(structure.ChunkPos, structure.Size, null, null)
    {
        Structure = structure;
    }


    public void AddSubworld(ISubworldEntranceObject obj, Subworld subworld)
    {
        if (Subworlds == null)
            Subworlds = new Dictionary<Subworld, ISubworldEntranceObject>();
        Subworlds.Add(subworld, obj);
    }
    /// <summary>
    /// Adds an entity to this chunk structure.
    /// The entities position should be defined locally, as global modification is done here
    /// </summary>
    /// <param name="entity"></param>
    public void AddEntity(Entity entity)
    {
        if (Entities == null)
            Entities = new List<Entity>();
        entity.MoveEntity(entity.Position + BaseTile.AsVector3());
        Entities.Add(entity);

    }

    public override void OnCreate()
    {
        if (Entities != null)
            Structure.SetEntities(Entities);
    }


}