using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class WorldEventManager : MonoBehaviour
{

    public static WorldEventManager Instance;


    public List<EntityGroup> EntityGroups;


    private List<WorldLocation> EventLocations;

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


    public List<WorldLocation> WorldEventLocations;

    private GenerationRandom GenRan;

    public void Init(ChunkBase2[,] chunks, GridPlacement gp, List<WorldLocation> eventLocations)
    {
        Groups = new Dictionary<Vec2i, List<EntityGroup>>();
        PathFinder = new WorldEventPathFinder(chunks);
        GridPlacement = gp;
        ChunkBases = chunks;

        WorldEventLocations = eventLocations;

        
        ThreadLock = new Object();
        GenRan = new GenerationRandom(0);      

    }



    float t = 0;
    /// <summary>
    /// Runs the world event ticks when required
    /// TODO - do this on seperate thread
    /// </summary>
    private void Update()
    {
        Debug.BeginDeepProfile("WorldEventTick");
        LocationTick();
        MovementTick();
        Debug.EndDeepProfile("WorldEventTick");

    }


    public void GetEntityGroupsNearPoint(Vec2i point, int search, List<EntityGroup> groups)
    {
        for (int x = -search; x <= search; x++)
        {
            for (int z = -search; z <= search; z++)
            {
                Vec2i p = new Vec2i(x, z) + point;
                List<EntityGroup> pg = GetAllGroupsAtPoint(p);
                if (pg != null)
                    groups.AddRange(pg);
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
        foreach(KeyValuePair<int, WorldLocation> locs in World.Instance.WorldLocations)
        {
            //if this location is not a settlement
            if (!(locs.Value is Settlement))
                continue;
            Settlement set = locs.Value as Settlement;
            //If the settlement cannot import the required type, we will ignore it
            if (!set.Economy.CanImport(exportType))
                continue;
            //Find the distance from the start to this settlement
            int sqrDist = set.BaseChunk.QuickDistance(start.BaseChunk);

            if(closestDist == -1 || sqrDist < closestDist)
            {
                target = set;
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
            path.Add(startChunk);
            path.Add(endChunk);
            return path;


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



   
    private void LocationTick()
    {
        foreach(WorldLocation wel in WorldEventLocations)
        {
            wel?.Tick();
        }
    }

    /// <summary>
    /// Calculates the movement of all entity groups for this tick
    /// </summary>
    private void MovementTick()
    {
        Debug.BeginDeepProfile("MovementTick");
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
                    RemoveEntityGroup(g);
                }
                else
                {
                    //We find the next chunk the entity group will sit on
                    Vec2i npos = null;
                    g.DecideNextTile(out npos);
                    //If this point is null, then we let the entity reach its destination
                    if (npos == null)
                    {
                        if (!g.OnReachDestination(g.EndChunk))
                        {
                            RemoveEntityGroup(g);
                            continue;
                        }
                            
                        npos = g.CurrentChunk;
                        // continue;

                    }
                    if (npos == null)
                    {
                        RemoveEntityGroup(g);
                        continue;
                    }
                        
                    //Debug.Log("Moved position to " + npos);
                    if (!nextGroups.ContainsKey(npos))
                        nextGroups.Add(npos, new List<EntityGroup>());
                    nextGroups[npos].Add(g);
                }

            }

        }
        Groups = nextGroups;
        Debug.EndDeepProfile("MovementTick");

    }

    private void RemoveEntityGroup(EntityGroup g)
    {
        if (EconomyTest.Instance != null)
        {
            EconomyTest.Instance.RemoveEntityGroup(g);

        }
    }



}
/// <summary>
/// World Event Location is an interface shared by all 
/// objects that represent a location on the world map that entity groups
/// can travel between.
/// - > All settlements (via settlement economoy) and chunk structures
/// </summary>
public abstract class WorldLocation
{
    /// <summary>
    /// The position of this world location in chunk coordinates
    /// </summary>
    public Vec2i ChunkPos { get; private set; }
    public int LocationID { get { return ChunkPos.GetHashCode(); } }

    public string Name { get; protected set; }
    public WorldMapLocation WorldMapLocation { get; private set; }

    public WorldLocation(Vec2i cPos)
    {
        ChunkPos = cPos;
    }
    public void SetWorldMapLocation(WorldMapLocation wml)
    {
        WorldMapLocation = wml;
    }
    public void SetName(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Calls the update tick for this location
    /// In settlements, this will update the economy
    /// In chunk structures, this will randomly spawn a relevent entity
    /// group
    /// </summary>
    public abstract void Tick();

    /// <summary>
    /// Called when the entity group param has returned back home
    /// </summary>
    /// <param name="group"></param>
    public abstract void GroupReturn(EntityGroup group);

}