using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class EntityGroupCaravan : EntityGroup
{
    private static bool BuffersCreated = false;
    private static Dictionary<Settlement, int> NearSetsBuffer;
    private static Dictionary<Settlement, int> TradeValuesBuffer;
    private static Dictionary<Settlement, Dictionary<EconomicItem, int>> SetTradesBuffer;

    public Dictionary<EconomicItem, int> Trade;
    public EntityGroupCaravan(Vec2i startChunk,  List<Entity> entities = null) : base(startChunk, entities)
    {
        Trade = new Dictionary<EconomicItem, int>();

        if (!BuffersCreated)
        {
            BuffersCreated = true;
            NearSetsBuffer = new Dictionary<Settlement, int>(40);
            TradeValuesBuffer = new Dictionary<Settlement, int>(40);
            SetTradesBuffer = new Dictionary<Settlement, Dictionary<EconomicItem, int>>(40);
        }
    }

    public override GroupType Type => EntityGroup.GroupType.Traders;


    Settlement TargetSet;
    public override bool DecideNextTile(out Vec2i nextTile)
    {
        CurrentPathIndex++;
        //If we are at the end of the path, we return false. This
        if (CurrentPathIndex == Path.Count)
        {
            nextTile = null;
            return false;
        }

        nextTile = Path[CurrentPathIndex];
        return true;
    }



    /// <summary>
    /// Calculates the best place to trade with next
    /// </summary>
    /// <param name="current"></param>
    public void DecideNextTradeTarget(Settlement current)
    {
        NearSetsBuffer.Clear();
        TradeValuesBuffer.Clear();
        SetTradesBuffer.Clear();

        Debug.BeginDeepProfile("CaravanDecide");

        //We find the surplus
        Dictionary<EconomicItem, int> surplus = current.Economy.Surplus;


        foreach(int id in current.Economy.NearSettlementsIDs)
        {
            Settlement set = World.Instance.GetSettlement(id);
            int d = Vec2i.QuickDistance(set.BaseChunk, current.BaseChunk);
            NearSetsBuffer.Add(set, d);

        }


        //We now iterate each one, and calculate the possible trade value
        foreach (KeyValuePair<Settlement, int> kvp in NearSetsBuffer)
        {
            if (kvp.Key == null)
                continue;
            SetTradesBuffer.Add(kvp.Key, new Dictionary<EconomicItem, int>());
            TradeValuesBuffer.Add(kvp.Key, 0);
            foreach (KeyValuePair<EconomicItem, int> surp in surplus)
            {
                //If the item is surplus here, and is a required import, then we add this to the trade value
                if (kvp.Key.Economy.RequiredImports.ContainsKey(surp.Key))
                {
                    //Find the surplus and desired imports
                    int surplusAmount = surp.Value;
                    int importAmount = kvp.Key.Economy.RequiredImports[surp.Key];
                    //Then trade will be for the smallest of these 2
                    int tradeAmount = Mathf.Min(surplusAmount, importAmount);
                    //We set the trades, and incriment the value
                    SetTradesBuffer[kvp.Key].Add(surp.Key, tradeAmount);
                    TradeValuesBuffer[kvp.Key] += kvp.Key.Economy.CalculateImportValue(surp.Key, tradeAmount);
                    //TradeValuesBuffer[kvp.Key] += (int)(tradeAmount * surp.Key.Value);
                }
                
            }
            TradeValuesBuffer[kvp.Key] = (int)(TradeValuesBuffer[kvp.Key] / Mathf.Pow(NearSetsBuffer[kvp.Key], 0.5f));
        }



        Settlement chosenSet = null;
        int currentValue = -1;

        foreach (KeyValuePair<Settlement, int> kvp in TradeValuesBuffer)
        {
            if (currentValue < kvp.Value)
            {
                currentValue = kvp.Value;
                chosenSet = kvp.Key;
            }
        }

        if (chosenSet == null || chosenSet.Economy == null)
        {
            Debug.Log("this is wierd" + chosenSet);
            return;
        }
        TargetSet = chosenSet;
        Trade.Clear();

        chosenSet.Economy.Export(SetTradesBuffer[chosenSet]);
        Trade = SetTradesBuffer[chosenSet];
        Debug.EndDeepProfile("CaravanDecide");
        GenerateNewPath(chosenSet.BaseChunk);
       
    }

    public override void OnGroupInteract(List<EntityGroup> other)
    {

    }

    public override void OnReachDestination(Vec2i position)
    {


        if(TargetSet != null)
        {
            TargetSet.Economy.Import(Trade);
        }
        StartChunk = position;
        CurrentChunk = position;
        DecideNextTradeTarget(TargetSet);


    }
}
