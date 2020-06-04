using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class WorldEventManager : MonoBehaviour
{

    public static WorldEventManager Instance;

    private void Awake()
    {
        Instance = this;
    }


    private Object ThreadLock;

    public WorldEventPathFinder PathFinder { get; private set; }
    public ChunkBase2[,] ChunkBases { get; private set; }
    public GridPlacement GridPlacement { get; private set; }

    private List<SettlementEconomy> Economies;


    public void SetEconomies(List<SettlementEconomy> econ)
    {
        Debug.Log("Has set");
        Economies = econ;
    }

    public void Init(ChunkBase2[,] chunks, GridPlacement gp, List<Settlement> settlements, List<ChunkStructure> chunkStructures)
    {
        PathFinder = new WorldEventPathFinder(chunks);
        GridPlacement = gp;
        ChunkBases = chunks;
        
        ThreadLock = new Object();
    }

    float t = 0;
    private void Update()
    {
        t += Time.deltaTime;
        if(t > 10)
        {
            Debug.Log("TICK");
            t = 0;
            EconomyTick();

        }
    }

    /// <summary>
    /// Starts a single world tick in a s
    /// </summary>
    public void Tick()
    {
        ChunkBase2[,] copy = CopyCurrent();
        ChunkBase2[,] next = CopyCurrent();

        //We prepair an empty set to fill
        for(int x=0; x<World.WorldSize; x++)
        {
            for(int z=0; z<World.WorldSize; z++)
            {
                next[x, z].ClearEntityGroups();
            }
        }

        for (int x = 0; x < World.WorldSize; x++)
        {
            for (int z = 0; z < World.WorldSize; z++)
            {
                ChunkBase2 cb = copy[x, z];
                //If this chunk has entity groups
                if (cb.HasEntityGroups())
                {
                    List<EntityGroup> groups = cb.GetEntityGroups();
                    //If there is only a single group here
                    if(groups.Count == 1)
                    {
                        Vec2i nPos = groups[0].NextPathPoint();
                        if (nPos == null)
                            AtPathEnd(next, groups[0], groups[0].FinalPathPoint());
                        else
                            next[nPos.x, nPos.z].AddEntityGroup(groups[0]);
                    }
                }
            }
        }

        lock (ThreadLock)
        {
            ChunkBases = next;
        }

    }

    private void EconomyTick()
    {
        foreach(SettlementEconomy setEc in Economies)
        {
            if(setEc != null)
            {
                setEc.Tick();
            }
        }
    }

    private void AtPathEnd(ChunkBase2[,] next, EntityGroup group, Vec2i lastPos)
    {
        if(group.Type == EntityGroup.GroupType.Traders)
        {
            ChunkBase2 cb = next[lastPos.x, lastPos.z];
            if (cb.HasSettlement)
            {

            }
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