using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
[System.Serializable]
public class EntityAI
{
    public static readonly int IDLE_CHUNK_DIST = 5;

    private Entity Entity;
    public EntityCombatAI CombatAI;
    public EntityTaskAI TaskAI;
    private EntityPathFinder EPF;


    private Vec2i EntityPathTarget;
    public EntityPath EntityPath; //TODO - change back to private

    public EntityAI(Entity entity, EntityCombatAI combatAI, EntityTaskAI taskAI)
    {
        Entity = entity;
        CombatAI = combatAI;
        TaskAI = taskAI;
    }

    /// <summary>
    /// Update is called every frame
    /// </summary>
    public void Update()
    {
        if (CombatAI.InCombat)
            CombatAI.Update();
        else
            TaskAI.Update();

    }
    //Tick is called not every frame
    public void Tick()
    {
        //Always run combat tick
        CombatAI.Tick();
        //Only run tasks while not in combat
        if (!CombatAI.InCombat)
            TaskAI.Tick();


       // Entity.GetLoadedEntity().SpeechBubble.SetText(ToString());
    }

    public override string ToString()
    {

        string data = Entity.ToString() + "\n";
        data += CombatAI.ToString() + "\n";
        data += TaskAI.ToString();
        return data;
    }


    private Vec2i Target;
    public bool GeneratePath(Vec2i target)
    {
        Entity.GetLoadedEntity().LEPathFinder?.SetTarget(target.AsVector2());
        Target = target;

        return true;
        //if no target, null previous paths and return.
        if (target == null)
        {
            Debug.Log("Target is null - no path");
            EntityPathTarget = null;
            EntityPath = null;
            return false;
        }

        if(EPF == null)
        {
            //TODO - get free path finder from parent
            EPF = new EntityPathFinder(GameManager.PathFinder);
        }
        
        Vec2i tilePos = Vec2i.FromVector3(Entity.Position);


        /*if(GameManager.PathFinder.NodeValue(tilePos) > 100)
        {
            tilePos = Vec2i.FromVector3(Entity.Position + new Vector3(0.5f, 0, 0.5f));
            if(GameManager.PathFinder.NodeValue(tilePos) > 100)
            {
                Debug.Log("hm");
            }
        }*/
        //if we have no path
        if (EntityPath == null)
        {
            Debug.Log("[PathFinding] Attempting path from " + tilePos + " to " + target);
            //We check if one is being generated
            if (!EPF.IsRunning)
            {
                //If not, we start generating it
                EPF.FindPath(tilePos, target);
                return false;
            }

            //Check if the path finder has completed its work
            if (EPF.IsComplete())
            {
                //If the path is found, we set it and return true
                if (EPF.FoundPath)
                {
                    EntityPathTarget = target;
                    EntityPath = new EntityPath(EPF.GetPath());
                    return true;
                }
                else
                {
                    Debug.Log("Path from " + tilePos + " to " + target + " not found");

                    return false;
                }
            }
            return false;

            /*
            //If there is no path, Attempt to generate it
            List<Vec2i> path = GameManager.PathFinder.GeneratePath(tilePos, target);
            if (path==null || path.Count == 0)
            {
                Debug.Log("Path from " + tilePos + " to " + target + " not found");
                return false;
            }
            EntityPathTarget = target;
            EntityPath = new EntityPath(path);
            return true;*/
        }
        else if(EntityPath.Target == target)
        {//If the current path has the same target, return true
            return true;
        }
        else
        {
            Debug.Log("[PathFinding] Attempting updated path " + tilePos + " to " + target);
            if (Vec2i.QuickDistance(EntityPath.Target, target) < 5)
            {
                //If the path exists but doesn't have the same target, but targets are close, we attempt to re-calculate the path
                return EntityPath.UpdateTarget(target, EPF);
            }else
            {
                EntityPath = null;
                //We check if one is being generated
                if (!EPF.IsRunning)
                {
                    //If not, we start generating it
                    EPF.FindPath(tilePos, target);
                    return false;
                }/*
                List<Vec2i> newPath = GameManager.PathFinder.GeneratePath(tilePos, target);
                if(newPath==null || newPath.Count == 0)
                {
                    Debug.Log("No path found from " + tilePos + " to " + target + " for entity " + Entity);
                    return false;
                }
                EntityPath = new EntityPath(newPath);
                return true;*/
            }
            
        }
        return false;
    }

    
    public bool FollowPath()
    {
        Debug.Log("OMGOMGOMGOMG" + "Entity " + Entity + " is moving to " + Target);
        return true;
        if (EntityPath == null)
            return false;
        Vec2i nexti_ = EntityPath.CurrentIndex();
        if (nexti_ == null)
            return true;
        Vector2 next = nexti_.AsVector2();
        if (PositionWithinDistance(Entity.GetLoadedEntity().transform.position, next + new Vector2(0.5f, 0.5f), 0.1f))
        {
            Vec2i nexti = EntityPath.NextIndex();
            if (nexti == null)
                return true;
            next = nexti.AsVector2();
        }
        
        if(next != null && Entity.GetLoadedEntity()!=null) {
            
            Entity.GetLoadedEntity().MoveTowards(next + new Vector2(0.5f,0.5f));
            Entity.GetLoadedEntity().LookTowardsPoint(next + new Vector2(0.5f, 0.5f));
            return false;
        }
        return false;
    }

    /// <summary>
    /// Called when entity is loaded into game. Adds event listeners
    /// </summary>
    public void OnEntityLoad()
    {
        EventManager.Instance.AddListener(CombatAI);
        CombatAI.CheckEquiptment();
        CombatAI.SetEntity(Entity);
        TaskAI.SetEntity(Entity);
        //GameManager.EventManager.AddListener(TaskAI);
    }
    /// <summary>
    /// Called when an entity is unloaded or killed, removes listeners
    /// </summary>
    public void OnEntityUnload()
    {
        GameManager.EventManager.RemoveListener(CombatAI);

    }


    private static bool PositionWithinDistance(Vector3 a, Vector2 b, float dist)
    {
        return (a.x - b.x) * (a.x - b.x) + (a.z - b.y) * (a.z - b.y) < dist * dist;
    }
    private static bool Vector2WithinDistance(Vector2 a, Vector2 b, float dist)
    {
        return (a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) < dist * dist;
    }
}