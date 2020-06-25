using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
/// <summary>
/// A settlement economy is always loaded, even if the settlement is not.
/// It contain
/// </summary>
public class SettlementEconomy
{

    

    //Each economic production that occurs per tick
    private Dictionary<EconomicProduction, int> EconomicProduction;

    private SettlementProductionAbility ProductionAbility;

    //The amount of each resource used per tick

    //Inventory holds all of a settlements economic items until use
    public EconomicInventory Inventory;
    //The amount of resources required per tick - food, 
    public Dictionary<EconomicItem, int> UsePerTick;
    //The amount of raw resources produced per tick 
    public Dictionary<EconomicItem, int> RawProductionPerTick;

    //The desired amount of required (food etc) stock kept in the inventory
    public Dictionary<EconomicItem, int> DesiredItemStock;

    //The required stock - food/clothes/weapons etc
    public Dictionary<EconomicItem, int> RequiredImports;
    //Desired imports for economic production
    //public Dictionary<EconomicItem, int> DesiredImports;

    public Dictionary<EconomicItem, int> Surplus;

    public Dictionary<EconomicItemType, Dictionary<EconomicItem, int>> SurlusByExportType;
    private Dictionary<EconomicItemType, int> SurplusByTypeCount;

    public int SettlementID;
    public Settlement Settlement { get { return World.Instance.GetSettlement(SettlementID); } }
    public int[] NearSettlementsIDs;

    public bool HasTrader;

    //A list of all entity groups that have originated from this settlement
    private List<EntityGroup> CurrentActiveGroups;




    public SettlementEconomy(SettlementGenerator2.SettlementShell shell)
    {
        shell.Economy = this;
        Inventory = shell.StartInventory;
        UsePerTick = shell.UsePerTick;
        RawProductionPerTick = shell.RawProductionPerTick;
        ProductionAbility = shell.ProductionAbility;
        DesiredItemStock = shell.DesiredStock;
        EconomicProduction = shell.EconomicProduction;

        RequiredImports = new Dictionary<EconomicItem, int>();
       // DesiredImports = new Dictionary<EconomicItem, int>();
        Surplus = new Dictionary<EconomicItem, int>();
        SurlusByExportType = new Dictionary<EconomicItemType, Dictionary<EconomicItem, int>>();
        SurplusByTypeCount = new Dictionary<EconomicItemType, int>();

        CurrentActiveGroups = new List<EntityGroup>();
    }

    /// <summary>
    /// Updates the settlement economy for this tick.
    /// First adds all raw production to the settlement inventory,
    /// we then subtract all the required use per tick
    /// Then attempts to run all productions available, taking from the inventory
    /// its produce will then be added to the inventory
    /// </summary>
    public void Tick()
    {
        //We first iterate all the raw production and add to inventory
        foreach (KeyValuePair<EconomicItem, int> kvp in RawProductionPerTick)
        {
            Inventory.AddItem(kvp.Key, kvp.Value);
        }
        //We then interate all the use per tick andsubtract from the inventory
        foreach (KeyValuePair<EconomicItem, int> kvp in UsePerTick)
        {
            int rem = Inventory.RemoveItem(kvp.Key, kvp.Value);

            if (rem < 0)
            {
               // Debug.Log(World.Instance.GetSettlement(SettlementID).ToString() + " has run out of " +  kvp.Key);
               // Debug.Log("RUN OUT OF " + kvp.Key);
            }
        }

        //We iterate all the productions, and calculate their productions
        foreach(KeyValuePair<EconomicProduction, int> prod in EconomicProduction)
        {
            CalculateEconomicProduction(prod.Key, prod.Value);           
        }

        Surplus.Clear();
        SurlusByExportType.Clear();
        SurplusByTypeCount.Clear();
        //We order our surplus by item type - ready for exports

        int surplusCount = 0;

        RequiredImports.Clear();
        //We iterate all the items in our inventory
        foreach (KeyValuePair<EconomicItem, int> allItems in Inventory.GetAllItems())
        {
            int itemCount = allItems.Value;
            //If this item has a desired stock level
            if(DesiredItemStock.TryGetValue(allItems.Key, out int desiredStock))
            {
                //We calculate our surplus
                int surplus = itemCount - desiredStock;
                //if negative, then we add to required imports
                if(surplus < 0)
                {
                    if (!RequiredImports.ContainsKey(allItems.Key))
                        RequiredImports.Add(allItems.Key, 0);
                    RequiredImports[allItems.Key] -= surplus;
                }
                else
                {//if positive, we add to surplus
                    if (!Surplus.ContainsKey(allItems.Key))
                        Surplus.Add(allItems.Key, 0);
                    Surplus[allItems.Key] += surplus;
                    surplusCount+= surplus;

                    //We check if we already have included a surplus of this export type
                    if (!SurplusByTypeCount.ContainsKey(allItems.Key.ExportType))
                        SurplusByTypeCount.Add(allItems.Key.ExportType, 0);

                    SurplusByTypeCount[allItems.Key.ExportType] += surplus;


                }
            }
            else
            {
                if (!Surplus.ContainsKey(allItems.Key))
                    Surplus.Add(allItems.Key, 0);
                //We check if we already have included a surplus of this export type
                if (!SurplusByTypeCount.ContainsKey(allItems.Key.ExportType))
                    SurplusByTypeCount.Add(allItems.Key.ExportType, 0);

                Surplus[allItems.Key] += allItems.Value;
                SurplusByTypeCount[allItems.Key.ExportType] += allItems.Value;

            }

        }

        //If this settlement is a village, then we desire to export any surplus produce to near by places
        if(Settlement.SettlementType == SettlementType.VILLAGE)
        {
            int foodSurplus = 0;
            int animalSurplus = 0;
            int oreSurplus = 0;
            int woodSurplus = 0;
            SurplusByTypeCount.TryGetValue(EconomicItemType.food, out foodSurplus);
            SurplusByTypeCount.TryGetValue(EconomicItemType.animalProduce, out animalSurplus);
            SurplusByTypeCount.TryGetValue(EconomicItemType.ore, out oreSurplus);
            SurplusByTypeCount.TryGetValue(EconomicItemType.wood, out woodSurplus);

            if (foodSurplus > 2000)
                VillageExport(EconomicItemType.food);
            if(animalSurplus > 1000)
                VillageExport(EconomicItemType.animalProduce);
            if (oreSurplus > 1000)
                VillageExport(EconomicItemType.ore);

           
            if (woodSurplus > 1000)
                VillageExport(EconomicItemType.wood);

        }
        //if this settlemet is a village, then we check how much surplus stuff we have

    }

    private bool HasEntityGroupOfType(EntityGroup.GroupType type)
    {
        foreach (EntityGroup g in CurrentActiveGroups)
            if (g.Type == type)
                return true;
        return false;
    }

    /// <summary>
    /// Attempts to export produce for a village.
    /// We first check if a group exporting this produce type exists
    /// If so, then we do not send another
    /// If not, then we remove the exported items from the settlement inventory, and add
    /// them to the traders inventory. We then send this data to 
    /// </summary>
    private void VillageExport(EconomicItemType type)
    {
        
        EntityGroup.GroupType groupType = type.GetTypeFromExport();


        if (HasEntityGroupOfType(groupType))
        {
            return;
        }
            
        EconomicInventory traderInventory = new EconomicInventory();
        //We iterate the surplus and add any relevent items to the inventory
        foreach (KeyValuePair<EconomicItem, int> surplus in Surplus)
        {
            //If the surplus item is of correct type
            if (surplus.Key.ExportType == type)
            {
                //We add to the trader inventory, remove it from the settlement inventory
                traderInventory.AddItem(surplus.Key, surplus.Value);
                Inventory.RemoveItem(surplus.Key, surplus.Value);
                //And then finally remove from the surplus
                //Surplus.Remove(surplus.Key);
            }
        }        
        //Create the group
        //TODO - decide which entities will form the group
        EntityGroup group = WorldEventManager.Instance.SpawnVillageTrader(this, type, traderInventory, null);
        if (group != null)
            Debug.Log("Group of type " + group.Type + " exporting from " + Settlement);
        else
            Debug.Log("Null group");
        if (group == null)
            Inventory.AddAll(traderInventory.GetAllItems());
        else
        {
            CurrentActiveGroups.Add(group);
            foreach(KeyValuePair<EconomicItem, int> kvp in traderInventory.GetAllItems())
            {
                Surplus.Remove(kvp.Key);
            }
        }
    }

    public void EntityGroupReturn(EntityGroup g)
    {
        CurrentActiveGroups.Remove(g);
    }

    public bool CanImport(EconomicItemType type)
    {
        //Village imports nothing
        if (Settlement.SettlementType == SettlementType.VILLAGE)
            return false;
        if (type == EconomicItemType.food)
            return true;

        if (type == EconomicItemType.animalProduce)
            return ProductionAbility.HasButcher;
        if (type == EconomicItemType.ore)
            return ProductionAbility.HasSmelter;
        if (type == EconomicItemType.wood)
            return ProductionAbility.HasLumberMill;

        return false;

    }



    /// <summary>
    /// Calculates the economic production.
    /// We calculate the total usage of required items, as well as the total production.
    /// We then check the current inventory against the required items, checking enough supplies are present.
    /// If not, we scale down the total production.
    /// </summary>
    /// <param name="prod"></param>
    /// <param name="count"></param>
    private void CalculateEconomicProduction(EconomicProduction prod, int count)
    {
  

        Dictionary<EconomicItem, int> totalRequiredItems = new Dictionary<EconomicItem, int>();
        Dictionary<EconomicItem, int> totalProducedItems = new Dictionary<EconomicItem, int>();

        //We iterate all items required to do 1 production
        foreach (EconomicItem it in prod.InputItems)
        {
            //
            if (!totalRequiredItems.ContainsKey(it))
                totalRequiredItems.Add(it, 0);
            totalRequiredItems[it] += count;
        }
        //Next we iterate all items produced by 1 production
        foreach(EconomicItem it in prod.OutputItems)
        {
            if (!totalProducedItems.ContainsKey(it))
                totalProducedItems.Add(it, 0);
            totalProducedItems[it] += count;
        }

        //If we cannot produce the total amount, we calculate the production mutliplier
        //We then multiply our total use, and produced amount. We round down, and this gives us the scaled amount produced
        float productionFraction = 1;
        foreach (KeyValuePair<EconomicItem, int> reqItem in totalRequiredItems)
        {
            int req = reqItem.Value;
            int stock = Inventory.ItemCount(reqItem.Key);
            //If we require more stock, then we calculate the fraction of a full production tick
            //We can run. For example, if we require 50 wheat to make 50 bread, but only have 25 wheat,
            //the mulitpier will be 0.5, and so only 25 bread would be produced
            if (req > stock)
            {
                float thisMult = ((float)stock) / ((float)req);
                if (thisMult < productionFraction)
                    productionFraction = thisMult;
            }
        }
        //Now that we know the production fraction, we can remove the required items from the inventory
        foreach (KeyValuePair<EconomicItem, int> reqItem in totalRequiredItems)
        {
            //We calculate the item count, and remove from the inventory
            int itemCount = (int)(reqItem.Value * productionFraction);
            Inventory.RemoveItem(reqItem.Key, itemCount);
        }
        //And finally, we calculate the total amount of each item produced after scaling by the production fraction
        foreach(KeyValuePair<EconomicItem, int> prodItem in totalProducedItems)
        {
            int prodCount = (int)(prodItem.Value*productionFraction);
            Inventory.RemoveItem(prodItem.Key, prodCount);
        }
        //TODO - figure out financial transfer?
        /*
         Possible ideas
         Calculate 'purchase value' will be equal to 100% of item value
         Sell back to inventory for 90% price (10% is tax?)

        Make the item production amount have a demand

         */

    }


    public void Export(Dictionary<EconomicItem, int> export)
    {
        foreach (KeyValuePair<EconomicItem, int> kvp in export)
        {
            Inventory.RemoveItem(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// Attempts to export the specified item out of this inventory.
    /// The economy will only export if the item is in surplus
    /// Will sell up to the total number surplus, or to the 'amount' specified, whichever is larger
    /// Will calculate the cost per item, and only sell up to the amount of maxMoney
    /// will return the remaining money
    /// </summary>
    /// <param name="item"></param>
    /// <param name="amount"></param>
    /// <param name="maxMoney"></param>
    /// <returns></returns>
    public bool AttemptExport(EconomicItem item, int amount, float maxMoney, out float remainingMoney, out int totalSold)
    {

        //Check if we have a surplus
        if(Surplus.TryGetValue(item, out int surplus))
        {
            totalSold = 0;

            while(surplus > 0 && amount > 0 && maxMoney > item.Value)
            {

                surplus--;
                amount--;
                maxMoney -= item.Value; //TODO - make item price vary 

                totalSold++;

            }
            remainingMoney = maxMoney;
            return true;

        }
        remainingMoney = maxMoney;
        totalSold = 0;
        return false;


    }


    /// <summary>
    /// Returns the amount of money this settlement will pay for this item, and this count of item
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public int CalculateImportValue(EconomicItem item, int count)
    {
        float tVal = 0;
        //If this item is a requirement, then we
        if (RequiredImports.ContainsKey(item))
        {

            int desiredStock = 0;
            DesiredItemStock.TryGetValue(item, out desiredStock);

            //if we have less than 10% of stock, we inflate the value of this transaction to encourage traders to come here.
            if (desiredStock > 0 && Inventory.ItemCount(item) < 0.1f * desiredStock)
            {
                tVal += count * item.Value * 1.5f;
            }

            for (int i = 0; i < count; i++)
            {
                tVal += item.Value * Mathf.Pow(1.01f, i);
            }
            return (int)tVal;
        }
        /*
        if (DesiredImports.ContainsKey(item))
        {
            for (int i = 0; i < count; i++)
            {
                tVal += item.Value * Mathf.Pow(1.005f, i);
            }
            return (int)tVal;
        }*/
        return 0;
    }

    public int Import(Dictionary<EconomicItem, int> import)
    {
        foreach (KeyValuePair<EconomicItem, int> kvp in import)
        {
            Inventory.AddItem(kvp.Key, kvp.Value);
        }
        return 0;
    }
    public override string ToString()
    {
        string res =string.Format("|{0, 30} | {1, 30} | {2 , 30} | {3, 30}| {4, 30} |\n", "Item", "Inventory", "Des Stock", "Use", "Produced");
        foreach(EconomicItem it in Economy.AllRawItems)
        {
            if (it == null)
                continue;
            int inInv = Inventory.ItemCount(it);
            int use = 0;
            int produce = 0;
            int reqStock = 0;
            UsePerTick.TryGetValue(it, out use);

            RawProductionPerTick.TryGetValue(it, out produce);

            DesiredItemStock.TryGetValue(it, out reqStock);

            if (inInv == 0 && use == 0 && produce == 0)
                continue;
            //res += it.ToString();
            res += string.Format("|{0, 30} | {1, 30} | {2 , 30} | {3, 30}| {4,30} |\n", it.ToString(), inInv ,reqStock, use, produce);
        }
        foreach (EconomicItem it in Economy.AllProducedItems)
        {
            if (it == null)
                continue;
            int inInv = Inventory.ItemCount(it);
            int use = 0;
            int produce = 0;
            int reqStock = 0;

            UsePerTick.TryGetValue(it, out use);

            RawProductionPerTick.TryGetValue(it, out produce);
            DesiredItemStock.TryGetValue(it, out reqStock);

            if (inInv == 0 && use == 0 && produce == 0)
                continue;
            //res += it.ToString();
            res += string.Format("|{0, 30} | {1, 30} | {2 , 30} | {3, 30}| {4,30} |\n", it.ToString(), inInv, reqStock, use, produce);
        }
        res += "\n Required Imports:\n";
        foreach(KeyValuePair<EconomicItem, int> kvp in RequiredImports)
        {
            res += string.Format("|{0,30} | {1, 30}\n", kvp.Key, kvp.Value);
        }
        
        res += "\n Surplus:\n";

        foreach (KeyValuePair<EconomicItem, int> kvp in Surplus)
        {
            res += string.Format("|{0,30} | {1, 30}\n", kvp.Key, kvp.Value);
        }
        //  res += Inventory.ToString();
        return res;
    }



}

public struct SettlementProductionAbility
{
    public bool HasLumberMill;
    public bool HasButcher;
    public bool HasSmelter;

    public SettlementProductionAbility(bool lumber, bool butch, bool smelt)
    {
        HasLumberMill = lumber;
        HasButcher = butch;
        HasSmelter = smelt;
    }

}