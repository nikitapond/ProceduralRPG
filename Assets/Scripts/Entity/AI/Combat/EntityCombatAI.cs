using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
[System.Serializable]
public abstract class EntityCombatAI : IWorldCombatEvent
{
    protected WorldCombat CurrentCombatEvent;
    protected Entity CurrentTarget;
    public bool InCombat { get { return CurrentCombatEvent != null; } }
    protected Entity Entity;

    private bool IsRunningFromCombat;
    private List<Entity> NearEntities;

    private string currentCombatTask;


    #region abstract_functions
    protected abstract bool ShouldRun(Entity entity);
    protected abstract bool ShouldCombat(Entity entity);
    protected abstract void ChooseEquiptWeapon();
    public abstract void WorldCombatEvent(WorldCombat wce);
    #endregion

    public void SetEntity(Entity e)
    {
        Entity = e;
    }

    public void Attack(Entity entity)
    {
        WorldCombat wce = null;
        if(EntityManager.Instance.EntityInWorldCombatEvent(entity, out wce))
        {
            if (wce.Team1.Contains(entity))
            {
                wce.Team2.Add(Entity);
            }
            else
            {
                wce.Team1.Add(Entity);
            }
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
        {
            wce = EntityManager.Instance.NewCombatEvent(entity, Entity);
        }
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
                    }

                }
            }
        }
    }
    public virtual void Update()
    {
        if (InCombat)
        {
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


    protected virtual void MeleeCombat()
    {
        currentCombatTask = "melee combat\n";
        Entity.LookAt(CurrentTarget.Position2);

        float attackRange = Entity.CombatManager.GetCurrentWeaponRange();
        Entity.LookAt(CurrentTarget.Position2);

        Vector2 combatDisplacement = Entity.Position2 - CurrentTarget.Position2;
        float distance = combatDisplacement.magnitude;

        if (distance < attackRange)
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
        else
        {
            currentCombatTask += " too far too attack (dist/range): " + distance + "/" + attackRange;
            RunToCombat();
        }
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
        //We now define that we are running from combat
        IsRunningFromCombat = true;
        //If the combats position is not defined, then we set it to the entities position
        if (combatPosition == null)
            combatPosition = Entity.TilePos;

        Vector2 movementDirection = Entity.Position2 - combatPosition.AsVector2();


        //If our movement is 0, we define it to be in a random direction.
        if(movementDirection == Vector2.zero)
        {
            movementDirection = GameManager.RNG.RandomVector2(-1, 1).normalized;
        }
        //Define the target position as at least 2 chunks away
        Vector2 targetPosition = Entity.Position2 + movementDirection * 32;
        //Set the AI target
        Entity.GetLoadedEntity().LEPathFinder.SetTarget(targetPosition);
        /*
        Vector2 movement = Entity.Position2 - CurrentTarget.Position2;
        Entity.GetLoadedEntity().MoveInDirection(movement);
        Entity.LookAt(Entity.Position2 + movement);*/
    }
    /// <summary>
    /// Default RunToCombat causes the entity to look at its target
    /// If it has line of sight, it runs directly towards 
    /// </summary>
    protected virtual void RunToCombat()
    {


        //Entity.EntityAI.GeneratePath(Vec2i.FromVector3(CurrentTarget.Position));
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
