﻿using UnityEngine;
using UnityEditor;

/// <summary>
/// This task attempts to get the entity to travel from a start location to an end location
/// </summary>
[System.Serializable]
public class EntityTaskGoto : EntityTask
{


    private Vec2i TargetTile;
    private bool Running;
    public EntityTaskGoto(Entity entity, Building building, bool running=false, string taskDesc=null) : base(entity, building.GetBuildingGotoTaskLocation(), 5, -1, taskDesc)
    {

        TargetTile = GameManager.RNG.RandomFromList(building.GetSpawnableTiles());
        Running = running;
    
    }
    public EntityTaskGoto(Entity entity, Vec2i targetTile, float priority=5, bool running=false, string taskDesc = null) : base (entity, priority, -1, taskDesc)
    {
        TargetTile = targetTile;
        Running = running;
    }

    public override void Update()
    {
       
        if (Entity.EntityAI.EntityPath == null)
            return;
        if (Running)
            Entity.GetLoadedEntity().SetRunning(true);
        //Entity.EntityAI.FollowPath();
        
    }

    public override void OnTaskEnd()
    {
        if (Running)
            Entity.GetLoadedEntity().SetRunning(false);
    }

    protected override void InternalTick()
    {
        if (Entity.TilePos == TargetTile)
        {
            IsComplete = true;
            return;
        }
        
        int quickDist = QuickDistanceToPlayer(Entity);
        //If we are currently far from the player
        if(quickDist > (World.ChunkSize * 6) * (World.ChunkSize * 6))
        {
            //And the target position is far from the player
            if(Vec2i.QuickDistance(TargetTile, PlayerManager.Instance.Player.TilePos) > (World.ChunkSize * 6) * (World.ChunkSize * 6))
            {

                if (HasTaskLocation)
                {
                    if(Location.SubworldWorldID != Entity.CurrentSubworldID)
                    {
                        //Then we teleport to our target position and world
                        Entity.MoveEntity(TargetTile, Location.SubworldWorldID);
                    }
                    else
                    {
                        //Then we teleport to our target position.
                        Entity.MoveEntity(TargetTile);
                    }
                }
                else
                {
                    //Then we teleport to our target position.
                    Entity.MoveEntity(TargetTile);
                }                
                IsComplete = true;
                return;
            }
        }
        //if no task location is set, we simply go to the desired tile position
        if (!HasTaskLocation)
        {
            Entity.GetLoadedEntity().LEPathFinder.SetTarget(TargetTile.AsVector2());
        }
        else
        {
            //if we do have a location, we check the subworldID
            //if it is the current world, we simply walk there
            if(Entity.CurrentSubworldID == Location.SubworldWorldID)
            {
                Entity.GetLoadedEntity().LEPathFinder.SetTarget(TargetTile.AsVector2());
                Debug.Log("In same world, going now");
            }
            else
            {
                //If we are not in the current target subworld, we have 3 possible cases:
                //1) We are in the main World (-1) travelling to a subworld
                //2) We are in a subworld, travelling to the main world (-1)
                //3) We are in a subworld, and wish to travel to another subworld via the main world
                //Case 3 and 2 can be ignored, as we already automatically take entities from their subworlds 
                //And place them outside if their AI brings them to a a target outside their subworld
                if(Entity.CurrentSubworldID == -1 && Location.SubworldWorldID != -1)
                {
                    Subworld sub = WorldManager.Instance.World.GetSubworld(Location.SubworldWorldID);
              
                    Entity.GetLoadedEntity().LEPathFinder.SetTarget((sub.Entrance as WorldObjectData).Position, TravelThroughDoor, new object[] { sub.Entrance, 0.5f });

                }

            }

        }
        
        
        if(!HasTaskLocation || Location.SubworldWorldID == Entity.CurrentSubworldID)
        {
            if(TargetTile.QuickDistance(Entity.TilePos) < 4)
            {
                IsComplete = true;
            }
        }

    }


    public void TravelThroughDoor(params object[] args)
    {
        ISubworldEntranceObject entrObjec = args[0] as ISubworldEntranceObject;
        WorldObjectData objDat = entrObjec as WorldObjectData;
        WorldObject obj = objDat.LoadedObject;

        obj.OnEntityInteract(Entity);

    }
    public override string ToString()
    {
        string baseStr = base.ToString();
        if (baseStr == null)
            return "Task Goto: " + TargetTile;
        return baseStr;
    }


}