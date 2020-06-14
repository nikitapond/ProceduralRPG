using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public abstract class EntityGroup
{

    public enum GroupType
    {
        BanditPatrol,
        Traders,
        SoldierPatrol,
        VillageFoodExport,
        VillageOreExport,
        VillageWoodExport,
        VillageAnimalExport
    }

    protected EntityGroup TargetGroup;
    public abstract GroupType Type { get; }

    public bool ShouldDestroy { get; protected set; }

    protected List<int> GroupEntityIDs;
    protected Inventory GroupInventory;

    public EconomicInventory EconomicInventory;


    protected int CombatStrength;

    public List<Vec2i> Path { get; private set; }
    public int CurrentPathIndex { get; protected set; }

    public Vec2i StartChunk;
    public Vec2i CurrentChunk;
    public Vec2i EndChunk;
    /// <summary>
    /// Creates a entity group that aims to travel from the start chunk to the end chunk
    /// </summary>
    /// <param name="startChunk"></param>
    /// <param name="endChunk"></param>
    /// <param name="entities"></param>
    public EntityGroup(Vec2i startChunk, List<Entity> entities=null, EconomicInventory inventory=null)
    {
        ShouldDestroy = false;
        StartChunk = startChunk;
        CurrentChunk = startChunk;
        GroupEntityIDs = new List<int>();
        GroupInventory = new Inventory();
        EconomicInventory = inventory==null?new EconomicInventory(): inventory;
        if (entities != null)
        {
            foreach (Entity e in entities)
            {
                
                GroupEntityIDs.Add(e.ID);
                GroupInventory.AddAll(e.Inventory);
                CombatStrength += e.CombatManager.CalculateEntityCombatStrength();
            }
        }

    }

    /// <summary>
    /// Generates a new path from the current position to the specified end
    /// </summary>
    /// <param name="end">The target destination for this entity group</param>
    public void GenerateNewPath(Vec2i end)
    {
        int pathDist = (int)end.Distance(CurrentChunk);

        if(pathDist < 32)
            Path = WorldEventManager.Instance.PathFinder.GeneratePath(CurrentChunk, end);
        else
            Path = WorldEventManager.Instance.GeneratePath(CurrentChunk, end);

        EndChunk = end;

        CurrentPathIndex = 0;
    }

    /// <summary>
    /// Finds the next point on this entity groups path
    /// </summary>
    /// <returns></returns>
    public Vec2i NextPathPoint()
    {
        if (Path == null)
            return null;
        CurrentPathIndex++;
        if (CurrentPathIndex >= Path.Count)
            return null;
        CurrentChunk = Path[CurrentPathIndex];
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

    public abstract void OnGroupInteract(List<EntityGroup> other);


    public void Kill()
    {
        ShouldDestroy = true;
        Debug.Log(this + " has been killed :(");
    }
}