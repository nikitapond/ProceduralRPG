using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
/// <summary>
/// A settlement economy is always loaded, even if the settlement is not.
/// It contain
/// </summary>
public class SettlementEconomy
{

    private Inventory SettlementInventory;



    //Each economic production that occurs per tick
    private List<EconomicProduction> Production;
    //The amount of each resource used per tick

    public EconomicInventory Inventory;
    public Dictionary<EconomicItem, int> UsePerTick;
    public Dictionary<EconomicItem, int> ProductionPerTick;
    public Dictionary<EconomicItem, int> ItemStock;

    public Dictionary<EconomicItem, int> DesiredImports;


    public SettlementEconomy(SettlementGenerator2.SettlementShell shell)
    {
        shell.Economy = this;
        Inventory = shell.StartInventory;
        UsePerTick = shell.UserPerTick;
        ProductionPerTick = shell.ProductionPerTick;
        ItemStock = shell.DesiredStock;
        DesiredImports = new Dictionary<EconomicItem, int>();
    }


    public void Tick()
    {

        foreach(KeyValuePair<EconomicItem, int> kvp in ProductionPerTick)
        {
            Inventory.AddItem(kvp.Key, kvp.Value);
        }
        foreach (KeyValuePair<EconomicItem, int> kvp in UsePerTick)
        {
            int rem = Inventory.RemoveItem(kvp.Key, kvp.Value);

            if(rem < 0)
            {
                Debug.Log("RUN OUT OF " + kvp.Key);
            }
        }
        DesiredImports.Clear();
        //Check for deficits
        foreach (KeyValuePair<EconomicItem, int> kvp in ItemStock)
        {
            int invCount = Inventory.ItemCount(kvp.Key);
            //If we currently have less than the desired amount, we set the difference of our desired imports
            if(invCount < kvp.Value)
            {
                DesiredImports.Add(kvp.Key, kvp.Value - invCount);
            }
        }


    }

    public override string ToString()
    {
        string res = string.Format("|{0, 30} | {1, 30} | {2 , 30} | {3, 30}|\n", "Item", "Inventory", "Use", "Produced");
        foreach(EconomicItem it in Economy.AllRawItems)
        {
            if (it == null)
                continue;
            int inInv = Inventory.ItemCount(it);
            int use = 0;
            int produce = 0;
            UsePerTick.TryGetValue(it, out use);

            ProductionPerTick.TryGetValue(it, out produce);

            if (inInv == 0 && use == 0 && produce == 0)
                continue;
            //res += it.ToString();
            res += string.Format("|{0, 30} | {1, 30} | {2 , 30} | {3, 30}|\n", it.ToString(), inInv , use, produce);

        }
      //  res += Inventory.ToString();
        return res;
    }



}