using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class EntityGroup
{

    public enum GroupType
    {
        BanditPatrol,
        Traders,
        SoldierPatrol
    }


    public GroupType Type { get; private set; }

    private List<int> GroupEntityIDs;
    private Inventory GroupInventory;
    private int CombatStrength;

    public List<Vec2i> Path { get; private set; }
    public int CurrentPathIndex { get; private set; }

    public EntityGroup(GroupType type, Vec2i startChunk, Vec2i endChunk, List<Entity> entities=null)
    {
        Type = type;
        GroupEntityIDs = new List<int>();
        GroupInventory = new Inventory();
        if(entities != null)
        {
            foreach (Entity e in entities)
            {
                GroupEntityIDs.Add(e.ID);
                GroupInventory.AddAll(e.Inventory);
                CombatStrength += e.CombatManager.CalculateEntityCombatStrength();
            }
        }

        Path = WorldEventManager.Instance.PathFinder.GeneratePath(startChunk, endChunk);
        CurrentPathIndex = 0;

    }


    public Vec2i NextPathPoint()
    {
        CurrentPathIndex++;
        if (CurrentPathIndex > Path.Count)
            return null;
        return Path[CurrentPathIndex];
    }
    public Vec2i FinalPathPoint()
    {
        return Path[Path.Count - 1];
    }

    /// <summary>
    /// returns the entities that are in this group
    /// </summary>
    /// <returns></returns>
    public List<Entity> GetEntities()
    {
        List<Entity> ent = new List<Entity>(GroupEntityIDs.Count);

        foreach(int id in GroupEntityIDs)
        {
            ent.Add(EntityManager.Instance.GetEntityFromID(id));
        }
        return ent;
    }

}