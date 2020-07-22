using UnityEngine;
using UnityEditor;
[System.Serializable]
public abstract class EntityTaskAI 
{
    protected Entity Entity;
    public EntityTask CurrentTask { get; protected set; }
    public void SetEntity(Entity e)
    {
        Debug.Log("Entity set" + e);
        Entity = e;


    }

    public bool HasTask { get { return CurrentTask != null && !CurrentTask.IsComplete; } }

    /// <summary>
    /// Decides and returns the task this entity should do when 
    /// there are no external stimuli.
    /// </summary>
    /// <returns></returns>
    public abstract EntityTask ChooseIdleTask();
    public virtual void Update()
    {
        if(CurrentTask != null && !CurrentTask.IsComplete)
        {
            CurrentTask.Update();
        }
    }
    public void OnEntityLoad()
    {
        if (Entity == null)
            return;
        EntityTask task = ChooseIdleTask();
        if (task != null)
            SetTask(task);
    }

    public virtual void Tick()
    {
        Debug.BeginDeepProfile("internal_ai_tick");
        //Check if we need a new task
        if(CurrentTask == null || CurrentTask.IsComplete || CurrentTask.ShouldTaskEnd() || WorldManager.Instance.Time.TimeChange)
        {
            
            //if so, choose our idle task
            EntityTask task = ChooseIdleTask();
            if(task != null)
                SetTask(task);
        }
        if(CurrentTask != null)
        {
            if (CurrentTask.IsComplete || CurrentTask.ShouldTaskEnd() || WorldManager.Instance.Time.TimeChange)
            {
                Entity.GetLoadedEntity().SpeechBubble.PushMessage("Task " + CurrentTask + " complete");
                CurrentTask = null;
                EntityTask task = ChooseIdleTask();
                if (task != null)
                    SetTask(task);
            }
            else
            {
                CurrentTask.Tick();
            }

            
        }
        Debug.EndDeepProfile("internal_ai_tick");
    }

    public void SetTask(EntityTask task, bool priorityCheck=true)
    {

        if(CurrentTask != null)
        {

            if (priorityCheck)
            {
                if(task.Priority > CurrentTask.Priority)
                {
                    CurrentTask.OnTaskEnd();
                    CurrentTask = task;
                    Entity.GetLoadedEntity()?.SpeechBubble.PushMessage("New task has higher priority : " + CurrentTask);

                }
            }
            else
            {
                CurrentTask.OnTaskEnd();
                CurrentTask = task;
                Entity.GetLoadedEntity()?.SpeechBubble.PushMessage("New task, skip priority check : " + CurrentTask);

            }

        }
        else
        {
            CurrentTask = task;
            Entity.GetLoadedEntity()?.SpeechBubble.PushMessage("No task, new task : " + CurrentTask);

        }



    }

    public override string ToString()
    {
        if (CurrentTask == null)
            return "no task";
        return CurrentTask.ToString();
    }
}