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



    public void Tick()
    {
        //If in combat, updating will be done in main update function
        if (InCombat)
        {
            return;
        }
        else
        {

            //If not currently in combat, gather all near entities
            List<Entity> nearEntities = GameManager.EntityManager.GetEntitiesNearChunk(Entity.LastChunkPosition);
            //If no near entities, then no combat loop to run
            if (nearEntities == null || nearEntities.Count == 0)
                return;
            Entity.GetLoadedEntity().NearEntities = nearEntities;
            //Iterate all near entities
            foreach (Entity ent in nearEntities)
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
                        CurrentCombatEvent = GameManager.EntityManager.NewCombatEvent(Entity, ent);
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

        Entity.LookAt(CurrentTarget.Position2);

        float attackRange = Entity.CombatManager.GetCurrentWeaponRange();
        Entity.LookAt(CurrentTarget.Position2);

        Vector2 combatDisplacement = Entity.Position2 - CurrentTarget.Position2;
        float distance = combatDisplacement.magnitude;

        if (distance < attackRange)
        {
            if (Entity.CombatManager.CanAttack())
                Entity.CombatManager.UseEquiptWeapon();
        }
        else
        {
            RunToCombat();
        }
    }
    /// <summary>
    /// Default RunFromCombat results in the entity moving in directly the 
    /// opposite direction from the current target.
    /// </summary>
    protected virtual void RunFromCombat()
    {
        Vector2 movement = Entity.Position2 - CurrentTarget.Position2;
        Entity.GetLoadedEntity().MoveInDirection(movement);
        Entity.LookAt(Entity.Position2 + movement);
    }
    /// <summary>
    /// Default RunToCombat causes the entity to look at its target
    /// If it has line of sight, it runs directly towards 
    /// </summary>
    protected virtual void RunToCombat()
    {
        Entity.LookAt(CurrentTarget.Position2);
        Entity.GetLoadedEntity().SetRunning(true);

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

        return LineOfSight(other);
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
