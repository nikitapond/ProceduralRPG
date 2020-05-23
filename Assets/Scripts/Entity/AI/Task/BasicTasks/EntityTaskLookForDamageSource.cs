using UnityEngine;
using UnityEditor;

/// <summary>
/// This task is started when an entity recieves damage, but they cannot see the source entity
/// </summary>
public class EntityTaskLookForDamageSource : EntityTask
{
    private Entity DamageSource;
    private Vec2i StartPosition;
    private float LookTime;
    private float StartTime;
    private Vec2i CurrentTileTarget;

    public EntityTaskLookForDamageSource(Entity entity, Entity damageSource, float lookTime) : base(entity, 100, -1, "Looking for damage source")
    {
        StartPosition = entity.TilePos;
        DamageSource = damageSource;
        LookTime = lookTime;
        StartTime = Time.time;
    }

    public override void Update()
    {
        Entity.GetLoadedEntity().LookTowardsPoint(DamageSource.Position);



        if (Entity.EntityAI.CombatAI.CanSeeEntity(DamageSource))
        {
            Entity.EntityAI.CombatAI.Attack(DamageSource);
            IsComplete = true;
            Debug.Log("Can see");
            //Entity.EntityAI.
        }
        else
        {
            Debug.Log("Cannot see");
        }

        /*
        float timePassed = Time.time - StartTime;
        //if we have spent too long looking
        if(timePassed > LookTime)
        {
            //We check if we have already stopped looking
            if (CurrentTileTarget != StartPosition)
            {
                //if not, we set our target as the start position.
                CurrentTileTarget = StartPosition;
                Entity.GetLoadedEntity().LEPathFinder.SetTarget(CurrentTileTarget.AsVector2());
            }
            else
            {
                //If our current position is close to our original position, then we are done
                if(Entity.TilePos.QuickDistance(StartPosition) < 4)
                {
                    IsComplete = true;
                }
            }

           
        }*/



    }

    protected override void InternalTick()
    {
        if (Entity.EntityAI.CombatAI.AngleBetweenLookAndEntity(DamageSource) > Entity.fov*0.7f)
        {
            Entity.GetLoadedEntity().LookTowardsPoint(DamageSource.Position2);
        }
        float timePassed = Time.time - StartTime;
        //if we have spent too long looking
        if (timePassed > LookTime)
        {
            //We check if we have already stopped looking
            if (CurrentTileTarget != StartPosition)
            {
                //if not, we set our target as the start position.
                CurrentTileTarget = StartPosition;
                Entity.GetLoadedEntity().LEPathFinder.SetTarget(CurrentTileTarget.AsVector2());
            }
            else
            {
                //If our current position is close to our original position, then we are done
                if (Entity.TilePos.QuickDistance(StartPosition) < 4)
                {
                    IsComplete = true;
                }
            }


        }else if (CurrentTileTarget == null || Entity.TilePos == CurrentTileTarget)
        {
            Vector2 direction = (DamageSource.Position2 - Entity.Position2).normalized;
            float dist = GameManager.RNG.RandomInt(2, 5);
            Vector2 target = Entity.Position2 + direction * dist;
            CurrentTileTarget = Vec2i.FromVector2(target);
            Entity.GetLoadedEntity().LEPathFinder.SetTarget(target);

        }
    }

}