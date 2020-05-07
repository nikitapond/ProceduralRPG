using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Threading;
/// <summary>
/// Used to generate all the kingdoms
/// </summary>
public class KingdomsGenerator
{
    private readonly World World;

    private GameGenerator GameGenerator;
    private TerrainGenerator TerrainGenerator;
    private Dictionary<Kingdom, List<Vec2i>> KingdomChunks;
    private Dictionary<Kingdom, List<SettlementBase>> KingdomSettlements;
    private GenerationRandom GenerationRandom;

    public List<Vec2i> BorderChunks;

    //Initiats the Kingdom generator, the World given is the world the kingdoms will be added to.
    public KingdomsGenerator(World world, GameGenerator gameGen)
    {
        GameGenerator = gameGen;
        World = world;
        TerrainGenerator = GameGenerator.TerrainGenerator;
        KingdomChunks = new Dictionary<Kingdom, List<Vec2i>>(World.ChunkSize*World.ChunkSize/3);
        KingdomSettlements = new Dictionary<Kingdom, List<SettlementBase>>();
        GenerationRandom = new GenerationRandom(gameGen.Seed);
    }

    private List<SettlementGenerator> SettlementGenerators;
    private List<Dictionary<Settlement, Dictionary<Vec2i, ChunkData2>>> ThreadComplete;
    /// <summary>
    /// Generates all settlements.
    /// Stores the chunks from said settlements with reference to their world positions
    /// </summary>
    /// <returns></returns>
    public Dictionary<Vec2i, ChunkData2> GenerateSettlements()
    {
        
        SettlementGenerators = new List<SettlementGenerator>();

        ThreadComplete = new List<Dictionary<Settlement, Dictionary<Vec2i, ChunkData2>>>();
        List<Thread> threads = new List<Thread>();


        Dictionary<Vec2i, ChunkData2> setChunks = new Dictionary<Vec2i, ChunkData2>(2000);
        
        foreach (KeyValuePair<Kingdom, List<SettlementBase>> kpv in KingdomSettlements)
        {
            
            SettlementGenerator sg = new SettlementGenerator(GameGenerator, World, kpv.Key, kpv.Value);
            
            threads.Add(InitSettlementGeneratorThread(kpv.Key, sg));
        }
        
        bool threadsComplete = false;
        while (!threadsComplete)
        {
            threadsComplete = true;
            foreach (Thread t in threads)
            {
                if (t.IsAlive)
                    threadsComplete = false;
            }
        }

        foreach(Dictionary<Settlement, Dictionary<Vec2i, ChunkData2>> dict in ThreadComplete)
        {
            foreach (KeyValuePair<Settlement, Dictionary<Vec2i, ChunkData2>> kpv2 in dict)
            {
                //Add the settlement to its relevent kingdom.
                foreach (KeyValuePair<Vec2i, ChunkData2> kpv3 in kpv2.Value)
                {
                    setChunks.Add(kpv3.Key, kpv3.Value);
                }


            }
        }
        return setChunks;

    }
    private Thread InitSettlementGeneratorThread(Kingdom k, SettlementGenerator s)
    {
        Thread thread = new Thread(() => InnerSettlementGenThread(k,s));
        thread.Start();
        return thread;

    }
    private void InnerSettlementGenThread(Kingdom k, SettlementGenerator g)
    {
        Debug.Log("gets here fine");
       
        Dictionary<Settlement, Dictionary<Vec2i, ChunkData2>> dc = g.GenerateAllSettlements();
        lock (ThreadComplete)
        {
            ThreadComplete.Add(dc);
        }
        foreach(KeyValuePair<Settlement, Dictionary<Vec2i, ChunkData2>> kpv in dc)
        {
            k.AddSettlement(kpv.Key);
            //kpv.Key.SetKingdomID(k.KingdomID);
        }
    }


    public void GenerateEmptyKingdomsFromBaseChunks(int count)
    {
        //Generate an array of positions that will be the position of each capital
        Vec2i[] KingdomCapitals = ChooseKingdomCapitals(TerrainGenerator.LandChunks, count); 
        //Take this array of positions, and generate empty kingdoms from this,
        List<Kingdom> emptyKingdoms = GenerateEmptyKingdoms(KingdomCapitals);
        foreach (Kingdom k in emptyKingdoms)
        {
            KingdomChunks.Add(k, new List<Vec2i>());
            k.SetKingdomID(World.AddKingdom(k));
        }
        ClaimKingdomTerritorys(emptyKingdoms);

        Dictionary<Kingdom, Color> kingColors = new Dictionary<Kingdom, Color>();
        Texture2D kingMap = new Texture2D(World.WorldSize, World.WorldSize);

        foreach(KeyValuePair<Kingdom, List<Vec2i>> kpv in KingdomChunks)
        {
            Color c = new Color(Random.value, Random.value, Random.value);
            foreach(Vec2i v in kpv.Value)
            {
                kingMap.SetPixel(v.x, v.z, c);

                TerrainGenerator.ChunkBases[v.x, v.z].Kingdom = kpv.Key;
            }
        }
        kingMap.Apply();
        GameManager.Game.toDrawTexts[1] = kingMap;
        DefineBorderChunks();
        GenerateAllSettlementBases(emptyKingdoms);
    }

    public void GenerateAllSettlementBases(List<Kingdom> kingdoms)
    {
        foreach(Kingdom k in kingdoms)
        {
            GenerateKingdomSettlementBases(k);
        }
    }
    public void GenerateKingdomSettlementBases(Kingdom kingdom)
    {
        KingdomSettlements[kingdom] = new List<SettlementBase>(100);
        AddSettlementBaseToKingdom(kingdom, new SettlementBase(kingdom.CapitalChunk, 8, SettlementType.CAPITAL));

        int kingdomSize = KingdomChunks[kingdom].Count;
        //int cityCount = 1;
        //int townCount = 1;
        //int villageCount = 1;
        int cityCount = MiscMaths.RandomRange(2, 3);
        int townCount = MiscMaths.RandomRange(7, 9);
        int villageCount = MiscMaths.RandomRange(18, 25);

        if(kingdomSize > 400)
        {
         //   cityCount++;
         //   townCount += MiscMaths.RandomRange(1, 3);
          //  villageCount += MiscMaths.RandomRange(1, 8);
        }
        int distMult = 4;
        for(int i=0; i<cityCount; i++)
        {
            Vec2i c = FindSpace(kingdom, 6* distMult);
            if (c == null)
                continue;
            AddSettlementBaseToKingdom(kingdom, new SettlementBase(c, 6, SettlementType.CITY));
        }
        for (int i = 0; i < townCount; i++)
        {
            Vec2i c = FindSpace(kingdom, 4* distMult);
            if (c == null)
                continue;
            AddSettlementBaseToKingdom(kingdom, new SettlementBase(c, 4, SettlementType.TOWN));
        }
        for (int i = 0; i < villageCount; i++)
        {
            Vec2i c = FindSpace(kingdom, 3* distMult);
            if (c == null)
                continue;
            AddSettlementBaseToKingdom(kingdom, new SettlementBase(c, 3, SettlementType.VILLAGE));
        }
        /*
        Vec2i city1 = FindSpace(kingdom, 12);
        AddSettlementBaseToKingdom(kingdom, new SettlementBase(city1, 6, SettlementType.CITY));
        Vec2i city2 = FindSpace(kingdom, 12);
        AddSettlementBaseToKingdom(kingdom, new SettlementBase(city2, 6, SettlementType.CITY));*/
    }

    public Vec2i FindSpace(Kingdom kingdom, int size)
    {
        int attempts = 0;
        while (attempts < 20)
        {
            Vec2i v = null;
            if (kingdom == null)
            {
                v = GenerationRandom.RandomFromList(GameGenerator.TerrainGenerator.LandChunks);
            }
            else
            {
                v = GenerationRandom.RandomFromList(KingdomChunks[kingdom]);
            }
            bool canDo = true;
            //Check point allows settlement within bounds
            if (v.x - size < 0 || v.x + size > World.WorldSize - 1 || v.z - size < 0 || v.z + size > World.WorldSize - 1)
            {
                canDo = false;
            }
            else
            {
                for (int x = -size; x <= size; x++)
                {
                    for (int z = -size; z <= size; z++)
                    {
                        int cx = x + v.x;
                        int cz = z + v.z;

                        if (cx < 0 || cz < 0 || cx >= World.WorldSize || cz >= World.WorldSize)
                            canDo = false;

                        ChunkBase cb = TerrainGenerator.ChunkBases[cx, cz];
                        if (cb.Kingdom != kingdom || cb.HasSettlement || !cb.IsLand)
                            canDo = false;
                        else if (cb.RiverNode != null || cb.Lake != null)
                            canDo = false;
                        /*
                        else if (TerrainGenerator.ChunkBases[Mathf.Clamp(x + v.x, 0, World.WorldSize - 1), Mathf.Clamp(z + v.z, 0, World.WorldSize - 1)].Kingdom != kingdom)
                            canDo = false;
                        else if (TerrainGenerator.ChunkBases[Mathf.Clamp(x + v.x, 0, World.WorldSize - 1), Mathf.Clamp(z + v.z, 0, World.WorldSize - 1)].Settlement != null)
                            canDo = false;
                        else if (!TerrainGenerator.ChunkBases[Mathf.Clamp(x + v.x, 0, World.WorldSize - 1), Mathf.Clamp(z + v.z, 0, World.WorldSize - 1)].IsLand)
                            canDo = false;
                            */
                        if (!canDo)
                            return null;
                    }
                }
            }
            if (canDo)
                return v;
            attempts++;
        }

        return null;
    }

    private void AddSettlementBaseToKingdom(Kingdom k, SettlementBase b)
    {
        foreach(Vec2i v in b.SettlementChunks)
        {
            TerrainGenerator.ChunkBases[v.x, v.z].SetHasSettlement(true);
        }
        KingdomSettlements[k].Add(b);
    }
    public void ClaimKingdomTerritorys(List<Kingdom> kingdoms)
    {
        for (int i = 0; i < TerrainGenerator.LandChunks.Count; i++)
        {
            Vec2i v = TerrainGenerator.LandChunks[i];
            //Find which Capital city the chunk is closest to
            //Give the chunk to that kingdom
            Kingdom k = ClosestKingdomToChunk(kingdoms, v);
            KingdomChunks[k].Add(v);
            //k.AddTerritory(v);

        }
    }

    public void DefineBorderChunks()
    {
        BorderChunks = new List<Vec2i>();
        for (int x = 1; x < World.WorldSize - 1; x++)
        {
            for (int z = 1; z < World.WorldSize - 1; z++)
            {
                Kingdom k = TerrainGenerator.ChunkBases[x, z].Kingdom;
                if ( (TerrainGenerator.ChunkBases[x + 1, z].Kingdom != k && TerrainGenerator.ChunkBases[x + 1, z].Kingdom != null)
                    || (TerrainGenerator.ChunkBases[x - 1, z].Kingdom != k && TerrainGenerator.ChunkBases[x - 1, z].Kingdom != null)
                    || (TerrainGenerator.ChunkBases[x, z + 1].Kingdom != k && TerrainGenerator.ChunkBases[x, z + 1].Kingdom != null)
                    || (TerrainGenerator.ChunkBases[x, z - 1].Kingdom != k && TerrainGenerator.ChunkBases[x, z - 1].Kingdom != null))
                {
                    TerrainGenerator.ChunkBases[x, z].SetBorder(true);
                    BorderChunks.Add(new Vec2i(x, z));
                }
            }
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public Kingdom ClosestKingdomToChunk(List<Kingdom> kingdoms, Vec2i v)
    {
        // UnityEngine.Profiling.Profiler.BeginSample("territory_choose");

        Kingdom current = null;
        float currentDistSqr = 0;
        int i = 0;
        foreach (Kingdom k in kingdoms)
        {
            if (v == k.CapitalChunk)
                return k;
            if (current == null)
            {
                current = k;
                currentDistSqr = Vec2i.QuickDistance(v, k.CapitalChunk);
            }
            else
            {
                float disSqr = Vec2i.QuickDistance(v, k.CapitalChunk);
                if (disSqr < currentDistSqr)
                {
                    currentDistSqr = disSqr;
                    current = k;
                }

            }
        }
        //UnityEngine.Profiling.Profiler.EndSample();

        return current;
    }
    /*

    /// <summary>
    /// Here we generate 'count' kingdoms and claim their territory
    /// </summary>
    /// <param name="count"></param>
    public void GenerateEmptyKingdoms(int count)
    {
        //Creates a list of all land chunks
        World.GetAllLandChunks();

        //We choose the capital city Coordinates for all of the kingdoms
        Vec2i[] KingdomCapitals = ChooseKingdomCapitals(count);
        //We then create all the kingdoms based on these capitals
        List<Kingdom> emptyKingdoms = GenerateEmptyKingdoms(KingdomCapitals);
        
        //Add all the kingdoms to the World's kingdoms
        foreach (Kingdom k in emptyKingdoms)
        {
            World.Kingdoms.Add(k);
        }
        //We then claim the territory for all kingdoms
        ClaimKingdomTerritorys();

    }

    public void GenerateAllKingdomSettlements()
    {
        foreach (Kingdom k in World.Kingdoms)
        {
            SettlementGenerator setGen = new SettlementGenerator(k);
            setGen.GenerateAllSettlements();
        }
    }

    /// <summary>
    /// Claims all territory for each kingdom
    /// </summary>
    /// <param name="emptyKingdoms"></param>
    public void ClaimKingdomTerritorys()
    {

        //Iterate every land chunk in the world
        for (int i = 0; i < World.LandChunks.Count; i++)
        {
            Vec2i v = World.LandChunks[i];
            //Find which Capital city the chunk is closest to
            //Give the chunk to that kingdom
            Kingdom k = ClosestKingdomToChunk(v);
            k.AddTerritory(v);

        }


        foreach (Kingdom k in World.Kingdoms)
        {
            //We may have added duplicates earlier in this method, so we clean that up.
            k.CleanupTerritory();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public Kingdom ClosestKingdomToChunk(Vec2i v)
    {
       // UnityEngine.Profiling.Profiler.BeginSample("territory_choose");

        Kingdom current = null;
        float currentDistSqr = 0;
        foreach (Kingdom k in World.Kingdoms)
        {
            if (v == k.CapitalChunk)
                return k;
            if (current == null)
            {
                current = k;
                currentDistSqr = Vec2i.QuickDistance(v, k.CapitalChunk);
            }
            else
            {
                float disSqr = Vec2i.QuickDistance(v, k.CapitalChunk);
                if (disSqr < currentDistSqr)
                {
                    currentDistSqr = disSqr;
                    current = k;
                }

            }
        }
        //UnityEngine.Profiling.Profiler.EndSample();

        return current;
    }*/

    public List<Kingdom> GenerateEmptyKingdoms(Vec2i[] capitals)
    {
        List<Kingdom> emptyKingdoms = new List<Kingdom>();
        int i = 1;
        foreach (Vec2i v in capitals)
        {
            Kingdom k = new Kingdom("kingdom" + i, v);
            emptyKingdoms.Add(k);
            i++;
        }

        return emptyKingdoms;
    }
    
    private Vec2i[] ChooseKingdomCapitals(List<Vec2i> land, int count)
    {
        
        List<Vec2i> kingdomCapitals = new List<Vec2i>();
        float minDistance = 40;
        /*

        for (int i=0; i<count; i++)
        {

            Vec2i pos = FindSpace(null, 10);
            if (pos == null)
                i--;
            else
            {
                if (kingdomCapitals.Count == 0)
                {
                    kingdomCapitals.Add(pos);
                }
                else
                {
                    bool isValid = false;

                    while (!isValid)
                    {
                        if (MinDistanceBetweenPoints(kingdomCapitals, pos) < minDistance)
                        {
                            isValid = true;
                            kingdomCapitals.Add(pos);
                        }
                        else
                        {
                            pos = FindSpace(null, 10);
                        }
                    }
                }
            }
            

        }
        return kingdomCapitals.ToArray();*/
        //We select the first kingdom to have a random position.


        int checkSize = 8;

        kingdomCapitals.Add(MiscUtils.RandomFromArray(land, Random.value));
        for (int i = 1; i < count; i++)
        {
            //We choose a random point
            Vec2i ran = MiscUtils.RandomFromArray(land, Random.value);

            if (ran.x - checkSize < 0 || ran.z - checkSize < 0 || ran.x + checkSize >= World.WorldSize || ran.z + checkSize >= World.WorldSize)
                i--;
            else if (kingdomCapitals.Contains(ran))
            {
                i--;
            }
            else if (MinDistanceBetweenPoints(kingdomCapitals, ran) < minDistance)
            {
                i--;
            }
            else
            {
                kingdomCapitals.Add(ran);
            }
        }
        return kingdomCapitals.ToArray();
    }
    /*
    /// <summary>
    /// Chooses the main chunk position (centre of each kingdom) for each kingdom.
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    private Vec2i[] ChooseKingdomCapitals(int count)
    {
        List<Vec2i> kingdomCapitals = new List<Vec2i>();

        //We select the first kingdom to have a random position.

        float minDistance = 40;

        kingdomCapitals.Add(MiscUtils.RandomFromArray(World.LandChunks, Random.value));
        for (int i = 1; i < count; i++)
        {
            //We choose a random point
            Vec2i ran = MiscUtils.RandomFromArray(World.LandChunks, Random.value);
            if (kingdomCapitals.Contains(ran))
            {
                i--;
            }
            else if (MinDistanceBetweenPoints(kingdomCapitals, ran) < minDistance)
            {
                i--;
            }
            else
            {
                kingdomCapitals.Add(ran);
            }
        }
        return kingdomCapitals.ToArray();
    }
    */
    private float MinDistanceBetweenPoints(List<Vec2i> current, Vec2i newPoint)
    {

        //We set the default minSqr to the distance between the first point and test point.
        float minSqr = Vec2i.QuickDistance(current[0], newPoint);
        //Iterate each point
        foreach (Vec2i v in current)
        {
            //Find this distance to test point
            float disSqr = Vec2i.QuickDistance(v, newPoint);
            //If it is less than the current minimum, then set to shortest distance.
            if (disSqr < minSqr)
            {
                minSqr = disSqr;
            }
        }


        return Mathf.Sqrt(minSqr);
    }
    

}