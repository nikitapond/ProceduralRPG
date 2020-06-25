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
    /// <summary>
    /// A dataset that holds all of the settlements in the game (excluding capital) position in the world,
    /// ordered by kingdom and by settlement type
    /// </summary>
    //public Dictionary<int, Dictionary<SettlementType, List<SettlementPlacement>>> SettlementPlacements;


    /// <summary>
    /// A weighted random list of grid points
    /// </summary>
    private WeightRandomList<GridPoint> DesireWeightedGridpoints;
    /// <summary>
    /// List containing all tactical location shells, and all settlement shells
    /// </summary>
    public List<Shell> SetAndTactShells;


    #region generate_settlement_shells
    /// <summary>
    /// We decide the placement 
    /// </summary>
    public void GenerateAllSettlementShells()
    {
        //Datastructure ordering the KingdomTotalSettlements by relative kingdom
        Dictionary<int, KingdomTotalSettlements> KTS = new Dictionary<int, KingdomTotalSettlements>();
        foreach(Kingdom k in GameGen.KingdomGen.Kingdoms)
        {
            //Calculate the maximum number of settlements each kingdom should have
            KTS.Add(k.KingdomID, CalculateSettlementCount(k));
        }
        //Create weighted random list for possible settlement placements
        FindGridPointsSettlementDesirability();
        //Decide the placement of all settlements
        Dictionary<int, Dictionary<SettlementType, List<SettlementShell2>>>  setPlace = DecideSettlementPlacements(KTS);
       
        SetAndTactShells = new List<Shell>();

        foreach (KeyValuePair<int, Dictionary<SettlementType, List<SettlementShell2>>> kingdomSets in setPlace){

            foreach(KeyValuePair<SettlementType, List<SettlementShell2>> setTypes in kingdomSets.Value)
            {
                foreach(SettlementShell2 ss in setTypes.Value)
                {
                    SetAndTactShells.Add(ss);
                }
            }
        }

    }
    /// <summary>
    /// Struct holds onto the total number of cities, towns, and villages to build
    /// </summary>
    private struct KingdomTotalSettlements
    {
        public int CityCount;
        public int TownCount;
        public int VillageCount;
    }

    /// <summary>
    /// Calculates the total number of cities, towns, and villages this kingdom should have
    /// Number of each is partly random, partly based on total size of kingdom
    /// </summary>
    /// <param name="k"></param>
    /// <returns></returns>
    private KingdomTotalSettlements CalculateSettlementCount(Kingdom k)
    {
        //In the region of 200,000
        int kingdomSize = k.ClaimedChunks.Count;
        int relSize = Mathf.CeilToInt(kingdomSize / 80000f);
        int cityCount = (int)(1 + GenRan.Random(0.5f, 1.5f) * relSize);
        int townCount = (int)(5 + GenRan.Random(2, 4) * relSize);
        int villageCount = (int)(4 + GenRan.Random(4, 7) * relSize);


        Debug.Log("Kingdom " + k + " has size " + relSize + " -> City: " + cityCount + ", Town: " + townCount + ", Village: " + villageCount);
        KingdomTotalSettlements kts = new KingdomTotalSettlements();
        kts.CityCount = cityCount;
        kts.TownCount = townCount;
        kts.VillageCount = villageCount;
        return kts;
    }

    /// <summary>
    /// Iterates each grid point in the world, calculating its settlement desirability
    /// We add each to a WeightRandomList <see cref="DesireWeightedGridpoints"/>.
    /// We add each grid point with a weight of its desirability
    /// </summary>
    private void FindGridPointsSettlementDesirability()
    {
        DesireWeightedGridpoints = new WeightRandomList<GridPoint>(GameGen.Seed);
        for (int x=0; x<GridPlacement.GridSize; x++)
        {
            for (int z = 0; z < GridPlacement.GridSize; z++)
            {
                //Get grid point
                GridPoint gp = GameGen.GridPlacement.GridPoints[x, z];
                float des = CalculateGridPointSettlementDesirability(gp);
                gp.Desirability = des;
                if(des > 0)
                {
                    DesireWeightedGridpoints.AddElement(gp, des);

                }
            }
        }
    }

    /// <summary>
    /// Finds the settlement placement desirability
    /// </summary>
    /// <param name="gp"></param>
    /// <returns></returns>
    private float CalculateGridPointSettlementDesirability(GridPoint gp)
    {
        bool onBorder = false;
        bool onRiver = false;
        bool onCoast = false;
        bool onLake = false;
        //Get the chunk this grid point is on
        ChunkBase2 cb = GameGen.TerGen.ChunkBases[gp.ChunkPos.x, gp.ChunkPos.z];
        //desirability less than 0 means not valid
        if (cb.Biome == ChunkBiome.ocean)
            return -1;
        int kingdomID = cb.KingdomID;
        //If this chunk is not claimed, then we don't use it
        if (kingdomID == -1)
            return -1;
        //iterate away from search point
        for (int i = 1; i < GridPlacement.GridPointSize; i++)
        {
            //Search in 4 directions
            foreach (Vec2i v in Vec2i.OCT_DIRDIR)
            {
                //Find the point in this direction
                Vec2i p = cb.Pos + v * i;
                //Ensure in world bounds
                if (GameGenerator2.InBounds(p))
                {
                    //Get chunk here
                    ChunkBase2 cb_p = GameGen.TerGen.ChunkBases[p.x, p.z];
                    if (cb_p.Biome == ChunkBiome.ocean)
                        onCoast = true;
                    if (cb_p.KingdomID != -1 && cb_p.KingdomID != kingdomID)
                        onBorder = true;
                    if (cb_p.ChunkFeature is ChunkRiverNode)
                        onRiver = true;
                }

            }
        }

        float des = 5 + (onRiver ? 5f : 0) + (onCoast ? 8f : 0) + (onLake ? 3f : 0) - (onBorder ? 4 : 0) + GenRan.Random();
        return des;
    }



    public struct SettlementPlacement
    {
        public GridPoint GridPoint;
        public SettlementType Type;

    }


    /// <summary>
    /// Decides the placement for all non capital settlements.
    /// 
    /// <list type="bullet">
    ///     <item>
    ///         We choose a valid grid point by accessing <see cref="DesireWeightedGridpoints"/>, 
    ///         and taking a weight random element from it. 
    ///         </item>
    ///          <item>We then check which kingdom owns the territory of the point.</item>
    ///     <item>We check the number of capitals, towns, and villages allowed by the kingdom, based on <paramref name="kts"/></item>
    ///     <item>If they have not reached their maximum number of a settlement type, we place one here</item>
    ///     <item>We continuously do this until all settlements for all kingdoms are found,</item>
    /// </list>
    /// </summary>
    /// <param name="kts"></param>
    private Dictionary<int, Dictionary<SettlementType, List<SettlementShell2>>> DecideSettlementPlacements(Dictionary<int, KingdomTotalSettlements> kts)
    {
        
        //Key = kingdom ID
        //Value[0] = Dictionary ordered by settlement type, containing values representing the desired settlement placement
        Dictionary<int, Dictionary<SettlementType, List<SettlementShell2>>> setPlace = new Dictionary<int, Dictionary<SettlementType, List<SettlementShell2>>>();
        //We create a data structure to hold each settlement
        foreach(Kingdom k in GameGen.KingdomGen.Kingdoms)
        {
            setPlace.Add(k.KingdomID, new Dictionary<SettlementType, List<SettlementShell2>>());
            setPlace[k.KingdomID].Add(SettlementType.CITY, new List<SettlementShell2>());
            setPlace[k.KingdomID].Add(SettlementType.TOWN, new List<SettlementShell2>());
            setPlace[k.KingdomID].Add(SettlementType.VILLAGE, new List<SettlementShell2>());
        }
        bool shouldContinue = true;
        while (shouldContinue)
        {
            Debug.Log("doing");
            //if no points remain, we break
            if (DesireWeightedGridpoints.Count == 0)
            {
                shouldContinue = false;
                break;
            }
            //We a random point
            GridPoint gp = DesireWeightedGridpoints.GetRandom(true);
            Debug.Log(gp);
            ChunkBase2 cb = GameGen.TerGen.ChunkBases[gp.ChunkPos.x, gp.ChunkPos.z];
            int kingdomID = cb.KingdomID;
            if (kingdomID == -1)
            {
                Debug.Log("no king");
                continue;
            }
                
            //We have now got to a valid settlement point so we check what type of settlement we need;

            SettlementType setType = SettlementType.CITY;
            if (setPlace[kingdomID][SettlementType.CITY].Count < kts[kingdomID].CityCount)
                setType = SettlementType.CITY;
            else if (setPlace[kingdomID][SettlementType.TOWN].Count < kts[kingdomID].TownCount)
                setType = SettlementType.TOWN;
            else if (setPlace[kingdomID][SettlementType.VILLAGE].Count < kts[kingdomID].VillageCount)
                setType = SettlementType.VILLAGE;
            else
            {
                Debug.Log("Already completed set placement for " + kingdomID);
                continue;
            }
                
            //Find the shorest distance 
            int distSqr = FindShortestSquareDistance(setPlace[kingdomID], gp);
            //the maximum distance
            int minDistSqr = GridPlacement.GridPointSize * GridPlacement.GridPointSize * 4;
            if (distSqr >= minDistSqr || distSqr < 0)
            {
                gp.HasSet = true;
                gp.SETYPE = setType;
                SettlementShell2 sp = new SettlementShell2(gp, kingdomID, setType);
        
                setPlace[kingdomID][setType].Add(sp);
            }
        }
        return setPlace;
    }
    /// <summary>
    /// Finds the shortest distance (in chunk coords) between the chunk pos at the gridpoint gp, and each of the settlements
    /// </summary>
    /// <param name="toSearch"></param>
    /// <param name="gp"></param>
    /// <returns></returns>
    private int FindShortestSquareDistance(Dictionary<SettlementType, List<SettlementShell2>> toSearch, GridPoint gp)
    {
        int minDist = -1;

        foreach(SettlementType setType in MiscUtils.GetValues<SettlementType>())
        {
            if(toSearch.TryGetValue(setType, out List<SettlementShell2> settlements))
            {
                foreach (SettlementShell2 ss in settlements)
                {
                    int sqrDist = ss.GridPoint.ChunkPos.QuickDistance(gp.ChunkPos);
                    if (minDist < 0 || sqrDist < minDist)
                        minDist = sqrDist;
                }
            }
            
            
        }

        

        return minDist;
    }

    #endregion
    #region generate_tactical_shells
    public void GenerateAllTacticalLocationShells()
    {
        //Calculate grid point desirability for tactical positions
        FindGridPointsTacticalDesirability();
        //Calculate the total number of towers and forts each kingdom should have
        Dictionary<int, KingdomTotalTactialLocations> KTL = new Dictionary<int, KingdomTotalTactialLocations>();
        foreach (Kingdom k in GameGen.KingdomGen.Kingdoms)
        {
            KTL.Add(k.KingdomID, CalculateTacticalLocationCount(k));
        }
        //Decide the placement of all tactical locations
        Dictionary<int, Dictionary<TacLocType, List<TacticalLocationShell>>>  tactLoc = DecideAllTacticalLocationPlacements(KTL);


        foreach(KeyValuePair<int, Dictionary<TacLocType, List<TacticalLocationShell>>> kingdomTacts in tactLoc)
        {
            foreach(KeyValuePair<TacLocType, List<TacticalLocationShell>> tactType in kingdomTacts.Value)
            {
                foreach(TacticalLocationShell lts in tactType.Value)
                {
                    SetAndTactShells.Add(lts);
                }
            }
        }
    }



    /// <summary>
    /// Iterates each grid point in the world, calculating its tactical desirability
    /// We add each to a WeightRandomList <see cref="DesireWeightedGridpoints"/>.
    /// We add each grid point with a weight of its desirability
    /// </summary>
    private void FindGridPointsTacticalDesirability()
    {
        DesireWeightedGridpoints = new WeightRandomList<GridPoint>(GameGen.Seed);

        for (int x = 0; x < GridPlacement.GridSize; x++)
        {
            for (int z = 0; z < GridPlacement.GridSize; z++)
            {
                //Get grid point
                GridPoint gp = GameGen.GridPlacement.GridPoints[x, z];
                float des = CalculateGridPointTacticalDesirability(gp);
                gp.Desirability = des;
                if (des > 0)
                {
                    DesireWeightedGridpoints.AddElement(gp, des);

                }
            }
        }
    }

    /// <summary>
    /// Finds the tactical placement desirability
    /// </summary>
    /// <param name="gp"></param>
    /// <returns></returns>
    private float CalculateGridPointTacticalDesirability(GridPoint gp)
    {

        bool onBorder = false;
        bool onRiver = false;
        bool onCoast = false;
        bool onLake = false;
        bool onHill = false;


        //Get the chunk this grid point is on
        ChunkBase2 cb = GameGen.TerGen.ChunkBases[gp.ChunkPos.x, gp.ChunkPos.z];
        int kingdomID = cb.KingdomID;
        //If ocean, contains settlement, or belongs to no kingdom, then we do not use it
        if (cb.Biome == ChunkBiome.ocean || gp.HasSet || kingdomID == -1)
            return -1;


        //we create an array, which holds the sum of all points, for each line in the 8 octangonal directions
        float[] sumSurroundingHeight = new float[8];
        //iterate away from search point
        for (int i = 1; i < GridPlacement.GridPointSize; i++)
        {
            int j = 0;
            //Search in 4 directions
            foreach (Vec2i v in Vec2i.OCT_DIRDIR)
            {
                //Find the point in this direction
                Vec2i p = cb.Pos + v * i;
                //Ensure in world bounds
                if (GameGenerator2.InBounds(p))
                {
                    //Get chunk here
                    ChunkBase2 cb_p = GameGen.TerGen.ChunkBases[p.x, p.z];
                    if (cb_p.Biome == ChunkBiome.ocean)
                        onCoast = true;
                    if (cb_p.KingdomID != -1 && cb_p.KingdomID != kingdomID)
                        onBorder = true;
                    if (cb_p.ChunkFeature is ChunkRiverNode)
                        onRiver = true;
                    //Sum total height
                    sumSurroundingHeight[j] += cb_p.Height;
                }
                j++;
            }
        }
        // Find chunk height, and create sum for surrounding heights

        float pointHeight = cb.Height;
        float averageSurrounding = 0;
        //iterte each surrounding height sum, and divide by 31 to find average height in each direction
        for (int j = 0; j < 8; j++)
        {
            sumSurroundingHeight[j] /= 31f;

            //If the height is lower, we increase 'averageSurrounding' by the height difference
            if (sumSurroundingHeight[j] + 1 < pointHeight)
                averageSurrounding += pointHeight - sumSurroundingHeight[j];
            else if (sumSurroundingHeight[j] - 1 > pointHeight)
            {
                averageSurrounding += pointHeight - sumSurroundingHeight[j];
            }
        }
        float des = 5 + (onRiver ? 4f : 0) + (onCoast ? 3f : 0) - (onLake ? 2f : 0) + (onBorder ? 10 : 0) + (onHill ? 6 : 0);
        return des;
    }
    /// <summary>
    /// Struct holding the total maximum number of towers and fort a kingdom should have
    /// </summary>
    private struct KingdomTotalTactialLocations
    {
        public int TowerCount;
        public int FortCount;
    }
    /// <summary>
    /// Calculates the total number of towers and forts a kingdom should have, based
    /// on its size and aggression
    /// </summary>
    /// <param name="k"></param>
    /// <returns></returns>
    private KingdomTotalTactialLocations CalculateTacticalLocationCount(Kingdom k)
    {
        //In the region of 200,000
        int kingdomSize = k.ClaimedChunks.Count;
        int relSize = Mathf.CeilToInt(kingdomSize / 80000f);

        int towerCount = (int)(Mathf.Exp(k.Aggresion * 1f) * (relSize + 2));
        int fortCount = (int)(Mathf.Exp((1 - k.Aggresion) * 1f) * (relSize + 2));
        KingdomTotalTactialLocations kttl = new KingdomTotalTactialLocations();
        kttl.TowerCount = towerCount;
        kttl.FortCount = fortCount;
        return kttl;
    }


    private struct TacticalLocationPlacement
    {
        public GridPoint GridPoint;
        public TacLocType Type;
    }
    /// <summary>
    /// Decides the placement of all tactical locations
    /// </summary>
    /// <param name="ktl"></param>
    private Dictionary<int, Dictionary<TacLocType, List<TacticalLocationShell>>> DecideAllTacticalLocationPlacements(Dictionary<int, KingdomTotalTactialLocations> ktl)
    {
        Dictionary<int, Dictionary<TacLocType, List<TacticalLocationShell>>> tactPlace = new Dictionary<int, Dictionary<TacLocType, List<TacticalLocationShell>>>();
        //Create data structure to hold tactical locations
        foreach (Kingdom k in GameGen.KingdomGen.Kingdoms)
        {
            tactPlace.Add(k.KingdomID, new Dictionary<TacLocType, List<TacticalLocationShell>>());
            tactPlace[k.KingdomID].Add(TacLocType.fort, new List<TacticalLocationShell>());
            tactPlace[k.KingdomID].Add(TacLocType.tower, new List<TacticalLocationShell>());
        }

        bool shouldContinue = true;
        while (shouldContinue)
        {
            Debug.Log("doing");
            //if no points remain, we break
            if (DesireWeightedGridpoints.Count == 0)
            {
                shouldContinue = false;
                break;
            }
            //We a random point
            GridPoint gp = DesireWeightedGridpoints.GetRandom(true);
            Debug.Log(gp);
            ChunkBase2 cb = GameGen.TerGen.ChunkBases[gp.ChunkPos.x, gp.ChunkPos.z];
            int kingdomID = cb.KingdomID;
            if (kingdomID == -1)
            {
                Debug.Log("no king");
                continue;
            }

            //We have now got to a valid settlement point so we check what type of settlement we need;

            TacLocType tactType = TacLocType.fort;
            if (tactPlace[kingdomID][TacLocType.fort].Count < ktl[kingdomID].FortCount)
                tactType = TacLocType.fort;
            else if (tactPlace[kingdomID][TacLocType.tower].Count < ktl[kingdomID].TowerCount)
                tactType = TacLocType.tower;
            else
            {
                Debug.Log("Already completed set placement for " + kingdomID);
                continue;
            }

            //Find the shorest distance 
            int distSqr = FindShortestSquareDistance(tactPlace[kingdomID], gp);
            //the maximum distance
            int minDistSqr = GridPlacement.GridPointSize * GridPlacement.GridPointSize * 4;
            if (distSqr >= minDistSqr || distSqr < 0)
            {
                gp.HasTacLoc = true;
                gp.TACTYPE = tactType;
                TacticalLocationShell tls = new TacticalLocationShell(gp, kingdomID);
               
                tactPlace[kingdomID][tactType].Add(tls);
            }
        }

        return tactPlace;


    }

    /// <summary>
    /// Finds the shortest distance (in chunk coords) between the chunk pos at the gridpoint gp, and each of the settlements
    /// </summary>
    /// <param name="toSearch"></param>
    /// <param name="gp"></param>
    /// <returns></returns>
    private int FindShortestSquareDistance(Dictionary<TacLocType, List<TacticalLocationShell>> toSearch, GridPoint gp)
    {
        int minDist = -1;

        foreach (TacLocType tp in MiscUtils.GetValues<TacLocType>())
        {
            if(toSearch.TryGetValue(tp, out List<TacticalLocationShell> tacLocs))
            {
                foreach (TacticalLocationShell tac in tacLocs)
                {
                    int sqrDist = tac.GridPoint.ChunkPos.QuickDistance(gp.ChunkPos);
                    if (minDist < 0 || sqrDist < minDist)
                        minDist = sqrDist;
                }
            }

        }


        return minDist;
    }
    #endregion
    #region location_surroundings

    /// <summary>
    /// struct containing data for a settlement/tactical location
    /// </summary>
    public struct LocationData
    {
        public bool OnLake;
        public bool OnRiver;
        public bool OnCoast;
        public bool OnBorder;
    }

    /// <summary>
    /// Iterates through each location shell in <see cref="SetAndTactShells"/>, and 
    /// gathers/generated info about surrounding area, such as near locations, rivers, etc
    /// </summary>
    public void CalculateTactialAndSettlementData()
    {
        foreach(Shell s in SetAndTactShells)
        {
            GenerateLocationData(s);
        }
      
    }
    /// <summary>
    ///  Calculates the location data for the supplied shell
    /// </summary>
    /// <param name="shell">The shell to generate and store data to</param>

    private void GenerateLocationData(Shell shell)
    {
        GridPoint gp = shell.GridPoint;

        bool onBorder = false;
        bool onRiver = false;
        bool onCoast = false;
        bool onLake = false;
        //Get the chunk this grid point is on
        ChunkBase2 cb = GameGen.TerGen.ChunkBases[gp.ChunkPos.x, gp.ChunkPos.z];

        int kingdomID = cb.KingdomID;
        if(kingdomID == -1)
        {
            Debug.Error("Shell " + shell + " lays on unclaimed territory - not valid");
        }
        //iterate away from search point
        for (int i = 1; i < GridPlacement.GridPointSize; i++)
        {
            //Search in 4 directions
            foreach (Vec2i v in Vec2i.OCT_DIRDIR)
            {
                //Find the point in this direction
                Vec2i p = cb.Pos + v * i;
                //Ensure in world bounds
                if (GameGenerator2.InBounds(p))
                {
                    //Get chunk here
                    ChunkBase2 cb_p = GameGen.TerGen.ChunkBases[p.x, p.z];
                    if (cb_p.Biome == ChunkBiome.ocean)
                        onCoast = true;
                    if (cb_p.KingdomID != -1 && cb_p.KingdomID != kingdomID)
                        onBorder = true;
                    if (cb_p.ChunkFeature is ChunkRiverNode)
                        onRiver = true;
                }

            }
        }
        LocationData ld = new LocationData() { OnCoast = true, OnLake = true, OnRiver = false, OnBorder = false };
        Debug.Log(ld.OnCoast + "_" + ld.OnLake + "_" + ld.OnRiver + "_" + ld.OnBorder);
        shell.SetLocationData(ld);

    }



    #endregion
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