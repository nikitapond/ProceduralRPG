using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Threading;
public class SettlementGenerator2
{
    private GameGenerator2 GameGen;
    private GenerationRandom GenRan;
    public SettlementGenerator2(GameGenerator2 gameGen)
    {
        GameGen = gameGen;
        GenRan = new GenerationRandom(gameGen.Seed);
    }
    private Dictionary<SettlementShell, SettlementEconomy> SettlementEconomies;

    public List<Settlement> Settlements;
    public Object LOCK = new Object();
    public void GenerateAllSettlements(Kingdom[] kingdoms)
    {
        //We decide the placement (an economies) of each settlement.
        //This also dictates information about the sort of buildings to be generated in the settlement
        Debug.BeginDeepProfile("SettlementPlacement");
        Dictionary<Kingdom, List<SettlementShell>> kingdomSets = DecideSettlementPlacement(kingdoms);
        List<Thread> threads = new List<Thread>(4);
        Debug.EndDeepProfile("SettlementPlacement");
        Settlements = new List<Settlement>(60);
        Debug.BeginDeepProfile("SettlementGeneration"); 
        foreach (KeyValuePair<Kingdom, List<SettlementShell>> kvp in kingdomSets)
        {
            threads.Add(ThreadGenerateSettlements(kvp.Key, kvp.Value));
            //GenerateSettlements(kvp.Key, kvp.Value);
        }
        Debug.EndDeepProfile("SettlementGeneration");
        foreach (Thread t in threads)
        {
            t.Join();
        }
        Debug.BeginDeepProfile("CalcNearSets");
        CalculateNearSettlements();
        Debug.EndDeepProfile("CalcNearSets");
    }

    private void GenerateSettlements(Kingdom k, List<SettlementShell> toGen)
    {
        List<Settlement> sets = new List<Settlement>(toGen.Count);

        foreach (SettlementShell s in toGen)
        {
            Vec2i midPos = s.ChunkPos + new Vec2i(s.Type.GetSize(), s.Type.GetSize()) / 2;
            SettlementBase setBase = new SettlementBase(midPos, s.Type.GetSize(), s.Type);
            SettlementBuilder builder = new SettlementBuilder(GameGen.TerGen.HeightFunction, setBase);
            Settlement set = new Settlement(k, "Set", builder);
            set.GridPointPosition = s.GridPointPlacement;



            if (SettlementEconomies.TryGetValue(s, out SettlementEconomy econ))
            {
                set.SetEconomy(econ);
            }
            else
            {
                Debug.Log("No economy found for settlement " + s);
            }
            sets.Add(set);
            // GameGen.World.AddSettlement(set);
        }
        lock (LOCK)
        {
            Settlements.AddRange(sets);
        }
        GameGen.World.AddSettlementRange(sets);
    }

    private Thread ThreadGenerateSettlements(Kingdom k, List<SettlementShell> toGen)
    {
        Thread thread = new Thread(() => GenerateSettlements(k, toGen));
        thread.Start();
        return thread;
    }

    /// <summary>
    /// Calculates the nearest settlements of each settlement by searching grid points near to it
    /// </summary>
    private void CalculateNearSettlements()
    {
        int checkR = 8;
        List<int> nearEconIDs = new List<int>(20);
        foreach(Settlement  thisSet in Settlements)
        {
            nearEconIDs.Clear();
            Vec2i gridPos = thisSet.GridPointPosition;

            for(int x=-checkR; x<=checkR; x++)
            {
                for(int z=-checkR; z<=checkR; z++)
                {
                    //If we are at THIS grid point, then it is this settlement, so we don't check
                    if (x == 0 && z == 0)
                        continue;
                    Vec2i v = gridPos + new Vec2i(x, z);
                   
                    if(GridPlacement.InGridBounds(v) && GameGen.GridPlacement.GridPoints[v.x, v.z] != null && GameGen.GridPlacement.GridPoints[v.x, v.z].Economy != null)
                    {
                        SettlementEconomy econ = GameGen.GridPlacement.GridPoints[v.x, v.z].Economy;
                       // Debug.Log("Settlement " + thisSet + " has near settlement with ID " + econ.SettlementID);
                        nearEconIDs.Add(econ.SettlementID);
                    }
                }
            }
            thisSet.Economy.NearSettlementsIDs = nearEconIDs.ToArray();


        }
    }


    /// <summary>
    /// Decides the placement for each settlement of each kingdom.
    /// We then generate the settlement shell via <see cref="GenerateSettlementShells(Kingdom, List{GridPoint})"/>
    /// </summary>
    /// <param name="kingdoms">The kingdoms that this world has</param>
    /// <returns></returns>
    private Dictionary<Kingdom, List<SettlementShell>> DecideSettlementPlacement(Kingdom[] kingdoms)
    {
        
        Dictionary<Kingdom, List<GridPoint>> freePoints = new Dictionary<Kingdom, List<GridPoint>>();

        Dictionary<Kingdom, List<SettlementShell>> kingdomSets = new Dictionary<Kingdom, List<SettlementShell>>();


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

        SettlementEconomies = new Dictionary<SettlementShell, SettlementEconomy>(150);


        foreach (Kingdom k in kingdoms)
        {
            kingdomSets.Add(k, GenerateSettlementShells(k, freePoints[k]));
        }

        //WorldEventManager.Instance.SetEconomies(SetEcon);
        return kingdomSets;
    }

    private List<SettlementShell>  GenerateSettlementShells(Kingdom k, List<GridPoint> freePoints)
    {
        List<SettlementShell> shells = new List<SettlementShell>(3 + 6 + 10);
        GridPoint capitalPoint = GameGen.GridPlacement.GetNearestPoint(k.CapitalChunk);
        Vec2i capP = capitalPoint.GridPos;
        SettlementShell cap = new SettlementShell(SettlementType.CAPITAL, capitalPoint);
        capitalPoint.SettlementShell = cap;
        shells.Add(cap);
        GameGen.GridPlacement.GridPoints[capP.x, capP.z].ChunkRoad = new ChunkRoad(ChunkRoad.RoadType.Paved);

        int desCityCount = GenRan.RandomInt(2, 3);
        int minCityClear = 130;
        int desTownCount = GenRan.RandomInt(5, 6);
        int minTownClear = 80;
        
        int desVilCount = GenRan.RandomInt(8, 10);
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
            GameGen.KingdomGen.BuildRoad(GameGen.GridPlacement.GridPoints[capP.x, capP.z], GameGen.GridPlacement.GridPoints[v.x, v.z]);

        }

        foreach (SettlementShell s in shells)
        {

            for(int x=0; x<s.Type.GetSize(); x++)
            {
                for(int z=0; z<s.Type.GetSize(); z++)
                {
                    GameGen.TerGen.ChunkBases[x, z].HasSettlement = true;
                }
            }

            CalculateSettlementEconomies(s);
            SettlementEconomies.Add(s, s.Economy);
            //SetEcon.Add(s.Economy);
        }

        return shells;
    }


    /// <summary>
    /// Calculates the total produce and demand per tick for a settlement
    /// </summary>
    private void CalculateSettlementEconomies(SettlementShell shell)
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
        }else if(shell.Type == SettlementType.TOWN)
        {
            GenerateTownEconomy(settlementResources, shell);
        }
        else
        {
            GenerateTownEconomy(settlementResources, shell);

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
        shell.UsePerTick = usePerTick;
        shell.RawProductionPerTick = producePerTick;
        shell.DesiredStock = DesiredInventoryAmounts;
        shell.EconomicProduction = new Dictionary<EconomicProduction, int>();
        SettlementEconomy econ = new SettlementEconomy(shell);
        //Set the economy
        GameGen.GridPlacement.GridPoints[shell.GridPointPlacement.x, shell.GridPointPlacement.z].Economy = econ;

    }

    private void GenerateTownEconomy(Dictionary<ChunkResource, float> settlementResources, SettlementShell shell)
    {
        List<BuildingPlan> reqBuildings = new List<BuildingPlan>();

        //A measure of how much of each resource is produced per tick
        Dictionary<EconomicItem, int> rawProductionPerTick = new Dictionary<EconomicItem, int>();
        //A measure of how much can be produced by industry
        Dictionary<EconomicProduction, int> productionPerTick = new Dictionary<EconomicProduction, int>();
        //How much of each item is used per tick (excluding economy)
        Dictionary<EconomicItem, int> usePerTick = new Dictionary<EconomicItem, int>();

        EconomicInventory economicInventory = new EconomicInventory();
        Dictionary<EconomicItem, int> desiredStock = new Dictionary<EconomicItem, int>();

        usePerTick.Add(Economy.Bread, 15);
        usePerTick.Add(Economy.Vegetables, 10);
        usePerTick.Add(Economy.Clothes, 3);
        //Use a small amount of weapons and armour per tick
        usePerTick.Add(Economy.LeatherArmour, 1);
        usePerTick.Add(Economy.IronWeapons, 1);
        SettlementProductionAbility productionAbility = new SettlementProductionAbility();




        //All towns will have a bakery, this will produce bread from wheat
        reqBuildings.Add(Building.BAKERY);
        productionPerTick.Add(Economy.WheatToBread, 50);
        economicInventory.AddItem(Economy.Wheat, 500);
        productionAbility.HasButcher = true;
        productionPerTick.Add(Economy.CowToBeef, 5);
        productionPerTick.Add(Economy.SheepToMutton, 5);

        //If we are in a forrest, then the town till cut wood & make into planks
        if (settlementResources.TryGetValue(ChunkResource.wood, out float v) && v > 1)
        {
            reqBuildings.Add(Building.WOODCUTTER);
            reqBuildings.Add(Building.LUMBERMILL);
            int rawProduce = (int)v*2;
            rawProductionPerTick.Add(ChunkResource.wood.GetEconomicItem()[0], rawProduce);
            //Takes 1 log and make 3 planks
            //We set the production such that it wished to import as much wood as possible
            productionPerTick.Add(Economy.WoodLogToPlank, (int)(rawProduce * GenRan.Random(2, 4)));
            productionAbility.HasLumberMill = true;
        }
        bool hasSmelt = false;

        if(settlementResources.TryGetValue(ChunkResource.ironOre, out float v1) && v1 > 1)
        {
            hasSmelt = true;
            reqBuildings.Add(Building.SMELTER);
            reqBuildings.Add(Building.BLACKSMITH);

            //We add some planks to start so we can produce weapons from start
            economicInventory.AddItem(Economy.WoodPlank, 5000);


            rawProductionPerTick.Add(Economy.IronOre, (int)v1);
            productionAbility.HasSmelter = true;

            productionPerTick.Add(Economy.Iron_OreToBar, (int)(v1 * GenRan.Random(2, 4)));
            productionPerTick.Add(Economy.IronToWeapon, 3);
            productionPerTick.Add(Economy.LeatherToArmour, 3);
        }
        if (settlementResources.TryGetValue(ChunkResource.silverOre, out float v2) && v2 > 1)
        {
            if(!hasSmelt)
                reqBuildings.Add(Building.SMELTER);
            rawProductionPerTick.Add(Economy.SilverOre, (int)v2);
            productionAbility.HasSmelter = true;

            productionPerTick.Add(Economy.Silver_OreToBar, (int)(v2 * GenRan.Random(2, 4)));
        }
        if (settlementResources.TryGetValue(ChunkResource.goldOre, out float v3) && v3 > 1)
        {
            if (!hasSmelt)
                reqBuildings.Add(Building.SMELTER);
            rawProductionPerTick.Add(Economy.GoldOre, (int)v3);
            productionAbility.HasSmelter = true;

            productionPerTick.Add(Economy.Gold_OreToBar, (int)(v3 * GenRan.Random(2, 4)));
        }
        //Calculates the amount 
        foreach (KeyValuePair<EconomicItem, int> kvp in usePerTick)
        {
            int amount = kvp.Value * Economy.KEEP_IN_INVENTORY_MULT;
            desiredStock.Add(kvp.Key, amount);
            economicInventory.AddItem(kvp.Key, amount);
        }
        shell.RequiredBuildings = reqBuildings;
        shell.StartInventory = economicInventory;
        shell.UsePerTick = usePerTick;
        shell.RawProductionPerTick = rawProductionPerTick;
        shell.DesiredStock = desiredStock;
        shell.EconomicProduction = productionPerTick;
        shell.ProductionAbility = productionAbility;
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
        public Dictionary<EconomicItem, int> UsePerTick;
        public Dictionary<EconomicItem, int> RawProductionPerTick;
        public Dictionary<EconomicItem, int> DesiredStock;
        public Dictionary<EconomicProduction, int> EconomicProduction;
        public SettlementProductionAbility ProductionAbility;
        public SettlementShell(SettlementType type, GridPoint gridPoint)
        {
            Type = type;
            GridPointPlacement = gridPoint.GridPos;
            ChunkPos = gridPoint.ChunkPos;
           
        }
    }
}