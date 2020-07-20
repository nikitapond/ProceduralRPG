using UnityEngine;
using UnityEditor;
[System.Serializable]
public abstract class EntityTask
{

    public string TaskDescription;

    public Entity Entity { get; private set; }
    public float Priority { get; private set; }
    public float TaskTime { get; private set; }


    public TaskLocation Location { get; private set; }
    public bool HasTaskLocation { get; private set; }

    private float StartTime=-1;
    public EntityTask(Entity entity, float priority, float taskTime=-1, string taskDesc = null)
    {
        TaskDescription = taskDesc;
        Entity = entity;
        Priority = priority;
        IsComplete = false;
        TaskTime = taskTime;
        HasTaskLocation = false;

        if(taskDesc != null)
        {
            if (entity.GetLoadedEntity() != null)
            {
                entity.GetLoadedEntity().SpeechBubble.PushMessage(taskDesc);
            }
        }

    }

    public EntityTask(Entity entity, TaskLocation tl, float priority, float taskTime = -1, string taskDesc = null)
    {
        TaskDescription = taskDesc;
        Entity = entity;
        Priority = priority;
        IsComplete = false;
        TaskTime = taskTime;
        Location = tl;
        HasTaskLocation = true;

        if (taskDesc != null)
        {
            if (entity.GetLoadedEntity() != null)
            {
                entity.GetLoadedEntity().SpeechBubble.PushMessage(taskDesc);
            }
        }

    }

    public bool IsComplete { get; protected set; }

    public void SetTaskLocation(TaskLocation tl)
    {
        Location = tl;
        HasTaskLocation = true;
    }

    public virtual void OnTaskEnd() { }

    public virtual bool ShouldTaskEnd()
    {
        return false;
    }

    public void Tick()
    {
        //If taskTime != -1, then the task has a finite life which we must check
        if(TaskTime != -1)
        {
            //If start time is -1, it hasn't been set, so we start now
            if (StartTime == -1)
                StartTime = Time.time;
            if (Time.time - StartTime > TaskTime)
            {
                IsComplete = true;
                return;
            }
        }

        InternalTick();
    }
    public abstract void Update();
    protected abstract void InternalTick();


    public static int QuickDistanceToPlayer(Entity entity)
    {
        return Vec2i.QuickDistance(entity.TilePos, PlayerManager.Instance.Player.TilePos);
    }
    public static int QuickDistance(Entity entity, Vec2i v)
    {
        return v.QuickDistance(entity.TilePos);
    }

    public override string ToString()
    {
    
        return TaskDescription;
    }
}
public struct TaskLocation
{
    public int SubworldWorldID;
    public Vec2i Position;
    
    public TaskLocation(int subworld, Vec2i pos)
    {
        SubworldWorldID = subworld;
        Position = pos;
    }
    public TaskLocation(Subworld subworld, Vec2i pos)
    {
        SubworldWorldID = subworld==null?-1:subworld.SubworldID;
        Position = pos;
    }
}