using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class EntityGroupBandits : EntityGroup
{

    private int LifeTime = 0;
    private bool RunFromTarget = false;


    private bool ReturningHome = false;

    private ChunkStructure Home;


    private List<EntityGroup> NearGroups;

    public EntityGroupBandits(ChunkStructure home, List<Entity> entities = null, EconomicInventory inventory = null) : base(home.ChunkPos, entities, inventory)
    {

        Vec2i deltaPosition = GenerationRandom.RNG.RandomVec2i(-64, 64);

        Vec2i tChunk = home.ChunkPos + deltaPosition;

        NearGroups = new List<EntityGroup>(20);

        Home = home;
        GenerateNewPath(tChunk);
    }

    public override GroupType Type => EntityGroup.GroupType.BanditPatrol;

    public override bool DecideNextTile(out Vec2i nextTile)
    {
        /*
        if (TargetGroup == null)
        {
            nextTile = NextPathPoint();
            return nextTile != null;
        }

        nextTile = CurrentChunk;
        return true;
        */

        LifeTime++;

        //If no target,
        if (TargetGroup == null)
        {
            //search for a target
            if (SearchForTarget())
            {
                //If we find one, we set it as our current target
                EndChunk = TargetGroup.CurrentChunk;
                nextTile = PersueTarget();
            }
            else
            {//If none is found, we move to the next point on the patrol
                nextTile = NextPathPoint();
                return true;
            }
        }
        else
        {
            nextTile = PersueTarget();
            return true;

        }

        nextTile = null;
        return false;
    }
    /// <summary>
    /// Finds the next position of this group while we persue the target
    /// </summary>
    /// <returns></returns>
    private Vec2i PersueTarget()
    {

        return TargetGroup.CurrentChunk;
        

    }


    private bool SearchForTarget()
    {
        Debug.BeginDeepProfile("TargetSearch");
        NearGroups.Clear();
        WorldEventManager.Instance.GetEntityGroupsNearPoint(CurrentChunk, 8, NearGroups);
        EntityGroup targetDecide = null;
        foreach(EntityGroup g in NearGroups)
        {
            if(g.Type == GroupType.SoldierPatrol)
            {
                targetDecide = g;
                RunFromTarget = true;
                break;
            }
            if(g.Type == GroupType.BanditPatrol)
            {

            }
            else
            {
                //Any trader group
                targetDecide = g;
            }


        }
        Debug.EndDeepProfile("TargetSearch");

        TargetGroup = targetDecide;
        if (targetDecide != null)
            return true;


        return false;
    }


    public override void OnGroupInteract(List<EntityGroup> other)
    {
        foreach(EntityGroup g in other)
        {
            if (g == this)
                continue;
            if (!(g is EntityGroupBandits))
            {
                g.Kill();
                if (g == TargetGroup)
                {
                    TargetGroup = null;
                    OnReachDestination(CurrentChunk);
                }
                    
            }
                
        }
        
    }

    public override bool OnReachDestination(Vec2i position)
    {
        if (ReturningHome)
        {
            //This means we are back at base
            ShouldDestroy = true;
            Home.GroupReturn(this);
            return false;
        }


        if(LifeTime > 20)
        {
            ReturningHome = true;
            GenerateNewPath(Home.ChunkPos);
        }
        else
        {
            Vec2i deltaPosition = GenerationRandom.RNG.RandomVec2i(-64, 64);
            GenerateNewPath(CurrentChunk + deltaPosition);
        }
        return true;
    }
}