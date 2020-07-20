using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
[System.Serializable]
public abstract class EntityCombatAI : IWorldCombatEvent
{
    protected WorldCombat CurrentCombatEvent;
    /// <summary>
    /// The current entity related to this entities combat.
    /// If the entity is attacking, this is their target
    /// If they are running, they are running from this entity
    /// </summary>
    public Entity CurrentTarget { get;  protected set; }
    public bool InCombat { get { return CurrentCombatEvent != null; } }
    protected Entity Entity;

    public bool IsRunningFromCombat { get; protected set; }
    private List<Entity> NearEntities;

    private string currentCombatTask;

    private LastTargetDetails LastTargetDetails;



    #region abstract_functions
    protected abstract bool ShouldRun(Entity entity);
    protected abstract bool ShouldCombat(Entity entity);
    protected abstract void ChooseEquiptWeapon();
    public abstract void WorldCombatEvent(WorldCombat wce);
    #endregion

    public void SetEntity(Entity e)
    {
        Entity = e;
        //If the entity should be running
        if (IsRunningFromCombat)
        {
            //We reset
            IsRunningFromCombat = true;
            //We check if we have a current valid target to be running from
            if(CurrentTarget != null && CurrentTarget.CurrentSubworldID == Entity.CurrentSubworldID)
            {
                RunFromCombat(CurrentTarget.TilePos);
            }
            else
            {
                //if we don't we run randomly.
                RunFromCombat();
            }
            
        }
    }
    /// <summary>
    /// Instructs the parent entity <see cref="EntityCombatAI.Entity"/> to try and attack the target <paramref name="entity"/>. <br/>
    /// We check if either the parent entity or target entity are already in combat events. If so, we add the other entity to the relevent team.
    /// <br/>
    /// </summary>
    /// <param name="entity"></param>
    public void Attack(Entity entity)
    {
        WorldCombat wce = null;
        //Check for combat event related to target entity
        if(EntityManager.Instance.EntityInWorldCombatEvent(entity, out wce))
        {
            //Add parent entity to relevent team
            if (wce.Team1.Contains(entity))
            {
                wce.Team2.Add(Entity);
            }
            else
            {
                wce.Team1.Add(Entity);
            }
            //Check for combat event related to parent entity.
        }else if(EntityManager.Instance.EntityInWorldCombatEvent(Entity, out wce))
        {
            if (wce.Team1.Contains(Entity))
            {
                wce.Team2.Add(entity);
            }
            else
            {
                wce.Team1.Add(entity);
            }
        }
        else
        {//If no combat event is found, we create a new one
            wce = EntityManager.Instance.NewCombatEvent(entity, Entity);
        }
        //We set thecombat event and combat target.
        CurrentCombatEvent = wce;

        CurrentTarget = entity;

    }

    public void Tick()
    {
        //If in combat, updating will be done in main update function
        if (InCombat)
        {

            //Entity.GetLoadedEntity().SpeechBubble.SetText(Entity.EntityAI.ToString());

            return;
        }
        else
        {
            Entity.GetLoadedEntity().LEPathFinder.Tick();

            //If not currently in combat, gather all near entities
            NearEntities = EntityManager.Instance.GetEntitiesNearChunk(Entity.LastChunkPosition);
            //If no near entities, then no combat loop to run
            if (NearEntities == null || NearEntities.Count == 0)
                return;
            //Iterate all near entities
            foreach (Entity ent in NearEntities)
            {
                //Skip this entity
                if (ent.Equals(Entity))
                    continue;
                //Check if we can see this entity
                if (CanSeeEntity(ent))
                {
                    if (ShouldCombat(ent))
                    {
                        Debug.Log("[EntityCombatAI] Entity " + Entity + " has seen Entity " + ent + " and is entering combat");
                        //Enter into combat
                        CurrentCombatEvent = EntityManager.Instance.NewCombatEvent(Entity, ent);
                        CurrentTarget = ent;

                        //Store details of entity
                        LastTargetDetails = new LastTargetDetails(ent);

                    }

                }
            }
            //Update the path finder
            Debug.Log("tick?");
        }
    }
    public virtual void Update()
    {
        if (InCombat)
        {
            Entity.GetLoadedEntity().LEPathFinder.Tick();
            //If the combat event is complete, then run no combat update
            if (CurrentCombatEvent.IsComplete)
            {
                IsRunningFromCombat = false;
                return;
            }

            //If we have a target
            if (CurrentTarget != null)
            {
                if (ShouldRun(CurrentTarget))
                    RunFromCombat();

                Vector2 combatDisplacement = Entity.Position2 - CurrentTarget.Position2;
                float distance = combatDisplacement.magnitude;
                ChooseEquiptWeapon();
                Weapon equipt = Entity.CombatManager.GetEquiptWeapon();

                //If we are unarmed, or the weapon is NOT ranged, then melee combat
                if(equipt == null || !(equipt is RangeWeapon))
                {
                    MeleeCombat();
                }//If the weapon in range, range combat
                else
                {
                    RangeCombat();
                }

                #region old_code
                /*
                if (equipt == null)
                {
                    //If unarmed, melee attack
                    MeleeCombat();
                }//If a range weapon is equipt
                if (equipt is RangeWeapon)
                {
                    //If too close to use range weapon, check for melee weapon
                    if (distance < 4)
                    {
                        if (humEnt.EquiptmentManager.HasMeleeWeapon())
                        {
                            //If we have a melee weapon, equipt and enter combat
                            humEnt.EquiptmentManager.EquiptMeleeWeapon();
                            MeleeCombat();
                        }
                        else
                        {
                            //If we have currently have no melee weapon, run away
                            RunFromCombat();
                        }
                    }
                    else
                    {
                        //If we are further than 4, then range fight
                        RangeCombat();
                    }
                }//If we have a melee weapon
                else if (equipt is Weapon)
                {
                    //if we are far away, check for range weapon
                    if (distance > 10)
                    {
                        if (humEnt.EquiptmentManager.HasRangeWeapon())
                        {
                            //If we have one, enter range combat
                            humEnt.EquiptmentManager.EquiptRangeWeapon();
                            RangeCombat();
                        }
                        else if (humEnt.EquiptmentManager.HasMeleeWeapon())
                        {
                            //If not, enter melee combat
                            humEnt.EquiptmentManager.EquiptMeleeWeapon();
                            MeleeCombat();
                        }
                    }
                    else
                    {
                        MeleeCombat();
                    }
                }*/
                #endregion
            }

        }
    }

    /// <summary>
    /// Called when an entity is subject to an attack.
    /// Used to determine how this entity should react
    /// </summary>
    /// <param name="source"></param>
    public abstract void OnDealDamage(Entity source);

    protected virtual void RangeCombat()
    {
        //TODO
        Entity.LookAt(CurrentTarget.Position2);
    }

    protected virtual IEnumerator LookForDamageSource(Entity source, float searchTime=5)
    {
        float time = 0;
        Vector3 initialPos = source.Position;
        //Desired look angle is based on direction of initial position + a random value of noise
        //float angle = Vector2.SignedAngle(Vector2.up, Entity.Position2-initialPos.XZ()) /*+ GenerationRandom.RNG.GaussianFloat(0, 5)*/;
        float angle = Vector3.SignedAngle(Vector3.forward, Entity.Position - initialPos, Vector3.up) + 90;

        Debug.Log("ang: " + angle + " cur look:" + Entity.LookAngle);
        bool hasFound = false;

        while(time < searchTime && !hasFound)
        {
            time += Time.deltaTime;
            //Debug.Log("Running: " + time);
            Entity.GetLoadedEntity().LookTowardsPoint(source.Position);
          //  Entity.GetLoadedEntity().SetDesiredLookAngle(angle);
            //Entity.SetLookAngle(Mathf.LerpAngle(Entity.LookAngle, angle, Time.deltaTime));

            if (CanSeeEntity(source))
            {
                hasFound = true;
                if (ShouldCombat(source))
                {
                    Attack(source);
                    Entity.GetLoadedEntity()?.SpeechBubble.PushMessage("Seen combat source -> Attacking");
                }
                else
                {
                    CurrentTarget = source;
                    RunFromCombat(source.TilePos);
                    Entity.GetLoadedEntity()?.SpeechBubble.PushMessage("Seen combat source -> Runnings");
                }                    
            }

            yield return null;
        }
        if (!hasFound)
        {
            Entity.GetLoadedEntity()?.SpeechBubble.PushMessage("Cannot see combat source -> Runnings");
            CurrentTarget = source;
            RunFromCombat(source.TilePos);
        }
            
        
        


    }

    protected virtual void MeleeCombat()
    {
        currentCombatTask = "melee combat\n";
        Entity.LookAt(CurrentTarget.Position2);

        float attackRange = Entity.CombatManager.GetCurrentWeaponRange();
        Entity.LookAt(CurrentTarget.Position2);

        Vector2 combatDisplacement = Entity.Position2 - CurrentTarget.Position2;
        float distance = combatDisplacement.magnitude;
        //Valid if the subworlds are the same
        bool validSubworld = (CurrentTarget.CurrentSubworldID == Entity.CurrentSubworldID);

        if (distance < attackRange && validSubworld)
        {
            if (Entity.CombatManager.CanAttack())
            {
                currentCombatTask += " - attacking!";
                Entity.CombatManager.UseEquiptWeapon();
            }
            else
            {
                currentCombatTask += "waiting to attack";
            }
                
        }
        else if(distance > attackRange && validSubworld)
        {
            currentCombatTask += " too far too attack (dist/range): " + distance + "/" + attackRange;
            RunToCombat();
        }else if (!validSubworld)
        {//If target is in a different subworld we must find the exit
            //If our target is in the main world, we must find OUR subworld exit
            if (CurrentTarget.CurrentSubworldID == -1)
            {
                Subworld ourSW = Entity.GetSubworld();
                Entity.GetLoadedEntity().SpeechBubble.PushMessage("Pursuing " + CurrentTarget + " through to world");
                Entity.GetLoadedEntity().LEPathFinder.SetTarget((ourSW.Exit as WorldObjectData).Position, PersueThroughDoor, new object[] { ourSW.Exit, CurrentTarget });
            }
            else
            {
                Debug.Log(CurrentTarget.CurrentSubworldID);
                //If we are in the world, and need to get to a subworld.
                Subworld targetSW = World.Instance.GetSubworld(CurrentTarget.CurrentSubworldID);
                Entity.GetLoadedEntity().SpeechBubble.PushMessage("Pursuing " + CurrentTarget + " through to subworld");
                Entity.GetLoadedEntity().LEPathFinder.SetTarget((targetSW.Entrance as WorldObjectData).Position, PersueThroughDoor, new object[] { targetSW.Entrance, CurrentTarget });
            }





        }
    }

    public void ExitThroughDoor(params object[] args)
    {
        Debug.Log("reached door?");
        ISubworldEntranceObject entrObjec = args[0] as ISubworldEntranceObject;
        WorldObjectData objDat = entrObjec as WorldObjectData;
        WorldObject obj = objDat.LoadedObject;

        obj.OnEntityInteract(Entity);
        IsRunningFromCombat = false;
        RunFromCombat();
    }

    /// <summary>
    /// Default RunFromCombat results in the entity moving in directly the 
    /// opposite direction from the current target.
    /// </summary>
    protected virtual void RunFromCombat(Vec2i combatPosition=null)
    {
        currentCombatTask = "running from combat";

        //If we are currently running, we don't need to update
        if (IsRunningFromCombat)
            return;
        Entity.GetLoadedEntity()?.SpeechBubble.PushMessage(Entity + " is running from combat");
        WorldManager.Instance.StartCoroutine(RunFromCombatCoolDown(5));

        //We now define that we are running from combat
        IsRunningFromCombat = true;
        //If the combats position is not defined, then we set it to the entities position
        if (combatPosition == null)
            combatPosition = Entity.TilePos;

        Debug.Log("Running");

        if(Entity.GetSubworld() != null)
        {
            Subworld sub = Entity.GetSubworld();
            //TODO - sub if small?
            Debug.Log("In subworld, exit: " + (sub.Exit as WorldObjectData).Position  );
            Entity.GetLoadedEntity().SpeechBubble.PushMessage("Running to subworld exit");
            //WorldObject obj = (sub.Exit as WorldObjectData).LoadedObject;
            Entity.GetLoadedEntity().LEPathFinder.SetTarget((sub.Exit as WorldObjectData).Position, ExitThroughDoor,callbackArgs: new object[] { sub.Entrance, 0.5f });

        }
        else
        {
            Debug.Log("not in subworld");
            Vector2 movementDirection = Entity.Position2 - combatPosition.AsVector2();


            //If our movement is 0, we define it to be in a random direction.
            if (movementDirection == Vector2.zero)
            {
                movementDirection = GameManager.RNG.RandomVector2(-1, 1).normalized;
            }
            //Define the target position as at least 2 chunks away
            Vector2 targetPosition = Entity.Position2 + movementDirection * 32;
            //Set the AI target
            Entity.GetLoadedEntity().LEPathFinder.SetTarget(targetPosition);
        }      
    }

 

    public void PersueThroughDoor(params object[] args)
    {
        ISubworldEntranceObject entrObjec = args[0] as ISubworldEntranceObject;
        WorldObjectData objDat = entrObjec as WorldObjectData;
        WorldObject obj = objDat.LoadedObject;

        obj.OnEntityInteract(Entity);
        Entity.GetLoadedEntity().LEPathFinder.SetEntityTarget(CurrentTarget);
        
    }

    private IEnumerator RunFromCombatCoolDown(float cooldown)
    {
        yield return new WaitForSeconds(cooldown);
        IsRunningFromCombat = false;
    }


    /// <summary>
    /// Default RunToCombat causes the entity to look at its target
    /// If it has line of sight, it runs directly towards 
    /// </summary>
    protected virtual void RunToCombat()
    {


        
        Entity.GetLoadedEntity().LEPathFinder.SetEntityTarget(CurrentTarget, 1.5f);
        Entity.LookAt(CurrentTarget.Position2);
        Entity.GetLoadedEntity().SetRunning(true);

        return;
        if (LineOfSight(CurrentTarget))
        {
            DebugGUI.Instance.SetData(Entity.Name, "line of sight");
            Vector2 movement = CurrentTarget.Position2 - Entity.Position2;
            Entity.GetLoadedEntity().MoveInDirection(movement);
        }
        else if (Entity.EntityAI.GeneratePath(Vec2i.FromVector3(CurrentTarget.Position)))
        {
            DebugGUI.Instance.SetData(Entity.Name, "path found");
            //If a valid path can/has been generated
            Entity.EntityAI.FollowPath();
        }
        else
        {
            DebugGUI.Instance.SetData(Entity.Name, "no line of sight");

            Vector2 movement = CurrentTarget.Position2 - Entity.Position2;
            Entity.GetLoadedEntity().MoveInDirection(movement);
        }
    }


    public override string ToString()
    {
        string data = "";
        if (InCombat)
        {
            data += "In combat with ";
            data += CurrentTarget == null ? "null" : CurrentTarget.ToString();
            data += "\n" + currentCombatTask;
        }

        return data;
    }





    public virtual void CheckEquiptment() { }
    



    /// <summary>
    /// Checks if this entity can see the other entity, checks only based on look direction and FOV
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool CanSeeEntity(Entity other)
    {
        float LookAngle = Entity.LookAngle;
        Vector3 Position = Entity.Position;
        float fov = Entity.fov;


        Vector3 entityLookDirection = new Vector3(Mathf.Sin(LookAngle * Mathf.Deg2Rad), 0, Mathf.Cos(LookAngle * Mathf.Deg2Rad));
        Vector3 difPos = new Vector3(other.Position.x - Position.x, 0, other.Position.z - Position.z).normalized;
        //float angle = Vector3.Angle(entityLookDirection, difPos);
        float dot = Vector3.Dot(entityLookDirection, difPos);
        
        float angle = Mathf.Abs(Mathf.Acos(dot) * Mathf.Rad2Deg);
        //Debug.Log(entityLookDirection + ", " + difPos + ", " + angle);
        if (angle > fov)
            return false;
        //Debug.Log("object in way");
        return LineOfSight(other);
    }

    public float AngleBetweenLookAndEntity(Entity other)
    {
        float LookAngle = Entity.LookAngle;
        Vector3 Position = Entity.Position;
        float fov = Entity.fov;


        Vector3 entityLookDirection = new Vector3(Mathf.Sin(LookAngle * Mathf.Deg2Rad), 0, Mathf.Cos(LookAngle * Mathf.Deg2Rad));
        Vector3 difPos = new Vector3(other.Position.x - Position.x, 0, other.Position.z - Position.z).normalized;
        //float angle = Vector3.Angle(entityLookDirection, difPos);
        float dot = Vector3.Dot(entityLookDirection, difPos);

        float angle = Mathf.Abs(Mathf.Acos(dot) * Mathf.Rad2Deg);
        return angle;
    }

    /// <summary>
    /// Returns true if there is no opaque world object blocking the direct line
    /// of sight between this entity and the entity i question.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool LineOfSight(Entity other)
    {

        Vector3 dir = other.Position - Entity.Position;
        RaycastHit[] hit = Physics.RaycastAll(Entity.Position + Vector3.up * 1.5f, dir, dir.magnitude);

        foreach (RaycastHit h in hit)
        {
            GameObject blocking = h.transform.gameObject;
            if (blocking.GetComponent<LoadedEntity>() != null)
            {
                continue; ;
            }
            else if (blocking.CompareTag("MainCamera"))
                continue;
            else if (blocking.GetComponent<LoadedChunk2>() != null)
                continue;
            else
            {
                return false;
            }

        }
        return true;
    }



}  
/// <summary>
/// Details about the combat target on the last tick
/// Used to check if an entity has changed subworld.
/// </summary>
struct LastTargetDetails
{
    public bool IsInstance;
    public int SubworldID;
    public Vector3 Position;

    public LastTargetDetails(Entity entity)
    {
        SubworldID = entity.CurrentSubworldID;
        Position = entity.Position;
        IsInstance = true;
    }
}