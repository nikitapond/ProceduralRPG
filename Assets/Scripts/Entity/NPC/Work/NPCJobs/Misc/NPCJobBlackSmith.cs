using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
/// <summary>
/// A blacksmith does not sell items. They produce items for the shop.
/// TODO - introduce skills associated with crafting (i.e, smithing, alchemy etc),
/// give blacksmiths random level.
/// They will produce weapons and armour related to their level of smithing, and add 
/// to the shop inventory.
/// 
/// Temporatily, we shall have them just walk between the forge and the anvil and 
/// add some shirts to the inventory.
/// </summary>
[System.Serializable]
public class NPCJobBlackSmith : NPCJob
{
    private Blacksmith BlackSmith;
    private List<IWorkEquiptmentObject> WorkEquiptment;


    /// <summary>
    /// The work equiptment this entity is currently situated at
    /// </summary>
    private IWorkEquiptmentObject CurrentlyWorkingAt;
    /// <summary>
    /// The time (Time.time) that this entity started working at
    /// </summary>
    private float CurrentWorkTime;
    /// <summary>
    /// The amount of time doing that this entity will continue to do this task
    /// </summary>
    private float WorkTimeLimit;

    public NPCJobBlackSmith(IWorkBuilding workLocation) : base("Blacksmith", workLocation)
    {
        BlackSmith = workLocation as Blacksmith;
        WorkEquiptment = new List<IWorkEquiptmentObject>(5);
        foreach(WorldObjectData obj in workLocation.WorkBuilding.GetBuildingExternalObjects())
        {
            if (obj is IWorkEquiptmentObject workObj)
                WorkEquiptment.Add(workObj);
        }

    }
    public override Color GetShirtColor => Color.green;

    public override void JobTick(NPC npc)
    {
        if(CurrentlyWorkingAt == null)
        {
            ChooseWorkTask(npc);
        }
        float deltaTime = Time.time - CurrentWorkTime;
        if(CurrentlyWorkingAt != null && deltaTime > WorkTimeLimit)
        {
            ChooseWorkTask(npc);
        }

    }

    public override void JobUpdate(NPC npc)
    {
        if(CurrentlyWorkingAt != null)
        {
            Vector3 workPos = CurrentlyWorkingAt.WorkPosition();
            Vector3 pathTarget = npc.GetLoadedEntity().LEPathFinder.Target;
            //Currently targeted to the work position
            if (pathTarget.WithinDistance(workPos, 1))
            {
                //If we are close to the current object
                if(workPos.WithinDistance(pathTarget, 0.2f))
                {
                    //Play working animation
                }
                //If we are not close, but our path finder target is set, we just wait till we arrive
            }
            else
            {
                npc.GetLoadedEntity().LEPathFinder.SetTarget(workPos);
                npc.GetLoadedEntity().SpeechBubble.PushMessage("Walking to work equiptment at " + workPos + " current target " + npc.GetLoadedEntity().LEPathFinder.Target);
            }
        }
    }


    /// <summary>
    /// Chooses the task for this entity based on this job.
    /// </summary>
    /// <param name="npc"></param>
    private void ChooseWorkTask(NPC npc)
    {
        if (CurrentlyWorkingAt != null)
            CurrentlyWorkingAt.CurrentUser = null;
        if (WorkEquiptment.Count == 0)
            throw new System.Exception("No work equiptment");
        CurrentlyWorkingAt = GameManager.RNG.RandomFromList(WorkEquiptment);
        for(int i=0; i<10; i++)
            if(CurrentlyWorkingAt.CurrentUser != null)
                CurrentlyWorkingAt = GameManager.RNG.RandomFromList(WorkEquiptment);
            else
            {
                CurrentlyWorkingAt.CurrentUser = npc;
            }
        CurrentWorkTime = Time.time;
        //Choose work time limit of 25-45 seconds
        WorkTimeLimit = GameManager.RNG.RandomInt(25, 45);
    }
}