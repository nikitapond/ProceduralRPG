using UnityEngine;
using UnityEditor;
[System.Serializable]
public abstract class EntityTaskAI 
{
    protected Entity Entity;
    public EntityTask CurrentTask { get; protected set; }
    public void SetEntity(Entity e)
    {
        Entity = e;
    }
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
    public virtual void Tick()
    {
        Debug.BeginDeepProfile("internal_ai_tick");
        //Check if we need a new task
        if(CurrentTask == null || CurrentTask.IsComplete)
        {
            //if so, choose our idle task
            CurrentTask = ChooseIdleTask();
        }
        if(CurrentTask != null)
        {
            CurrentTask.Tick();
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

                }
            }
            else
            {
                CurrentTask.OnTaskEnd();
                CurrentTask = task;

            }

        }
        else
        {
            CurrentTask = task;

        }
 


    }

    public override string ToString()
    {
        if (CurrentTask == null)
            return "no task";
        return CurrentTask.ToString();
    }
}