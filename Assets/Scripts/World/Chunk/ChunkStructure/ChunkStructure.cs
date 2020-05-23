using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public abstract class ChunkStructure
{

    public string Name;


    public Vec2i Position { get; private set; }
    public Vec2i Size { get; private set; }

    public List<Entity> Entities { get; private set; }

    public List<int> SubworldIDs { get; private set; }

    public IInventoryObject MainLootChest { get; private set; }

    public int StructureID { get; private set; }

    public bool HasEntities { get { return Entities != null; } }
    public bool HasSubworlds { get { return SubworldIDs != null; } }
    public bool HasLootChest { get { return MainLootChest != null; } }



    public WorldMapLocation WorldMapLocation { get; private set; }

    public ChunkStructure(Vec2i cPos, Vec2i cSize)
    {
        Position = cPos;
        Size = cSize;
    }


    public void SetEntities(List<Entity> entities)
    {
        Entities = entities;
    }
    public void SetSubworlds(List<Subworld> subworlds)
    {
        SubworldIDs = new List<int>(subworlds.Count);
        foreach(Subworld sw in subworlds)
        {
            SubworldIDs.Add(sw.SubworldID);
        }
    }

    public void SetWorldMapLocation(WorldMapLocation wml)
    {
        WorldMapLocation = wml;
    }
    public void SetMainLootChest(IInventoryObject inv)
    {
        MainLootChest = inv;
    }
    public void SetID(int id)
    {
        StructureID = id;
    }
}