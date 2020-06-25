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


    protected float CurrentMovementSpeed = 1;

    protected int CombatStrength;
    /// <summary>
    /// The current travel path that this entity group will follow
    /// Two points on this path are not required to be neihbors,
    /// in this case we will move a number of chunks towards the next path point,
    /// based on <see cref="CurrentMovementSpeed"/>
    /// </summary>
    public List<Vec2i> Path { get; private set; }
    public int CurrentPathIndex { get; protected set; }

    public Vec2i StartChunk;
    public Vec2i CurrentChunk { get { return Vec2i.FromVector2(CurrentPosition); } }
    private Vector2 CurrentPosition;
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
        CurrentPosition = startChunk.AsVector2();
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
        CurrentPosition = CurrentChunk.AsVector2();
        /*if (pathDist < 32)
            Path = WorldEventManager.Instance.PathFinder.GeneratePath(CurrentChunk, end);
        else*/
            Path = WorldEventManager.Instance.GeneratePath(CurrentChunk, end);

        EndChunk = end;
        
        CurrentPathIndex = 0;

    }

    /// <summary>
    /// Finds the next point on this entity groups path by travelling a distance
    /// based on   
    /// </summary>
    /// <param name="movement">The amount of movement the entity should do. If this is -1, we set it to their current movement speed</param>
    /// <returns>The position the entity group will be at after this tick.
    ///          Returns null if we are at the end of the path</returns>
    public Vec2i NextPathPoint(float movement=-1)
    {



        if (Path == null)
            return null;

        Vec2i finalPoint = FinalPathPoint();
        /*
        if (Vec2i.QuickDistance(finalPoint, CurrentChunk) < 4)
        {
            CurrentPosition = finalPoint.AsVector2();
            return null;
        }*/

        if (movement <= 0)
            movement = CurrentMovementSpeed;

        if (movement < 0.5f)
        {
            //If the current chunk is the last point, we return null to imply we have ended our travels
            if (CurrentChunk == finalPoint)
                return null;
            return CurrentChunk;
        }
            


        //If the next point is out of bounds, then that means we are at the final point
        if(CurrentPathIndex + 1 >= Path.Count)
        {
            //This means there is no next path point
            return null;
        }
        //If the next path point exists, we find it, and the direction from our current position to it
        Vec2i nextPoint = Path[CurrentPathIndex + 1];
        Vector2 direction = (nextPoint.AsVector2() - CurrentPosition);

        //We find the size, and then normalise the direction
        float mag = direction.magnitude;
        direction /= mag;
        
        //If the next position is within our current movement
        if(mag + 0.3f < movement)
        {

            float rem = movement - mag;

            //We incriment to the next path point
            CurrentPathIndex++;
            //And find the path accordingly
            return NextPathPoint(rem);
        }
        //We find the new position
        CurrentPosition = CurrentPosition + direction * movement;
        return CurrentChunk;
    }
    public Vec2i FinalPathPoint()
    {
        if (Path == null)
            return null;
        if (Path.Count == 0)
            return null;
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
    /// <summary>
    /// Called when an entity group has reached its current destination - 'position'
    /// Returns true if the entity group has a new target, returns false if the entity group is finsihed.
    /// </summary>
    /// <param name="position"></param>
    /// <returns>true</returns>
    public abstract bool OnReachDestination(Vec2i position);

    public abstract void OnGroupInteract(List<EntityGroup> other);


    public void Kill()
    {
        ShouldDestroy = true;
        Debug.Log(this + " has been killed :(");
    }
}