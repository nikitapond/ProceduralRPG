using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public abstract class ChunkStructureBuilder : BuilderBase
{

    public Dictionary<Subworld, ISubworldEntranceObject> Subworlds { get; private set; }
    public bool HasSubworlds { get { return Subworlds != null; } }
    public ChunkStructure Structure { get; private set; }
    public ChunkStructureBuilder(ChunkStructure structure) : base(structure.Position, structure.Size)
    {
        Structure = structure;
    }


    public void AddSubworld(ISubworldEntranceObject obj, Subworld subworld)
    {
        if (Subworlds == null)
            Subworlds = new Dictionary<Subworld, ISubworldEntranceObject>();
        Subworlds.Add(subworld, obj);
    }


}