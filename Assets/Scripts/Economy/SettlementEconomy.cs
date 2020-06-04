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
    //The amount of each resource used per tick

    //Inventory holds all of a settlements economic items until use
    public EconomicInventory Inventory;
    //The amount of resources required per tick - food, 
    public Dictionary<EconomicItem, int> UsePerTick;
    //The amount of raw resources produced per tick 
    public Dictionary<EconomicItem, int> ProductionPerTick;

    //The desired amount of required (food etc) stock kept in the inventory
    public Dictionary<EconomicItem, int> DesiredItemStock;

    //The required stock - food/clothes/weapons etc
    public Dictionary<EconomicItem, int> RequiredImports;
    //Desired imports for economic production
    public Dictionary<EconomicItem, int> DesiredImports;

    public Dictionary<EconomicItem, int> Surplus;

    public SettlementEconomy(SettlementGenerator2.SettlementShell shell)
    {
        shell.Economy = this;
        Inventory = shell.StartInventory;
        UsePerTick = shell.UserPerTick;
        ProductionPerTick = shell.RawProductionPerTick;
        DesiredItemStock = shell.DesiredStock;
        EconomicProduction = shell.EconomicProduction;

        RequiredImports = new Dictionary<EconomicItem, int>();
        DesiredImports = new Dictionary<EconomicItem, int>();
        Surplus = new Dictionary<EconomicItem, int>();
    }


    public void Tick()
    {
        //We first iterate all the raw production and add to inventory
        foreach(KeyValuePair<EconomicItem, int> kvp in ProductionPerTick)
        {
            Inventory.AddItem(kvp.Key, kvp.Value);
        }
        //We then interate all the use per tick andsubtract from the inventory
        foreach (KeyValuePair<EconomicItem, int> kvp in UsePerTick)
        {
            int rem = Inventory.RemoveItem(kvp.Key, kvp.Value);

            if(rem < 0)
            {
                Debug.Log("RUN OUT OF " + kvp.Key);
            }
        }
        //We clear the imports 
        DesiredImports.Clear();
        RequiredImports.Clear();
        //Check for deficits in the required stock (food etc)
        foreach (KeyValuePair<EconomicItem, int> kvp in DesiredItemStock)
        {
            int invCount = Inventory.ItemCount(kvp.Key);
            //If we currently have less than the desired amount, we set the difference of our desired imports
            if(invCount < kvp.Value)
            {
                RequiredImports.Add(kvp.Key, kvp.Value - invCount);
            }
        }


        //We next iterate all of the productions
        foreach(KeyValuePair<EconomicProduction, int> kvp in EconomicProduction)
        {
            //The items required to do 1 of this production
            EconomicItem[] requiredItems = kvp.Key.InputItems;

            if (debug)
            {
                string de = "";
                foreach (EconomicItem it in requiredItems)
                {
                    de += it + ",";
                }
                Debug.Log(de);
                    
            }


            Dictionary<EconomicItem, int> totalRequiredItems = new Dictionary<EconomicItem, int>();
            //We iterate all items required to do 1 production
            foreach(EconomicItem it in requiredItems)
            {
                if (!totalRequiredItems.ContainsKey(it))
                {
                    totalRequiredItems.Add(it, 0);
                }                   
                
                totalRequiredItems[it] += kvp.Value;
            }
            //If we cannot produce the total amount, we calculate the production mutliplier
            //We then multiply our total use, and produced amount. We round down, and this gives us the scaled amount produced
            float productionMultiplier = 1;
            foreach(KeyValuePair<EconomicItem, int> kvp2 in totalRequiredItems)
            {
                Debug.Log("TRI: " + kvp2.Key + "," + kvp2.Value);
                int req = kvp2.Value;
                int stock = Inventory.ItemCount(kvp2.Key);

                if (req > stock)
                {
                    float thisMult = ((float)stock) / ((float)req);
                    if(thisMult < productionMultiplier)
                        productionMultiplier = thisMult;

                    if (!DesiredImports.ContainsKey(kvp2.Key))
                        DesiredImports.Add(kvp2.Key, 0);
                    //If we don't have enough stock for this production, we add it to the desired imports
                    DesiredImports[kvp2.Key] +=  req-stock;
                }
            }

            //Now that we know the multiplier, we take the required items from the inventory
            foreach(KeyValuePair<EconomicItem, int> kvp2 in totalRequiredItems)
            {
                int amount = Mathf.FloorToInt(productionMultiplier * kvp2.Value);
                Inventory.RemoveItem(kvp2.Key, amount);
            }
            //We next calculate the total amount of items we will produce
            EconomicItem[] producedItems = kvp.Key.OutputItems;
            Dictionary<EconomicItem, int> totalProducedItems = new Dictionary<EconomicItem, int>();
            foreach (EconomicItem it in producedItems)
            {
                if (!totalProducedItems.ContainsKey(it))
                {
                    totalProducedItems.Add(it, 0);
                }

                totalProducedItems[it] += kvp.Value;
            }
            //We then iterate all, and add scaled production to inventory
            foreach (KeyValuePair<EconomicItem, int> kvp2 in totalProducedItems)
            {
                int amount = Mathf.FloorToInt(productionMultiplier * kvp2.Value);
                Inventory.AddItem(kvp2.Key, amount);
            }

        }
        Surplus.Clear();
        //We iterate all items in inventory and check against stock
        foreach(KeyValuePair<EconomicItem, int> kvp in Inventory.GetAllItems())
        {

            int desiredStock = 0;
            DesiredItemStock.TryGetValue(kvp.Key, out desiredStock);
            //If no stock is desired, then all goes to import
            if(desiredStock == 0)
            {
                Surplus.Add(kvp.Key, kvp.Value);
            }//If we have more than the desired stock, we add it to surplus
            else if(kvp.Value > desiredStock)
            {
                Surplus.Add(kvp.Key, kvp.Value - desiredStock);
            }


        }


    }

    private bool debug = false;
    public override string ToString()
    {
        debug = true;
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

            ProductionPerTick.TryGetValue(it, out produce);

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

            ProductionPerTick.TryGetValue(it, out produce);
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
        res += "\n Desired Imports:\n";
        foreach (KeyValuePair<EconomicItem, int> kvp in DesiredImports)
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