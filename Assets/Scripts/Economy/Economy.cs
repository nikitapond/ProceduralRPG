﻿using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class Economy
{
    public static readonly int KEEP_IN_INVENTORY_MULT = 48 * 7; //1 tick is 30 seconds = 30 minutes in game time -> 48*7 = 1 week of storage




    // #region RAW_RESOURCES
    public static EconomicItem IronOre = new EconomicItem("Iron Ore", 1, 10, EconomicItemType.ore);
    public static EconomicItem SilverOre = new EconomicItem("Silver Ore", 2, 50, EconomicItemType.ore);
    public static EconomicItem GoldOre = new EconomicItem("Gold Ore", 3, 100, EconomicItemType.ore);

    public static EconomicItem WoodLog = new EconomicItem("Wood Log", 4, 20, EconomicItemType.wood);

    public static EconomicItem Wheat = new EconomicItem("Wheat", 5, 1, EconomicItemType.food);
    public static EconomicItem Vegetables = new EconomicItem("Vegetables", 6, 5, EconomicItemType.food);
    public static EconomicItem CowCarcus = new EconomicItem("Cow", 7, 200, EconomicItemType.animalProduce);
    public static EconomicItem SheepCarcus = new EconomicItem("Sheep", 8, 250, EconomicItemType.animalProduce);


    public static EconomicItem Silk = new EconomicItem("Silk", 9, 50, EconomicItemType.misc);
    public static EconomicItem CowHide = new EconomicItem("Cow Hide", 10, 20, EconomicItemType.animalProduce);
    public static EconomicItem Wool = new EconomicItem("Wool", 11, 30, EconomicItemType.animalProduce);

    // #endregion
    #region PRODUCED_RESOURCES
    public static EconomicItem IronBar = new EconomicItem("Iron Bar", 12, 20, EconomicItemType.bars);
    public static EconomicItem SilverBar = new EconomicItem("Silver Bar", 13, 70, EconomicItemType.bars);
    public static EconomicItem GoldBar = new EconomicItem("Gold Bar", 14, 130, EconomicItemType.bars);

    public static EconomicItem WoodPlank = new EconomicItem("Wood Plank", 15, 20, EconomicItemType.wood);

    public static EconomicItem Bread = new EconomicItem("Bread", 16, 2, EconomicItemType.food);
    public static EconomicItem Beef = new EconomicItem("Beef", 17, 30, EconomicItemType.food);
    public static EconomicItem Mutton = new EconomicItem("Mutton", 18, 35, EconomicItemType.food);

    public static EconomicItem Leather = new EconomicItem("Leather", 19, 40, EconomicItemType.animalProduce);

    public static EconomicItem LeatherArmour = new EconomicItem("Leather Armour", 20, 50, EconomicItemType.combat);
    public static EconomicItem IronArmour = new EconomicItem("Iron Armour", 21, 150, EconomicItemType.combat);
    public static EconomicItem IronWeapons = new EconomicItem("Iron Weapon", 22, 100, EconomicItemType.combat);

    public static EconomicItem FancyClothes = new EconomicItem("Fancy Clothes", 23, 50, EconomicItemType.misc);
    public static EconomicItem Clothes = new EconomicItem("Clothes", 24, 20, EconomicItemType.misc);
    #endregion

    #region PRODUCTION
    public static EconomicProduction Iron_OreToBar = new EconomicProduction(new EconomicItem[] { IronOre }, new EconomicItem[] { IronBar });
    public static EconomicProduction Silver_OreToBar = new EconomicProduction(new EconomicItem[] { SilverOre }, new EconomicItem[] { SilverBar });
    public static EconomicProduction Gold_OreToBar = new EconomicProduction(new EconomicItem[] { GoldOre }, new EconomicItem[] { GoldBar });

    public static EconomicProduction WoodLogToPlank = new EconomicProduction(new EconomicItem[] { WoodLog }, new EconomicItem[] { WoodPlank, WoodPlank, WoodPlank });

    public static EconomicProduction WheatToBread = new EconomicProduction(new EconomicItem[] { Wheat }, new EconomicItem[] { Bread });
    public static EconomicProduction CowToBeef = new EconomicProduction(new EconomicItem[] { CowCarcus }, new EconomicItem[] { Beef, Beef, Beef, Beef, Beef, Beef, Beef, Beef, Beef, Beef });
    public static EconomicProduction SheepToMutton = new EconomicProduction(new EconomicItem[] { SheepCarcus }, new EconomicItem[] { Mutton, Mutton, Mutton, Mutton, Mutton, Mutton, Mutton, Mutton, Mutton, Mutton });

    public static EconomicProduction HideToLeather = new EconomicProduction(new EconomicItem[] { CowHide }, new EconomicItem[] { Leather });

    public static EconomicProduction LeatherToArmour = new EconomicProduction(new EconomicItem[] { Leather }, new EconomicItem[] { LeatherArmour });
    public static EconomicProduction IronToArmour = new EconomicProduction(new EconomicItem[] { IronBar }, new EconomicItem[] { IronArmour });
    public static EconomicProduction IronToWeapon = new EconomicProduction(new EconomicItem[] { IronBar, WoodPlank }, new EconomicItem[] { IronWeapons });

    public static EconomicProduction SilkToClothes = new EconomicProduction(new EconomicItem[] { Silk, Silk }, new EconomicItem[] { FancyClothes });
    public static EconomicProduction WoolToClothes = new EconomicProduction(new EconomicItem[] { Wool, Wool }, new EconomicItem[] { Clothes });
    #endregion


    public static EconomicItem[] AllRawItems = { IronOre, SilverOre, GoldOre, WoodLog, Wheat, Vegetables, CowCarcus, SheepCarcus, Silk, CowHide, Wool };
    public static EconomicItem[] AllProducedItems = { IronBar, SilverBar, GoldBar, WoodPlank, Bread, Beef, Mutton, Leather, LeatherArmour, IronArmour, IronWeapons, FancyClothes, Clothes };

}


public class EconomicInventory
{

    private Dictionary<EconomicItem, int> AllItems;
    public EconomicInventory()
    {
        AllItems = new Dictionary<EconomicItem, int>();
    }
    public EconomicInventory(Dictionary<EconomicItem, int> items)
    {
        AllItems = items;
    }

    public bool HasItem(EconomicItem item)
    {
        if (item == null)
            return false;
        return AllItems.ContainsKey(item);
    }
    public int ItemCount(EconomicItem item)
    {
        if (item == null)
            return 0;
        if (AllItems.TryGetValue(item, out int count))
            return count;
        return 0;
    }
    public void AddItem(EconomicItem item, int count)
    {
        if (item == null)
            return;
        if (AllItems.ContainsKey(item))
        {
            AllItems[item] += count;
        }
        else
        {
            AllItems.Add(item, count);
        }
    }

    public void AddAll(Dictionary<EconomicItem, int> items)
    {
        foreach (KeyValuePair<EconomicItem, int> kvp in items)
            AddItem(kvp.Key, kvp.Value);
    }

    public Dictionary<EconomicItem, int> GetAllItems()
    {
        return AllItems;
    }

    /// <summary>
    /// Removes the set amount of product from this inventory, returns the remaining
    /// If the remaining is negative, we set the count to 0, but still return the -ve number
    /// </summary>
    /// <param name="item"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public int RemoveItem(EconomicItem item, int count)
    {
        if (!HasItem(item))
            return -count;
        int ic = ItemCount(item);
        if(ic >= count)
        {

            int rem = ic - count;

            AllItems[item] = rem;
            return rem;
        }
        AllItems[item] = 0;

        return ic-count;
    }

    public override string ToString()
    {
        string res = "";
        foreach(KeyValuePair<EconomicItem, int> kvp in AllItems)
        {
            res += kvp.Key + ":" + kvp.Value + "\n";
        }
        return res;
    }
}


public struct EconomicProduction
{
    public EconomicItem[] InputItems;
    public EconomicItem[] OutputItems;
    public EconomicProduction(EconomicItem[] input, EconomicItem[] output)
    {
        InputItems = input;
        OutputItems = output;
    }
}

public enum EconomicItemType
{
    food, animalProduce, ore, wood, bars, produce, misc, combat
}
public static class EconomicItemTypeHelper
{
    /// <summary>
    /// Returns the entity group associated with exports of a specific type
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static EntityGroup.GroupType GetTypeFromExport(this EconomicItemType type)
    {
        switch (type)
        {
            case EconomicItemType.animalProduce:
                return EntityGroup.GroupType.VillageAnimalExport;
            case EconomicItemType.food:
                return EntityGroup.GroupType.VillageFoodExport;
            case EconomicItemType.ore:
                return EntityGroup.GroupType.VillageOreExport;
            case EconomicItemType.wood:
                return EntityGroup.GroupType.VillageWoodExport;
        }
        return EntityGroup.GroupType.Traders;
    }
}
public class EconomicItem
{
    public string Name;
    public int ItemID;
    public float Value;
    public EconomicItemType ExportType;
    public EconomicItem(string name, int id, float value, EconomicItemType type)
    {
        Name = name;
        ItemID = id;
        Value = value;
        ExportType = type;
    }

    public override string ToString()
    {
        return Name;
    }
    /*
    public static bool operator ==(EconomicItem c1, EconomicItem c2)
    {
        if (c1 == null && c2 == null)
            return true;
        if(c1 != null && c2 != null)
        {
            return c1.ItemID == c2.ItemID;
        }
        return false;
    }

    public static bool operator !=(EconomicItem c1, EconomicItem c2)
    {
        return !(c1 == c2);
    }

    public override bool Equals(object obj)
    {
        if (obj == null || !(obj is EconomicItem)) return false;

        return ((EconomicItem)obj).ItemID == ItemID;
    }

    public override int GetHashCode()
    {
        return ItemID;
    }*/
}