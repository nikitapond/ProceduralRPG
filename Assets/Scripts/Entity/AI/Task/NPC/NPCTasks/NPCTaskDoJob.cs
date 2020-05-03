using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
[System.Serializable]
public class NPCTaskDoJob : EntityTask
{
    private Building WorkBuilding;
    private IWorkEquiptmentObject WorkEquiptment;
    bool CanWork;
    public NPCTaskDoJob(Entity entity, IWorkBuilding building,  float priority, float taskTime = -1) : base(entity, priority, taskTime)
    {
        WorkBuilding = building.WorkBuilding;
        List<IWorkEquiptmentObject> wed = new List<IWorkEquiptmentObject>(10);
        foreach(WorldObjectData wed_ in WorkBuilding.GetBuildingObjects())
        {
            if (wed_ is IWorkEquiptmentObject && (wed_ as IWorkEquiptmentObject).CurrentUser==null)
                wed.Add(wed_ as IWorkEquiptmentObject);
        }
        WorkEquiptment = GameManager.RNG.RandomFromList(wed);
        

        if(WorkEquiptment != null)
        {
            WorkEquiptment.CurrentUser = Entity as NPC;
            CanWork = QuickDistance(Entity, Vec2i.FromVector3(WorkEquiptment.WorkPosition())) <= 1;

        }
        else
        {
            CanWork = true;
        }

    }

    public override void Update()
    {
        if (CanWork)
        {
            Debug.Log("working");
            Entity.GetLoadedEntity().Jump();
        }
    }

    protected override void InternalTick()
    {
        if(WorkEquiptment != null)
        {
            if (!CanWork)
            {
                /*
                Debug.Log("Work equiptment at " + WorkEquiptment.WorldPosition);

                if (Entity.EntityAI.GeneratePath(WorkEquiptment.WorldPosition))
                {
                    if (Entity.EntityAI.FollowPath())
                        CanWork = true;
                }*/
            }
        }
 
    }
}