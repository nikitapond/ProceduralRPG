using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Farmer groups spawn when their settlement has reached a set amount of surplus,
/// or if their stocks run low.
/// They will travel to the settlement that is specified (nearest town/city) and sell food
/// They will then purchase items based on thier villages requirements
/// </summary>
public class EntityGroupVillageTrader : EntityGroup
{

    private VillageTraderTask Task;
    private bool HasSold;
    private GroupType Type_;


    public EntityGroupVillageTrader(VillageTraderTask task, EntityGroup.GroupType type, List<Entity> entities = null) : base(task.Start.Settlement.BaseChunk, entities, task.ToSell)
    {
        Task = task;
        HasSold = false;

        Type_ = type;

        //We create a path to the target settlement
        GenerateNewPath(task.End.Settlement.BaseChunk);

    }

    public override GroupType Type => Type_;

    /// <summary>
    /// Decides the next position of this entity group.
    /// If there is no target entity group, we follow our chosen path.
    /// A target group is an enemy group - bandits, vampires, etc
    /// </summary>
    /// <param name="nextTile"></param>
    /// <returns></returns>
    public override bool DecideNextTile(out Vec2i nextTile)
    {

        if (TargetGroup == null)
        {
            nextTile = NextPathPoint();
            return nextTile != null;
        }

        nextTile = CurrentChunk;
        return true;
    }

    public override void OnGroupInteract(List<EntityGroup> other)
    {
        //throw new System.NotImplementedException();
    }

    public override bool OnReachDestination(Vec2i position)
    {
        //If we haven't sold yet, then we are at our desitnation and must sell produce
        if (!HasSold)
        {
            Debug.Log("Village trader has sold");
            //We sell all items to the settlement
            int income = Task.End.Import(EconomicInventory.GetAllItems());
            
            //We then iterate each desired purchase and attempt to buy from settlement
            foreach(KeyValuePair<EconomicItem, int> desiredPurch in Task.DesiredPurchases)
            {
                //We attempt to buy
                if(Task.End.AttemptExport(desiredPurch.Key, desiredPurch.Value, income, out float remainingMoney, out int totalPurched)){
                    EconomicInventory.AddItem(desiredPurch.Key, totalPurched);
                    income = (int)remainingMoney;
                }
            }
            Debug.Log("Finding path back home");
            //After we have sold, we set the path as heading back to the home settlement.
            GenerateNewPath(Task.Start.Settlement.BaseChunk);
            HasSold = true;
            return true;
        }
        else
        {
            Debug.Log("back home");
            //if we have reached the destination now, we are back at our home settlement
            foreach(var kvp in EconomicInventory.GetAllItems())
            {
                Task.Start.Inventory.AddItem(kvp.Key, kvp.Value);
            }

            //As the task is complete, this group should be destoryed
            ShouldDestroy = true;
            Task.Start.EntityGroupReturn(this);
            return false;
        }
    }
}
public struct VillageTraderTask
{
    public SettlementEconomy Start;
    public SettlementEconomy End;

    public EconomicInventory ToSell;
    public Dictionary<EconomicItem, int> DesiredPurchases;
}