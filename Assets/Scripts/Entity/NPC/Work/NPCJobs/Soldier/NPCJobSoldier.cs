using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class NPCJobSoldier : NPCJob
{

    private enum CurrentSubTask
    {
        None,
        GettingEquiptment,
        Training,
        Patrolling,
        RemovingEquiptment,
    }

    private Vector3 CurrentTargetPosition;
    private IWorkEquiptmentObject CurrentTargetEquiptment;

    private bool HasEquiptment;
    private CurrentSubTask SubTask;



    private float CurrentTaskTime;
    private float CurrentTaskTimeout;

    private List<Item> WorkItems;

    private List<IWorkEquiptmentObject> WorkEquiptment;


    public NPCJobSoldier(IWorkBuilding workLocation) : base("Soldier", workLocation, KingdomHierarchy.Citizen)
    {
        HasEquiptment = false;
        SubTask = CurrentSubTask.None;
        WorkItems = new List<Item>();
        WorkEquiptment = new List<IWorkEquiptmentObject>(5);
        foreach (WorldObjectData obj in workLocation.WorkBuilding.GetBuildingExternalObjects())
        {
            if (obj is IWorkEquiptmentObject workObj)
                WorkEquiptment.Add(workObj);
        }
    }

    public override Color GetShirtColor => Color.red;

    public override void JobTick(NPC npc)
    {
        //If we currently have no task
        if(SubTask == CurrentSubTask.None)
        {
            if (!HasEquiptment)
            {
                SetSubTask(npc, CurrentSubTask.GettingEquiptment);
            }
        }else if(SubTask == CurrentSubTask.GettingEquiptment)
        {
            //if we have sucesfully got our equiptment
            if (HasEquiptment)
            {
                if (GameManager.RNG.RandomBool())
                {
                    //TODO - uncomment this
                    //SetSubTask(npc, CurrentSubTask.Patrolling);
                    SetSubTask(npc, CurrentSubTask.Training);
                }
                else
                {
                    SetSubTask(npc, CurrentSubTask.Training);
                }
            }
        }else if(SubTask == CurrentSubTask.Patrolling)
        {
            if(CurrentTargetPosition.WithinDistance(npc.Position, 3))
            {

                Settlement set = npc.NPCKingdomData.GetSettlement();
                CurrentTargetPosition = set.RandomPathPoint().AsVector3();
                npc.GetLoadedEntity().LEPathFinder.SetTarget(CurrentTargetPosition);
                npc.GetLoadedEntity().SpeechBubble.PushMessage("Choosing new patrol target " + CurrentTargetPosition);

            }
        }
        else if(SubTask == CurrentSubTask.Training)
        {
            if(CurrentTargetEquiptment == null)
            {
                CurrentTargetEquiptment = GameManager.RNG.RandomFromList(WorkEquiptment);
                for (int i = 0; i < 10; i++)
                    if (CurrentTargetEquiptment.CurrentUser != null)
                        CurrentTargetEquiptment = GameManager.RNG.RandomFromList(WorkEquiptment);
                    else
                    {
                        CurrentTargetEquiptment.CurrentUser = npc;
                    }
                //If no valid equiptment is found, we patrol
                if (CurrentTargetEquiptment == null)
                    SetSubTask(npc, CurrentSubTask.Patrolling);
                else
                {

                    //if the equiptment is non null, we target and travel to it
                    CurrentTargetPosition = CurrentTargetEquiptment.WorkPosition();
                    npc.GetLoadedEntity().LEPathFinder.SetTarget(CurrentTargetPosition);
                    npc.GetLoadedEntity().SpeechBubble.PushMessage("Training on object " + CurrentTargetPosition);

                }

            }
            
        }

    }

    private void SetSubTask(NPC npc, CurrentSubTask task)
    {
        SubTask = task;

        if(task == CurrentSubTask.GettingEquiptment)
        {
            npc.GetLoadedEntity().SpeechBubble.PushMessage("Walking to go get equiptment");

            //Choose random spot at work building.
            //TODO - add specific parts to each building (perhaps, we define rooms)
            CurrentTargetPosition = GameManager.RNG.RandomFromList(WorkLocation.WorkBuilding.GetSpawnableTiles()).AsVector3();
            npc.GetLoadedEntity().LEPathFinder.SetTarget(CurrentTargetPosition);
            CurrentTaskTime = -1;
            //Ensure this task does not time out till completed
            CurrentTaskTimeout = float.MaxValue;
        }else if(task == CurrentSubTask.Patrolling)
        {
            npc.GetLoadedEntity().SpeechBubble.PushMessage("Starting patrol");

            Settlement set = npc.NPCKingdomData.GetSettlement();
            CurrentTargetPosition = set.RandomPathPoint().AsVector3();
            npc.GetLoadedEntity().LEPathFinder.SetTarget(CurrentTargetPosition);
            npc.GetLoadedEntity().SpeechBubble.PushMessage("Patrol target: " + CurrentTargetPosition);

        }
        else if(task == CurrentSubTask.Training)
        {

            npc.GetLoadedEntity().SpeechBubble.PushMessage("Starting to train");
            //Choose equiptment to work at
            CurrentTargetEquiptment = GameManager.RNG.RandomFromList(WorkEquiptment);
            for (int i = 0; i < 10; i++)
                if (CurrentTargetEquiptment.CurrentUser != null)
                    CurrentTargetEquiptment = GameManager.RNG.RandomFromList(WorkEquiptment);
                else
                {
                    CurrentTargetEquiptment.CurrentUser = npc;
                }
            //If no valid equiptment is found, we patrol
            if (CurrentTargetEquiptment == null) 
                SetSubTask(npc, CurrentSubTask.Patrolling);
            else
            {

                //if the equiptment is non null, we target and travel to it
                CurrentTargetPosition = CurrentTargetEquiptment.WorkPosition();
                npc.GetLoadedEntity().LEPathFinder.SetTarget(CurrentTargetPosition);
                npc.GetLoadedEntity().SpeechBubble.PushMessage("Training on object " + CurrentTargetPosition);

            }


            if (npc.Position.WithinDistance(CurrentTargetPosition, 0.5f))
            {
                EquiptmentAssociatedTask(npc);
            }
        }
    }


    public override void JobUpdate(NPC npc)
    {
        if(SubTask == CurrentSubTask.GettingEquiptment)
        {
            //If we are close to building, we get equiptment
            if(npc.Position.WithinDistance(CurrentTargetPosition, 1) && !HasEquiptment)
            {
                npc.GetLoadedEntity().SpeechBubble.PushMessage("Equipting gear");

                //Choose random valid items from work storage and equipt.
                EquiptableItem chest = GameManager.RNG.RandomEquiptmentForSlot(WorkLocation.WorkBuilding.Inventory, EquiptmentSlot.chest);
                EquiptableItem legs = GameManager.RNG.RandomEquiptmentForSlot(WorkLocation.WorkBuilding.Inventory, EquiptmentSlot.legs);
                EquiptableItem helm = GameManager.RNG.RandomEquiptmentForSlot(WorkLocation.WorkBuilding.Inventory, EquiptmentSlot.head);
                Weapon weapon = GameManager.RNG.RandomItemFromInventoryOfType<Weapon>(WorkLocation.WorkBuilding.Inventory);
                if (chest != null)
                {
                    WorkItems.Add(chest);
                    npc.EquiptmentManager.EquiptItem(LoadedEquiptmentPlacement.chest, chest);
                }
                if (legs != null)
                {
                    WorkItems.Add(legs);
                    npc.EquiptmentManager.EquiptItem(LoadedEquiptmentPlacement.legs, legs);
                }
                if (helm != null)
                {
                    WorkItems.Add(helm);
                    npc.EquiptmentManager.EquiptItem(LoadedEquiptmentPlacement.head, helm);
                }
                if (weapon != null)
                {
                    WorkItems.Add(weapon);
                    npc.EquiptmentManager.EquiptItem(LoadedEquiptmentPlacement.weaponHand, weapon);
                }
                HasEquiptment = true;
            }
        }else if (SubTask == CurrentSubTask.Training)
        {//if we are currently training
            if(CurrentTargetEquiptment == null)
            {
                //if equiptment is null, try and choose another
                ChooseWorkEquiptment(npc);
            }
            else
            {
                if(npc.Position.WithinDistance(CurrentTargetEquiptment.WorkPosition(), 0.5f))
                {
                    EquiptmentAssociatedTask(npc);
                }
            }
        }

       
    }


    private void ChooseWorkEquiptment(NPC npc)
    {
        CurrentTargetEquiptment = GameManager.RNG.RandomFromList(WorkEquiptment);
        for (int i = 0; i < 10; i++)
            if (CurrentTargetEquiptment.CurrentUser != null)
                CurrentTargetEquiptment = GameManager.RNG.RandomFromList(WorkEquiptment);
            else
            {
                CurrentTargetEquiptment.CurrentUser = npc;
            }
    }

    private void EquiptmentAssociatedTask(NPC npc)
    {

        if (CurrentTargetEquiptment == null)
            return;
        else if(CurrentTargetEquiptment is TrainingDummy)
        {
            //make sure we are looking at the target
            npc.GetLoadedEntity().LookTowardsPoint((CurrentTargetEquiptment as WorldObjectData).Position);
            if (npc.CombatManager.CanAttack())
            {
                npc.CombatManager.UseEquiptWeapon();
                npc.GetLoadedEntity().SpeechBubble.PushMessage("Attacking dummy");

            }
        }
    }



}