using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class SettlementGenerator2
{
    private GameGenerator2 GameGen;
    private GenerationRandom GenRan;
    public SettlementGenerator2(GameGenerator2 gameGen)
    {
        GameGen = gameGen;
        GenRan = new GenerationRandom(gameGen.Seed);
    }
    private List<SettlementEconomy> SetEcon;
    public void DecideSettlementPlacement(Kingdom[] kingdoms)
    {
        SetEcon = new List<SettlementEconomy>();
        Dictionary<Kingdom, List<GridPoint>> freePoints = new Dictionary<Kingdom, List<GridPoint>>();
        foreach (Kingdom k in kingdoms)
            freePoints.Add(k, new List<GridPoint>());
        //List<GridPoint> freePoints = new List<GridPoint>();
        for(int x=0; x<GridPlacement.GridSize; x++)
        {
            for(int z=0; z<GridPlacement.GridSize; z++)
            {
                GridPoint gp = GameGen.GridPlacement.GridPoints[x, z];
                if(gp != null && gp.IsValid && !gp.HasSettlement)
                {
                    freePoints[gp.Kingdom].Add(gp);
                }
            }
        }
        foreach(Kingdom k in kingdoms)
        {
            GenerateSettlementShells(k, freePoints[k]);
        }

        WorldEventManager.Instance.SetEconomies(SetEcon);

    }

    private void GenerateSettlementShells(Kingdom k, List<GridPoint> freePoints)
    {
        List<SettlementShell> shells = new List<SettlementShell>();
        GridPoint capitalPoint = GameGen.GridPlacement.GetNearestPoint(k.CapitalChunk);
        SettlementShell cap = new SettlementShell(SettlementType.CAPITAL, capitalPoint);
        capitalPoint.SettlementShell = cap;
        shells.Add(cap);
        

        int desCityCount = GenRan.RandomInt(3, 5);
        int minCityClear = 130;
        int desTownCount = GenRan.RandomInt(7, 10);
        int minTownClear = 80;
        
        int desVilCount = GenRan.RandomInt(10, 15);
        int minVilClear = 50;

        int attempts = 20;
        for(int i=0; i<desCityCount; i++)
        {
            for(int j=0; j<attempts; j++)
            {
                //Take a random point
                GridPoint gp = GenRan.RandomFromList(freePoints);

                if (gp.EnclosedBiomes[14].Contains(ChunkBiome.mountain))
                    continue;

                //freePoints.Remove(gp);

                int minDist = -1;
                foreach (SettlementShell s in shells)
                {
                    int dist = Vec2i.QuickDistance(s.ChunkPos, gp.ChunkPos);
                    if (minDist == -1 || dist < minDist)
                    {
                        minDist = dist;
                    }
                }

                if (Mathf.Sqrt(minDist) > minCityClear)
                {
                    SettlementShell shell = new SettlementShell(SettlementType.CITY, gp);
                    shells.Add(shell);
                    gp.SettlementShell = shell;
                    freePoints.Remove(gp);
                    break;
                    Debug.Log(string.Format("City placed at grid point {0} - chunk {1} for kingdom {2}", gp.GridPos, gp.ChunkPos, k.Name));
                }
            }    
        }

        for (int i = 0; i < desTownCount; i++)
        {
            for (int j = 0; j < attempts; j++)
            {
                //Take a random point
                GridPoint gp = GenRan.RandomFromList(freePoints);
                if (gp.EnclosedBiomes[14].Contains(ChunkBiome.mountain))
                    continue;
                //freePoints.Remove(gp);

                int minDist = -1;
                foreach (SettlementShell s in shells)
                {
                    int dist = Vec2i.QuickDistance(s.ChunkPos, gp.ChunkPos);
                    if (minDist == -1 || dist < minDist)
                    {
                        minDist = dist;
                    }
                }

                if (Mathf.Sqrt(minDist) > minTownClear)
                {
                    SettlementShell shell = new SettlementShell(SettlementType.TOWN, gp);
                    shells.Add(shell);
                    gp.SettlementShell = shell;
                    freePoints.Remove(gp);

                    break;
                }
            }
        }
        for (int i = 0; i < desVilCount; i++)
        {
            for (int j = 0; j < attempts; j++)
            {
                //Take a random point
                GridPoint gp = GenRan.RandomFromList(freePoints);
                //freePoints.Remove(gp);

                int minDist = -1;
                foreach (SettlementShell s in shells)
                {
                    int dist = Vec2i.QuickDistance(s.ChunkPos, gp.ChunkPos);
                    if (minDist == -1 || dist < minDist)
                    {
                        minDist = dist;
                    }
                }

                if (Mathf.Sqrt(minDist) > minVilClear)
                {
                    SettlementShell shell = new SettlementShell(SettlementType.VILLAGE, gp);
                    shells.Add(shell);
                    gp.SettlementShell = shell;
                    freePoints.Remove(gp);
                    break;
                }
            }
        }
        for(int i=1; i<shells.Count; i++)
        {
            Vec2i v = shells[i].GridPointPlacement;
            GameGen.KingdomGen.BuildRoad(capitalPoint, GameGen.GridPlacement.GridPoints[v.x, v.z]);

        }

        foreach (SettlementShell s in shells)
        {
            CalculateSettlementEconomies(s);
            SetEcon.Add(s.Economy);
        }
        
    }


    /// <summary>
    /// Calculates the total produce and demand per tick for a settlement
    /// </summary>
    public void CalculateSettlementEconomies(SettlementShell shell)
    {

        int size = shell.Type == SettlementType.CAPITAL ? 16 : shell.Type == SettlementType.CITY ? 14 : shell.Type == SettlementType.TOWN ? 10 : 8;

        //The total amount of resources within the settlement bounds
        Dictionary<ChunkResource, float> settlementResources = new Dictionary<ChunkResource, float>();






        Vec2i baseChunk = shell.ChunkPos;
        //We iterate every chunk within this settlement
        for(int x=0; x<size; x++)
        {
            for(int z=0; z<size; z++)
            {
                ChunkBase2 cb = GameGen.TerGen.ChunkBases[baseChunk.x + x, baseChunk.z + z];
                //We iterate each possible resource the chunk could have, 
                foreach(ChunkResource cr in MiscUtils.GetValues<ChunkResource>())
                {
                    //We find the amount of this resource in the chunk, and add this to the settlement total
                    float am = cb.GetResourceAmount(cr);
                    if (!settlementResources.ContainsKey(cr))
                        settlementResources.Add(cr, 0);

                    settlementResources[cr] += am;
                }
            }
        }

        //We now know the raw resources in the area,
        if(shell.Type == SettlementType.VILLAGE)
        {
            GenerateVillageEconomy(settlementResources, shell);
        }

    }
    /// <summary>
    /// Calculates the economic item production per tick for this village based on the resources in the eclcosed chunks
    /// Adds building plans based on these production amounts
    /// Calculates the economic items used per tick 
    /// Calculates the start inventory, and the desired amounts to keep in the inventory
    /// </summary>
    /// <param name="settlementResources"></param>
    /// <param name="shell"></param>
    private void GenerateVillageEconomy(Dictionary<ChunkResource, float> settlementResources, SettlementShell shell)
    {
        List<BuildingPlan> reqBuildings = new List<BuildingPlan>();

        //A measure of how much of each resource is produced per tick
        Dictionary<EconomicItem, int> producePerTick = new Dictionary<EconomicItem, int>();

        //How much of each item is used per tick
        Dictionary<EconomicItem, int> usePerTick = new Dictionary<EconomicItem, int>();

        EconomicInventory economicInventory = new EconomicInventory();
        Dictionary<EconomicItem, int> DesiredInventoryAmounts = new Dictionary<EconomicItem, int>();
        //Villages only take raw production (of farms and wood)

        if (settlementResources.TryGetValue(ChunkResource.wheatFarm, out float v) && v > 1)
        {
            reqBuildings.Add(Building.WHEATFARM);
            foreach (EconomicItem it in ChunkResource.wheatFarm.GetEconomicItem())
            {
                if (!producePerTick.ContainsKey(it))
                    producePerTick.Add(it, 0);
                producePerTick[it] += ((int)v * 10);
            }
        }
        if (settlementResources.TryGetValue(ChunkResource.vegetableFarm, out float v0) && v0 > 1)
        {
            reqBuildings.Add(Building.VEGFARM);
            foreach (EconomicItem it in ChunkResource.vegetableFarm.GetEconomicItem())
            {
                if (!producePerTick.ContainsKey(it))
                    producePerTick.Add(it, 0);
                producePerTick[it] += ((int)v * 10);
            }
        }
        if (settlementResources.TryGetValue(ChunkResource.silkFarm, out float v1) && v1 > 1)
        {
            reqBuildings.Add(Building.SILKFARM);
            foreach (EconomicItem it in ChunkResource.silkFarm.GetEconomicItem())
            {
                if (!producePerTick.ContainsKey(it))
                    producePerTick.Add(it, 0);
                producePerTick[it] += ((int)v1 * 4);
            }
        }
        if (settlementResources.TryGetValue(ChunkResource.cattleFarm, out float v2) && v2 > 1)
        {
            reqBuildings.Add(Building.CATTLEFARM);
            foreach (EconomicItem it in ChunkResource.cattleFarm.GetEconomicItem())
            {
                if (!producePerTick.ContainsKey(it))
                    producePerTick.Add(it, 0);
                producePerTick[it] += ((int)v2);
            }

        }
        if (settlementResources.TryGetValue(ChunkResource.sheepFarm, out float v3) && v3 > 1)
        {
            reqBuildings.Add(Building.SHEEPFARM);
            foreach (EconomicItem it in ChunkResource.sheepFarm.GetEconomicItem())
            {
                if (!producePerTick.ContainsKey(it))
                    producePerTick.Add(it, 0);
                producePerTick[it] += ((int)v3);
            }
        }
        if (settlementResources.TryGetValue(ChunkResource.wood, out float v4) && v4 > 1)
        {
            reqBuildings.Add(Building.WOODCUTTER);
            foreach (EconomicItem it in ChunkResource.wood.GetEconomicItem())
            {
                if (!producePerTick.ContainsKey(it))
                    producePerTick.Add(it, 0);
                producePerTick[it] += ((int)v4);
            }
        }
        if (settlementResources.TryGetValue(ChunkResource.ironOre, out float v5) && v5 > 1)
        {
            reqBuildings.Add(Building.IRONMINE);
            foreach (EconomicItem it in ChunkResource.ironOre.GetEconomicItem())
            {
                if (!producePerTick.ContainsKey(it))
                    producePerTick.Add(it, 0);
                producePerTick[it] += ((int)v5);
            }
        }
        if (settlementResources.TryGetValue(ChunkResource.silverOre, out float v6) && v6 > 1)
        {
            reqBuildings.Add(Building.SILVERMINE);
            foreach (EconomicItem it in ChunkResource.silverOre.GetEconomicItem())
            {
                if (!producePerTick.ContainsKey(it))
                    producePerTick.Add(it, 0);
                producePerTick[it] += ((int)v6);
            }
        }
        if (settlementResources.TryGetValue(ChunkResource.goldOre, out float v7) && v7 > 1)
        {
            reqBuildings.Add(Building.GOLDMINE);
            foreach (EconomicItem it in ChunkResource.goldOre.GetEconomicItem())
            {
                if (!producePerTick.ContainsKey(it))
                    producePerTick.Add(it, 0);
                producePerTick[it] += ((int)v7);
            }
        }
        usePerTick.Add(Economy.Bread, 5);
        usePerTick.Add(Economy.Vegetables, 5);
        usePerTick.Add(Economy.Clothes, 1);

        //Calculates the amount 
        foreach(KeyValuePair<EconomicItem, int> kvp in usePerTick)
        {
            DesiredInventoryAmounts.Add(kvp.Key, kvp.Value * Economy.KEEP_IN_INVENTORY_MULT);
            economicInventory.AddItem(kvp.Key, DesiredInventoryAmounts[kvp.Key]);
        }
        shell.RequiredBuildings = reqBuildings;
        shell.StartInventory = economicInventory;
        shell.UserPerTick = usePerTick;
        shell.ProductionPerTick = producePerTick;
        shell.DesiredStock = DesiredInventoryAmounts;

        SettlementEconomy econ = new SettlementEconomy(shell);
        //Set the economy
        GameGen.GridPlacement.GridPoints[shell.GridPointPlacement.x, shell.GridPointPlacement.z].Economy = econ;

    }

    private void GenerateTownEconomy(Dictionary<ChunkResource, float> settlementResources, SettlementShell shell)
    {
        List<BuildingPlan> reqBuildings = new List<BuildingPlan>();

        //A measure of how much of each resource is produced per tick
        Dictionary<EconomicItem, int> producePerTick = new Dictionary<EconomicItem, int>();

        //How much of each item is used per tick
        Dictionary<EconomicItem, int> usePerTick = new Dictionary<EconomicItem, int>();

        EconomicInventory economicInventory = new EconomicInventory();
        Dictionary<EconomicItem, int> DesiredInventoryAmounts = new Dictionary<EconomicItem, int>();

        usePerTick.Add(Economy.Bread, 15);
        usePerTick.Add(Economy.Vegetables, 10);
        usePerTick.Add(Economy.Clothes, 3);

        //Use a small amount of weapons and armour per tick
        usePerTick.Add(Economy.LeatherArmour, 1);
        usePerTick.Add(Economy.IronWeapons, 1);

        reqBuildings.Add(Building.BAKERY);

        

        if (settlementResources.TryGetValue(ChunkResource.wood, out float v) && v > 1)
        {
            reqBuildings.Add(Building.WOODCUTTER);
            //We add the logs produced here
            foreach (EconomicItem it in ChunkResource.wood.GetEconomicItem())
            {
                if (!producePerTick.ContainsKey(it))
                    producePerTick.Add(it, 0);
                producePerTick[it] += ((int)v * 10);
            }


        }
        


        //Calculates the amount 
        foreach (KeyValuePair<EconomicItem, int> kvp in usePerTick)
        {
            DesiredInventoryAmounts.Add(kvp.Key, kvp.Value * Economy.KEEP_IN_INVENTORY_MULT);
            economicInventory.AddItem(kvp.Key, DesiredInventoryAmounts[kvp.Key]);
        }
        shell.RequiredBuildings = reqBuildings;
        shell.StartInventory = economicInventory;
        shell.UserPerTick = usePerTick;
        shell.ProductionPerTick = producePerTick;
        shell.DesiredStock = DesiredInventoryAmounts;

        SettlementEconomy econ = new SettlementEconomy(shell);
        //Set the economy
        GameGen.GridPlacement.GridPoints[shell.GridPointPlacement.x, shell.GridPointPlacement.z].Economy = econ;
    }


    public class SettlementShell
    {
        public SettlementType Type;
        public Vec2i GridPointPlacement;
        public Vec2i ChunkPos;

        public SettlementEconomy Economy;

        public List<BuildingPlan> RequiredBuildings;
        public EconomicInventory StartInventory;
        public Dictionary<EconomicItem, int> UserPerTick;
        public Dictionary<EconomicItem, int> ProductionPerTick;
        public Dictionary<EconomicItem, int> DesiredStock;
        public SettlementShell(SettlementType type, GridPoint gridPoint)
        {
            Type = type;
            GridPointPlacement = gridPoint.GridPos;
            ChunkPos = gridPoint.ChunkPos;
           
        }
    }
}