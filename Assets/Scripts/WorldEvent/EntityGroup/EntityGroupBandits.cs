using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class EntityGroupBandits : EntityGroup
{

    private int LifeTime = 0;
    private bool RunFromTarget = false;


    private bool ReturningHome = false;

    private ChunkStructure Home;

    public EntityGroupBandits(ChunkStructure home, List<Entity> entities = null, EconomicInventory inventory = null) : base(home.Position, entities, inventory)
    {

        Vec2i deltaPosition = GenerationRandom.RNG.RandomVec2i(-5, 5);

        Vec2i tChunk = home.Position + deltaPosition;

        Home = home;
        GenerateNewPath(tChunk);
    }

    public override GroupType Type => EntityGroup.GroupType.BanditPatrol;

    public override bool DecideNextTile(out Vec2i nextTile)
    {
        LifeTime++;

        //If no target, we move towards next point
        if (TargetGroup == null)
        {
            //search for a target
            if (SearchForTarget())
            {
                //If we find one, we set it as our current target
                EndChunk = TargetGroup.CurrentChunk;
                PersueTarget();
            }
            else
            {
                nextTile = NextPathPoint();
                return true;
            }
        }
        else
        {



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

        List<EntityGroup> nearGroups = WorldEventManager.Instance.GetEntityGroupsNearPoint(CurrentChunk, 3);
        EntityGroup targetDecide = null;
        foreach(EntityGroup g in nearGroups)
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
                g.Kill();
        }
        
    }

    public override void OnReachDestination(Vec2i position)
    {
        if (ReturningHome)
        {
            //This means we are back at base
            ShouldDestroy = true;
            Home.GroupReturn(this);
        }


        if(LifeTime > 20)
        {
            ReturningHome = true;
            GenerateNewPath(Home.Position);
        }
        else
        {
            Vec2i deltaPosition = GenerationRandom.RNG.RandomVec2i(-5, 5);
            GenerateNewPath(CurrentChunk + deltaPosition);

        }
    }
}