using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class WorldEventManager : MonoBehaviour
{

    public static WorldEventManager Instance;


    public List<EntityGroup> EntityGroups;
    private void Awake()
    {
        Instance = this;
        EntityGroups = new List<EntityGroup>();
    }


    private Dictionary<Vec2i, List<EntityGroup>> Groups;

    private Object ThreadLock;

    public WorldEventPathFinder PathFinder { get; private set; }
    public ChunkBase2[,] ChunkBases { get; private set; }
    public GridPlacement GridPlacement { get; private set; }

    public List<Settlement> Settlements = new List<Settlement>();
    public List<ChunkStructure> ChunkStructures;

    private GenerationRandom GenRan;

    public void Init(ChunkBase2[,] chunks, GridPlacement gp, List<Settlement> settlements, List<ChunkStructure> chunkStructures)
    {
        Groups = new Dictionary<Vec2i, List<EntityGroup>>();
        PathFinder = new WorldEventPathFinder(chunks);
        GridPlacement = gp;
        ChunkBases = chunks;
        Debug.Log(settlements.Count);
        Settlements = settlements;
        ChunkStructures = chunkStructures;
        ThreadLock = new Object();
        GenRan = new GenerationRandom(0);      

    }



    float t = 0;
    private void Update()
    {
        t += Time.deltaTime;
        if (t > 0.5f)
        {
            Debug.Log("TICK");
            t = 0;
            
            try
            {
                EconomyTick();
                ChunkStructureTick();
                MovementTick();
            }catch(System.Exception e)
            {
                Debug.LogError(e);
            }
            
        }
    }

    public List<EntityGroup> GetEntityGroupsNearPoint(Vec2i point, int search)
    {
        List<EntityGroup> allGroups = new List<EntityGroup>();
        for(int x=-search; x<= search; x++)
        {
            for(int z=-search; z<= search; z++)
            {
                Vec2i p = new Vec2i(x, z) + point;
                List<EntityGroup> pg = GetAllGroupsAtPoint(p);
                if (pg != null)
                    allGroups.AddRange(pg);
            }
        }
        return allGroups;


    }

    public List<EntityGroup> GetAllGroupsAtPoint(Vec2i point)
    {
        if (point.x < 0 || point.z < 0 || point.x >= World.WorldSize || point.z >= World.WorldSize)
            return null;
        if (Groups.TryGetValue(point, out List<EntityGroup> g))
            return g;
        return null;
    }



    public EntityGroup SpawnBanditPatrol(ChunkStructure camp, List<Entity> groupEntities)
    {
        EntityGroupBandits bandits = new EntityGroupBandits(camp, groupEntities);


        AddEntityGroup(bandits);

        return bandits;
    }

    /// <summary>
    /// Spawns an entity group that exports a set type of good from a village, and takes it to the
    /// nearest valid settlement to sell.
    /// </summary>
    /// <param name="home"></param>
    /// <param name="exportType"></param>
    /// <param name="export"></param>
    /// <param name="groupEntities"></param>
    /// <returns></returns>
    public EntityGroup SpawnVillageTrader(SettlementEconomy economy, EconomicItemType exportType, EconomicInventory export, List<Entity> groupEntities)
    {
        Debug.Log("Spawning village trader");
        SettlementEconomy target = null;
        int nearestDist = -1;
        //We iterate the near settlements in an attempt to find a settlement to import these goods
        foreach (int id in economy.NearSettlementsIDs)
        {
            Settlement set = World.Instance.GetSettlement(id);
            if (set == null)
                continue;
            //We skip other villages
            if (set.SettlementType == SettlementType.VILLAGE)
                continue;

            if (!set.Economy.CanImport(exportType))
                continue;
            int dist = Vec2i.QuickDistance(economy.Settlement.BaseChunk, set.BaseChunk);
            if (nearestDist == -1 || dist < nearestDist)
            {
                nearestDist = dist;
                target = set.Economy;
            }
        }
        //If a valid settlement is not found to be close, 
        if(target == null)
        {
            //We search all settlements
            target = GetNearestImportingSettlement(economy.Settlement, exportType);
            //This should never happen
            if(target == null)
            {
                //in future, the only way this could happen would be if a kingdom cannot trade with any other kingdoms.
                Debug.LogError("Could not find Settlement to import " + exportType + " near to " + economy.Settlement);
                return null;
            }
                
        }

        EntityGroup.GroupType groupType = exportType.GetTypeFromExport();

        VillageTraderTask task = new VillageTraderTask();
        task.Start = economy;
        task.End = target;
        task.DesiredPurchases = economy.RequiredImports;
        task.ToSell = export;
        EntityGroup group = new EntityGroupVillageTrader(task, groupType, groupEntities);
        AddEntityGroup(group);
        return group;

    }
    /// <summary>
    /// Searches all settlements in the world, and returns the nearest settlement
    /// that will accept the specified import
    /// </summary>
    /// <param name="start">The settlement that we are exporting from - find closest valid settlement to this</param>
    /// <param name="exportType">The type of economic item we wish our target settlement to import</param>
    /// <returns></returns>
    private SettlementEconomy GetNearestImportingSettlement(Settlement start, EconomicItemType exportType)
    {

        Settlement target = null;
        int closestDist = -1;
        //We iterate all settlements in the world
        foreach(KeyValuePair<int, Settlement> sets in World.Instance.WorldSettlements)
        {
            if (sets.Value == start)
                continue;
            //If the settlement cannot import the required type, we will ignore it
            if (!sets.Value.Economy.CanImport(exportType))
                continue;
            //Find the distance from the start to this settlement
            int sqrDist = sets.Value.BaseChunk.QuickDistance(start.BaseChunk);

            if(closestDist == -1 || sqrDist < closestDist)
            {
                target = sets.Value;
                closestDist = sqrDist;
            }
        }
        if (target == null)
            return null;
        return target.Economy;

    }



    public List<Vec2i> GeneratePath(Vec2i startChunk, Vec2i endChunk)
    {


        List<Vec2i> path = new List<Vec2i>(60);
        //Find the nearest points as required
        GridPoint a = GridPlacement.GetNearestPoint(startChunk);
        GridPoint b = GridPlacement.GetNearestPoint(endChunk);

        if(a != null && b != null)
        {
            Debug.Log("YAAAYY!!");
           // Debug.BeginDeepProfile("PathFind_start");
            /*
            if (startChunk != a.ChunkPos)
                path.AddRange(PathFinder.GeneratePath(startChunk, a.ChunkPos));

            Debug.EndDeepProfile("PathFind_start");
            */
            Debug.BeginDeepProfile("PathFind_mid");

            path.Add(startChunk);

            path.AddRange(GridPlacement.ConnectPointsGridPoints(a, b));
            Debug.EndDeepProfile("PathFind_mid");

            path.Add(endChunk);
            /*
            Debug.BeginDeepProfile("PathFind_end");

            if (b.ChunkPos != endChunk)
                path.AddRange(PathFinder.GeneratePath(b.ChunkPos, endChunk));

            Debug.EndDeepProfile("PathFind_end");
            */
        }
        else
        {
            Debug.BeginDeepProfile("PathFind_null");
            path = PathFinder.GeneratePath(startChunk, endChunk);
            Debug.EndDeepProfile("PathFind_null");
        }
        
        return path;

    }


    private void AddEntityGroup(EntityGroup g)
    {
        EconomyTest.Instance.AddEntityGroup(g);
        Debug.Log("Adding entity group " + g);
        Vec2i v = g.CurrentChunk;
        if (!Groups.ContainsKey(v))
            Groups.Add(v, new List<EntityGroup>());
        Groups[v].Add(g);
    }



    private void ChunkStructureTick()
    {
        if (ChunkStructures == null)
            return;
                
        foreach(ChunkStructure cs in ChunkStructures)
        {
            cs.Tick();
        }
    }

    /// <summary>
    /// Calculates the movement of all entity groups for this tick
    /// </summary>
    private void MovementTick()
    {
        Dictionary<Vec2i, List<EntityGroup>> nextGroups = new Dictionary<Vec2i, List<EntityGroup>>();
        foreach (KeyValuePair<Vec2i, List<EntityGroup>> groups in Groups)
        {
            
            if(groups.Value.Count > 1)
            {
                foreach(EntityGroup g in groups.Value)
                {
                    g.OnGroupInteract(groups.Value);
                }

            }
            
            foreach (EntityGroup g in groups.Value)
            {
                if (g.ShouldDestroy)
                {
                    if (EconomyTest.Instance != null)
                    {
                        EconomyTest.Instance.RemoveEntityGroup(g);

                    }
                }
                else
                {
                    Vec2i npos = null;
                    g.DecideNextTile(out npos);
                    //Vec2i npos = g.NextPathPoint();
                    if (npos == null)
                    {
                        g.OnReachDestination(g.EndChunk);
                        npos = g.CurrentChunk;
                        continue;
                        // continue;

                    }
                    if (npos == null)
                        continue;
                    //Debug.Log("Moved position to " + npos);
                    if (!nextGroups.ContainsKey(npos))
                        nextGroups.Add(npos, new List<EntityGroup>());
                    nextGroups[npos].Add(g);
                }

            }

        }
        Groups = nextGroups;
    }
    /// <summary>
    /// Calculates the economic changes for all settlements for this tick.
    /// Will spawn entity groups for trading as required.
    /// </summary>
    private void EconomyTick()
    {
        foreach(Settlement s in Settlements)
        {
            s.Tick();
            /*
            if (s != null && s.Economy != null)
                s.Economy.Tick();*/
        }
        
    }



    public ChunkBase2[,] GetData()
    {
        lock (ThreadLock)
        {
            return ChunkBases;
        }
    }

    private ChunkBase2[,] CopyCurrent()
    {
        ChunkBase2[,] data = new ChunkBase2[World.WorldSize, World.WorldSize];
        lock (ThreadLock)
        {
            for (int x = 0; x < World.WorldSize; x++)
            {
                for (int z = 0; z < World.WorldSize; z++)
                {
                    data[x, z] = ChunkBases[x, z];
                }
            }
        }
        return data;
    }


}

public interface IWorldEventLocation
{
    void Tick();

    void GroupReturn(EntityGroup group);

}