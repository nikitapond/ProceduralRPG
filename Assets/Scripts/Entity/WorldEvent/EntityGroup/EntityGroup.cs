using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public abstract class EntityGroup
{

    public enum GroupType
    {
        BanditPatrol,
        Traders,
        SoldierPatrol
    }


    public abstract GroupType Type { get; }

    protected List<int> GroupEntityIDs;
    protected Inventory GroupInventory;
    protected int CombatStrength;

    public List<Vec2i> Path { get; private set; }
    public int CurrentPathIndex { get; protected set; }

    /// <summary>
    /// Creates a entity group that aims to travel from the start chunk to the end chunk
    /// </summary>
    /// <param name="startChunk"></param>
    /// <param name="endChunk"></param>
    /// <param name="entities"></param>
    public EntityGroup(Vec2i startChunk, Vec2i endChunk, List<Entity> entities=null)
    {
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

    public void GenerateNewPath(Vec2i start, Vec2i end)
    {
        Path = WorldEventManager.Instance.PathFinder.GeneratePath(start, end);
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
    /// <summary>
    /// Function called to decide the next position of this group
    /// </summary>
    public abstract bool DecideNextTile(out Vec2i nextTile);

    public abstract void OnReachDestination(Vec2i position);

    public abstract void OnGroupInteract(EntityGroup other);

}