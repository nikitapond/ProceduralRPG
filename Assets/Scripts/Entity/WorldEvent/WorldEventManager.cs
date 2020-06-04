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
    private ChunkBase[,] ChunkBases;


    private List<SettlementEconomy> Economies;
    public void SetEconomies(List<SettlementEconomy> econ)
    {
        Debug.Log("Has set");
        Economies = econ;
    }

    public void Init(ChunkBase[,] chunks, List<Settlement> settlements, List<ChunkStructure> chunkStructures)
    {
        PathFinder = new WorldEventPathFinder(chunks);
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
        ChunkBase[,] copy = CopyCurrent();
        ChunkBase[,] next = CopyCurrent();

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
                ChunkBase cb = copy[x, z];
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

    private void AtPathEnd(ChunkBase[,] next, EntityGroup group, Vec2i lastPos)
    {
        if(group.Type == EntityGroup.GroupType.Traders)
        {
            ChunkBase cb = next[lastPos.x, lastPos.z];
            if (cb.HasSettlement)
            {

            }
        }

    }


    public ChunkBase[,] GetData()
    {
        lock (ThreadLock)
        {
            return ChunkBases;
        }
    }

    private ChunkBase[,] CopyCurrent()
    {
        ChunkBase[,] data = new ChunkBase[World.WorldSize, World.WorldSize];
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