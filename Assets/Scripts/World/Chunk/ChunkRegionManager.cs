using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;

public class ChunkRegionManager : MonoBehaviour
{
    public static int LoadChunkRadius = 6;
    private Object ThreadSafe;
    public ChunkRegion[,] LoadedRegions { get; private set; }
    public Dictionary<Vec2i, LoadedChunk2> LoadedChunks { get; private set; }

    private World World { get { return GameManager.WorldManager.World; } }
    private Player Player { get { return GameManager.PlayerManager.Player; } }
    public Vec2i LoadedChunksCentre { get; private set; }
    public Dictionary<Vec2i, LoadedChunk2> SubworldChunks { get; private set; }

    private Subworld CurrentSubworld;

    public bool InSubworld { get { return CurrentSubworld != null; } }


    private ChunkLoader ChunkLoader;


    void Awake()
    {
        LoadedChunks = new Dictionary<Vec2i, LoadedChunk2>();
        LoadedRegions = new ChunkRegion[World.RegionCount, World.RegionCount];

        SubworldChunks = new Dictionary<Vec2i, LoadedChunk2>();
        ThreadSafe = new Object();

        ChunkLoader = GetComponent<ChunkLoader>();
    }
    private void Start()
    {

    }


    private void Update()
    {
        if (InSubworld)
            return;
        Vec2i playerChunk = World.GetChunkPosition(Player.Position);
        bool forceLoad = false;
        if (LoadedChunksCentre == null)
            forceLoad = true;
        else if(playerChunk.QuickDistance(LoadedChunksCentre) > LoadChunkRadius* LoadChunkRadius * 2)
        {
            UnloadAllChunks();
            forceLoad = true;
        }
        if (playerChunk != LoadedChunksCentre)
        {
            LoadChunks(new Vec2i(playerChunk.x, playerChunk.z), LoadChunkRadius, forceLoad);
        }



    }


   


    public void LoadSubworldChunks(Subworld sub)
    {

        UnloadAllChunks();
        CurrentSubworld = sub;

        foreach(ChunkData cd in sub.SubworldChunks)
        {
            ChunkLoader.LoadChunk(cd);
        }
        ChunkLoader.ForceLoadAll();

        //TODO - fix this bad boy
        return;
        /*
        UnloadAllChunks();
        InSubworld = true;
        foreach (ChunkData c in sub.SubworldChunks)
        {
            int x = c.X;
            int z = c.Z;
            GameObject chunkObject = Instantiate(ResourceManager.ChunkPrefab); ; //We create a new empty gameobject for the chunk
            chunkObject.transform.parent = transform;
            chunkObject.name = "sw" + sub.SubworldID + "_" + c.X + "_" + c.Z;
            LoadedChunk loadedChunk = chunkObject.AddComponent<LoadedChunk>();

            ChunkData[] neigh = { sub.GetChunkSafe(x, z + 1), sub.GetChunkSafe(x + 1, z + 1), sub.GetChunkSafe(x + 1, z) };

            loadedChunk.SetChunkData(c, neigh);
            SubworldChunks.Add(new Vec2i(x,z), loadedChunk);
        }*/
    }

    public void LeaveSubworld()
    {
        UnloadAllChunks();
        CurrentSubworld = null;
        LoadedChunksCentre = null;
        return;
        /*
        InSubworld = false;
        foreach (KeyValuePair<Vec2i, LoadedChunk> kvp in SubworldChunks)
        {
            Destroy(kvp.Value.gameObject);

        }
        SubworldChunks.Clear();*/
    }

    /// <summary>
    /// Checks if the specified chunk coordinate is currently loaded, 
    /// checking both the world, and checking if a subworld is loaded
    /// </summary>
    /// <param name="chunkPos"></param>
    /// <returns></returns>
    public bool IsCurrentChunkPositionLoaded(Vec2i chunkPos)
    {/*
        if (InSubworld)
        {
            return SubworldChunks.ContainsKey(chunkPos);
        }*/
        return LoadedChunks.ContainsKey(chunkPos);
    }



    public ChunkData GetChunk(int x, int z, bool shouldLoad=true)
    {
        return GetChunk(new Vec2i(x, z), shouldLoad);
    }
    public ChunkData GetChunk(Vec2i v, bool shouldLoad=true)
    {
        //Debug.BeginDeepProfile("get_chunk");
        //Find region of chunk and check if valid within bounds
        Vec2i r = World.GetRegionCoordFromChunkCoord(v);
        //Debug.Log("Chunk " + v + " in region " + r);
        if (r.x >= World.RegionCount || r.z >= World.RegionCount || r.x < 0 || r.z < 0)
        {
           // Debug.Log("Region " + r + " out of bounds");
            //Debug.EndDeepProfile("get_chunk");

            return null;
        }
        ChunkRegion cr;
        lock (ThreadSafe)
        {
            cr = LoadedRegions[r.x, r.z];
        }
        
        //Get chunk region, check if it has been loaded
        if (cr == null && shouldLoad)
        {
            //Debug.Log("[CRManager] Region null, trying to load");
            //If it has not been laoded, then load it
            LoadRegion(r);
            cr = LoadedRegions[r.x, r.z];
            if (cr == null)
            {
                //Debug.EndDeepProfile("get_chunk");
                return null;
            }
            lock (ThreadSafe)
            {
                cr = LoadedRegions[r.x, r.z];
            }
            
            //GameManager.PathFinder.LoadRegion(cr);
        }else if(cr == null)
        {
            //Debug.EndDeepProfile("get_chunk");

            //Debug.Log("[CRManager] Region " + r + " could not be found - not generating");
            return null;
        }
            

        ChunkData cDat;
        lock (ThreadSafe)
        {
            int cx = v.x % World.RegionSize;
            int cz = v.z % World.RegionSize;
            cDat = cr.Chunks[cx, cz];
            //Debug.Log("[CRManager] Chunk at local region position " + cx + "," + cz + ": " + cDat);
        }
        //Debug.EndDeepProfile("get_chunk");

        return cDat;
    }
    public void LoadRegion(Vec2i rPos)
    {
        if (rPos.x >= World.RegionCount || rPos.z >= World.RegionCount || rPos.x < 0 || rPos.z < 0)
        {
            throw new System.Exception("Region " + rPos + " is not within world bounds");
        }

        //If valid, load and add to array
        if (GameManager.ChunkRegionGenerator.IsGeneratingRegion(rPos))
            return;
        LoadedRegions[rPos.x, rPos.z] = GameManager.LoadSave.LoadChunkRegion(rPos.x, rPos.z);
        if (LoadedRegions[rPos.x, rPos.z] == null || LoadedRegions[rPos.x, rPos.z].Generated == false)
        {
            Debug.Log("[CRManager] Region " + rPos + " is null, attempting to generate");
            LoadedRegions[rPos.x, rPos.z] = GameManager.ChunkRegionGenerator.ForceGenerateRegion(rPos);
            if (LoadedRegions[rPos.x, rPos.z] == null)
                Debug.Log("[CRManager] Region " + rPos + " could not be loaded");
        }
        else
        {
            Debug.Log("[CRManager] Region " + rPos + " loaded succesfully");
        }
        GameManager.PathFinder.LoadRegion(LoadedRegions[rPos.x, rPos.z]);

    }


    /// <summary>
    /// Loads all chunks within a square of size 2*radius centred on the player
    /// </summary>
    /// <param name="middle"></param>
    /// <param name="radius"></param>
    public void LoadChunks(Vec2i middle, int radius, bool forceLoad)
    {
        Debug.BeginDeepProfile("load_chunks");

        Debug.BeginDeepProfile("load_chunks1");
        //A list containing all the chunks currently loaded
        List<Vec2i> currentlyLoaded = new List<Vec2i>();
        foreach(KeyValuePair<Vec2i,LoadedChunk2> kvp in LoadedChunks)
        {
            currentlyLoaded.Add(kvp.Value.Position);
        }
        //We do not wish to start generating a chunk if it has already been added to the generation loop
        currentlyLoaded.AddRange(ChunkLoader.GetCurrentlyLoadingChunks());
        LoadedChunksCentre = middle;

        //We initiate a list of all the chunks we wish to unload.
        //To start, this is a list of all currently loaded chunks.
        List<Vec2i> toUnload = new List<Vec2i>(currentlyLoaded);

        Debug.Log("Currently loaded " + currentlyLoaded.Count);
        Debug.EndDeepProfile("load_chunks1");
        Debug.BeginDeepProfile("load_chunks2");

        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                //Check if the requested position is inside world bounds
                if (x + middle.x < 0 || x + middle.x >= World.WorldSize - 1 || z + middle.z < 0 || z + middle.z >= World.WorldSize - 1)
                    continue;
                Vec2i pos = new Vec2i(x + middle.x, z + middle.z);
                //If this chunk isn't currently loaded/being loaded
                if(!currentlyLoaded.Contains(pos)){
                    ChunkData cd = GetChunk(pos);
                    if (cd == null)
                    {
                        Debug.Log("Chunk at " + pos +  " was null");
                        continue;
                    }
                    GameManager.EntityManager.LoadChunk(pos);
                    ChunkLoader.LoadChunk(cd);
                }else if (toUnload.Contains(pos))
                {
                    //We check if 'toUnload' contains this position (this should always happen if the chunk
                    //isn't already loaded.
                    //We then remove it from this list, as this list will define which chunks need to be unloaded.
                    toUnload.Remove(pos);
                }
            }
        }
        Debug.EndDeepProfile("load_chunks2");

        if (forceLoad)
        {
            Debug.Log("[CRManager] Forcing Chunk Loader to load all chunks");
            ChunkLoader.ForceLoadAll();
        }
        if(toUnload.Count != 0)
        {
            foreach(Vec2i v in toUnload)
            {
                if (LoadedChunks.ContainsKey(v))
                {
                    LoadedChunk2 lc = LoadedChunks[v];
                    Destroy(lc.gameObject);
                    LoadedChunks.Remove(v);
                }
                EntityManager.Instance.UnloadChunk(v);
                //TODO - unload entity chunk
            }
        }
        Debug.EndDeepProfile("load_chunks");


    }

    public LoadedChunk2 GetLoadedChunk(Vec2i chunk)
    {
        if(LoadedChunks.TryGetValue(chunk, out LoadedChunk2 val))
        {
            return val;
        }
        return null;

    }

   


   
    public void UnloadChunk(Vec2i chunk)
    {
        if (LoadedChunks.ContainsKey(chunk))
        {
            LoadedChunk2 loaded = LoadedChunks[chunk];
            LoadedChunks.Remove(chunk);
            //GameManager.EntityManager.UnloadChunk(chunk);
            Destroy(loaded.gameObject);
        }
    }

    public void UnloadAllChunks()
    {
        List<Vec2i> chunkKeys = new List<Vec2i>();

        foreach (KeyValuePair<Vec2i, LoadedChunk2> kpv in LoadedChunks)
        {
            chunkKeys.Add(kpv.Key);
            Destroy(kpv.Value.gameObject);
        }
        LoadedChunks.Clear();
        GameManager.EntityManager.UnloadChunks(chunkKeys);
    }

    public ChunkData[] GetNeighbors(Vec2i c)
    {

        if (InSubworld)
        {
            return new ChunkData[] { CurrentSubworld.GetChunkSafe(c.x, c.z+1), CurrentSubworld.GetChunkSafe(c.x+1, c.z + 1),
                                      CurrentSubworld.GetChunkSafe(c.x+1, c.z), };
         
        }
        else if(c.x > 0 && c.x < World.WorldSize - 2 && c.z > 0 && c.z < World.WorldSize - 2)
        {
            //TODO - get this data from the CR manager
            return new ChunkData[] { GetChunk(c.x, c.z+1, false), GetChunk(c.x+1, c.z + 1, false),
                                      GetChunk(c.x+1, c.z, false),/* GetChunk(c.x+1, c.z - 1, false),
                                      GetChunk(c.x, c.z-1, false), GetChunk(c.x-1, c.z-1, false)  ,
                                      GetChunk(c.x-1,c.z, false), GetChunk(c.x-1, c.z+1 , false)*/};
        }

        else
        {
            return null;
        }

    }
}