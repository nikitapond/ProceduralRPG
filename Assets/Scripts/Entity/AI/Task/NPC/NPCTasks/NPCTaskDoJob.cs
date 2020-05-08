using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
[System.Serializable]
public class NPCTaskDoJob : EntityTask
{
    private Building WorkBuilding;
    private NPCJob Job;

    public NPCTaskDoJob(Entity entity, IWorkBuilding building,  NPCJob job, float priority, float taskTime = -1) : base(entity, priority, taskTime)
    {
        WorkBuilding = building.WorkBuilding;
        Job = job;

    }

    public override void Update()
    {
        Job.JobUpdate(Entity as NPC);
 
    }

    protected override void InternalTick()
    {
        Job.JobTick(Entity as NPC);
 
    }
}