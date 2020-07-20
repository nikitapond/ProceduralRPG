using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
public class ChunkRegionManager : MonoBehaviour
{

    public static ChunkRegionManager Instance;


    public static int LoadChunkRadius = 40;
    public static int[] LOD_Distances = { 16, 20, 24, 28, 32 , 40};

    private Object ThreadSafe;
    public ChunkRegion[,] LoadedRegions { get; private set; }
    public Dictionary<Vec2i, LoadedChunk2> LoadedChunks { get; private set; }
    private Object LoadedChunksLock = new Object();
    private Dictionary<ChunkData, int> ToGetLoadedChunks;
    private Object ToGetLoadedChunksLOCK = new Object();

    private List<Vec2i> ToUnloadChunks;
    //private Object ToUnloadChunksLOCK = new Object();

    private World World { get { return WorldManager.Instance.World; } }
    private Player Player { get { return PlayerManager.Instance.Player; } }
    public Vec2i LoadedChunksCentre { get; private set; }
    public Dictionary<Vec2i, LoadedChunk2> SubworldChunks { get; private set; }

    private Thread ChunkUpdateThread;


    private Subworld CurrentSubworld;

    public bool InSubworld { get { return CurrentSubworld != null; } }


    public ChunkLoader ChunkLoader { get; private set; }


    void Awake()
    {
        Instance = this;
        ToGetLoadedChunks = new Dictionary<ChunkData, int>();
        LoadedChunks = new Dictionary<Vec2i, LoadedChunk2>();
        LoadedRegions = new ChunkRegion[World.RegionCount, World.RegionCount];


        ToUnloadChunks = new List<Vec2i>();

        SubworldChunks = new Dictionary<Vec2i, LoadedChunk2>();
        ThreadSafe = new Object();

        ChunkLoader = GetComponent<ChunkLoader>();



    }


    /// <summary>
    /// Main chunk update function.
    /// If the player is in a subworld, all chunks are loaded and this function is skipped.
    /// Otherwise, we check if the player has moved chunk. If they have, we call <see cref="LoadedChunks"/> on a thread,
    /// which initiated the loading or LOD change for all required chunks
    /// </summary>
    private void Update()
    {
        if (InSubworld)
            return;
        Debug.BeginDeepProfile("CRUpdate");
        Vec2i playerChunk = World.GetChunkPosition(Player.Position);
        bool forceLoad = false;
        if (LoadedChunksCentre == null)
            forceLoad = true;
        //if we are far (for example, teleporting) then no chunks will be kept, and so we can unload all 
        //require a force load
        else if(playerChunk.QuickDistance(LoadedChunksCentre) > LoadChunkRadius* LoadChunkRadius * 2)
        {
            LoadedChunksCentre = null;
            UnloadAllChunks();
            forceLoad = true;
        }
        //if the players current chunk is different to their last chunk, we update
        if (playerChunk != LoadedChunksCentre)
        {
            //If we are currently running a thread on this, we force stop the thread 
            //TODO - check if this is safe?
            if (ChunkUpdateThread!= null && ChunkUpdateThread.IsAlive)
            {
                ChunkUpdateThread.Abort();
            }
            //We start a thread to load the chunks
            Debug.BeginDeepProfile("ChunkThreadStart");
            ChunkUpdateThread = new Thread(() => LoadChunks(new Vec2i(playerChunk.x, playerChunk.z), LoadChunkRadius, forceLoad));
            ChunkUpdateThread.Start();
            Debug.EndDeepProfile("ChunkThreadStart");
            //LoadChunks(new Vec2i(playerChunk.x, playerChunk.z), LoadChunkRadius, forceLoad);
        }

        //We create a listto hold onto loaded chunks, this allows us to be thread safe
        List<KeyValuePair<Vec2i, LoadedChunk2>> loadedToAdd = new List<KeyValuePair<Vec2i, LoadedChunk2>>();
        //We unload all un-required chunks
        lock (LoadedChunksLock)
        {
            if(ToUnloadChunks.Count > 0)
            {
                foreach(Vec2i v in ToUnloadChunks) {
                    UnloadChunk(v);
                }
                ToUnloadChunks.Clear();
            }            
        }
        //We get all chunks
        lock (ToGetLoadedChunksLOCK)
        {
            if (ToGetLoadedChunks.Count > 0)
            {
                Debug.Log(ToGetLoadedChunks.Count + " chunks to gen");
                foreach (KeyValuePair<ChunkData, int> kvp in ToGetLoadedChunks)
                {
                    Debug.Log("In the loop...");
                    //Get free object instance and add to list of chunks
                    loadedToAdd.Add(new KeyValuePair<Vec2i, LoadedChunk2>(kvp.Key.Position, ChunkLoader.GetLoadedChunk(kvp.Key, kvp.Value)));
                    EntityManager.Instance?.LoadChunk(kvp.Key.Position);

                    //LoadedChunks.Add(kvp.Key.Position, ChunkLoader.GetLoadedChunk(kvp.Key, kvp.Value));
                }
                ToGetLoadedChunks.Clear();
            }
        }
        lock (LoadedChunksLock)
        {
            foreach(KeyValuePair<Vec2i,LoadedChunk2> kvp in loadedToAdd)
            {
                LoadedChunks.Add(kvp.Key, kvp.Value);
            }
        }
        /*
        lock (ToGetLoadedChunksLOCK)
        {
            if(ToGetLoadedChunks.Count > 0)
            {
                foreach(KeyValuePair<ChunkData, int> kvp in ToGetLoadedChunks)
                {
                    //Get free object instance and add to list of chunks
                    LoadedChunks.Add(kvp.Key.Position, ChunkLoader.GetLoadedChunk(kvp.Key, kvp.Value));
                }
                ToGetLoadedChunks.Clear();
            }
        }
        */
        Debug.EndDeepProfile("CRUpdate");

    }

    #region chunk load and unload
    /// <summary>
    /// Loads all chunks within a square of size 2*radius centred on the player
    /// </summary>
    /// <param name="middle"></param>
    /// <param name="radius"></param>
    public void LoadChunks(Vec2i middle, int radius, bool forceLoad)
    {


        Debug.BeginDeepProfile("load_chunks");



        //We define a list of chunks to unload
        List<Vec2i> toUnload = new List<Vec2i>();

        HashSet<int> currentlyLoadedH = new HashSet<int>();
        lock (LoadedChunksLock)
        {
            //A list containing all the chunks currently loaded
            foreach (KeyValuePair<Vec2i, LoadedChunk2> kvp in LoadedChunks)
            {

                int distSqr = middle.QuickDistance(kvp.Key);
                if (distSqr > radius * radius)
                    toUnload.Add(kvp.Key);

                currentlyLoadedH.Add(kvp.Key.GetHashCode());
            }
        }

        lock (ToGetLoadedChunksLOCK)
        {
            foreach (KeyValuePair<ChunkData, int> kvp in ToGetLoadedChunks)
            {
                currentlyLoadedH.Add(kvp.Key.Position.GetHashCode());
            }
        }
        foreach (Vec2i v in ChunkLoader.GetCurrentlyLoadingChunks())
        {
            currentlyLoadedH.Add(v.GetHashCode());
        }
        //We iterate each chunk too far from the player, and unload it
        foreach (Vec2i v in toUnload)
        {
            UnloadChunkSafe(v);
            //UnloadChunk(v);
        }

        LoadedChunksCentre = middle;

        Debug.EndDeepProfile("load_chunks");
        Debug.BeginDeepProfile("load_chunks1");

        //We now iterate the position of each chunk we require
        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                //Check if the requested position is inside world bounds
                if (x + middle.x < 0 || x + middle.x >= World.WorldSize - 1 || z + middle.z < 0 || z + middle.z >= World.WorldSize - 1)
                    continue;
                Vec2i pos = new Vec2i(x + middle.x, z + middle.z);
                int distSqr = middle.QuickDistance(pos);
                if (distSqr > radius * radius)
                    continue;
                int lod = CalculateLOD(distSqr);
                int posHash = pos.GetHashCode();
                //if our currently loaded does not contain the position, we must generate it
                if (!currentlyLoadedH.Contains(posHash))
                {

                    //Attempt to get the chunk data
                    ChunkData cd = GetChunk(pos);
                    if (cd == null)
                    {
                        // Debug.Log("Chunk at " + pos + " was null");
                        continue;
                    }
                    lock (ToGetLoadedChunksLOCK)
                    {
                        ToGetLoadedChunks.Add(cd, lod);
                    }


                    //Load the entities for this chunk
                    //TODO - check this doesn't cause issues with entities loading before chunks?

                    //ChunkLoader.LoadChunk(cd);
                }
                else if (toUnload.Contains(pos))
                {
                    //We check if 'toUnload' contains this position (this should always happen if the chunk
                    //isn't already loaded.
                    //We then remove it from this list, as this list will define which chunks need to be unloaded.
                    toUnload.Remove(pos);
                }
                else
                {
                    //If the chunk is loaded, we get it and set its LOD - this ensures it will update if required
                    LoadedChunk2 lc = GetLoadedChunk(pos);
                    lc.SetLOD(lod);
                }
            }
        }
        Debug.BeginDeepProfile("load_chunks1");
    }

    /// <summary>
    /// A thread safe version of <see cref="UnloadChunk(Vec2i)"/>. Adds the position to a list,
    /// with all chunks unloaded in the update thread <see cref="Update"/>
    /// </summary>
    /// <param name="chunk"></param>
    public void UnloadChunkSafe(Vec2i chunk)
    {
        lock (LoadedChunksLock)
        {
            ToUnloadChunks.Add(chunk);
        }
    }


    public void UnloadChunk(Vec2i chunk)
    {
        if (LoadedChunks.ContainsKey(chunk))
        {
            LoadedChunk2 loaded = LoadedChunks[chunk];
            lock (LoadedChunksLock)
            {
                LoadedChunks.Remove(chunk);
            }


            //We set inactive, and add it the chunk Loaders ChunkBuffer
            loaded.gameObject.SetActive(false);
            ChunkLoader.AddToChunkBuffer(loaded);
            //GameManager.EntityManager.UnloadChunk(chunk);
            //Destroy(loaded.gameObject);
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
        EntityManager.Instance?.UnloadChunks(chunkKeys);
    }


    #endregion

    #region subworld


    public void LoadSubworldChunks(Subworld sub)
    {
        CurrentSubworld = sub;


        if (sub.ShouldUnloadWorldOnEnter())
        {
            UnloadAllChunks();
        }
        else
        {
            foreach(KeyValuePair<Vec2i, LoadedChunk2> kvp in LoadedChunks)
            {
                kvp.Value.gameObject.SetActive(false);
            }
        }

        
        

        SubworldChunks.Clear();
        
        foreach(ChunkData cd in sub.SubworldChunks)
        {
            Debug.Log("Object count:" + cd.WorldObjects.Count);
            SubworldChunks.Add(cd.Position, ChunkLoader.GetLoadedChunk(cd, 1));
            Debug.Log("sub chunk: " + cd.Position);
            //ChunkLoader.LoadChunk(cd);
        }
        CurrentSubworld = sub;
        //ChunkLoader.ForceLoadAll();

        //TODO - fix this bad boy
        return;
        
    }

    public void LeaveSubworld()
    {
        UnloadSubworldChunks();

        if (!CurrentSubworld.ShouldUnloadWorldOnEnter())
        {
            foreach (KeyValuePair<Vec2i, LoadedChunk2> kvp in LoadedChunks)
            {
                kvp.Value.gameObject.SetActive(true);
                EntityManager.Instance?.LoadChunk(kvp.Key);
            }
        }

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

    public void UnloadSubworldChunks()
    {
        //Iterate subworld chunks
        foreach(KeyValuePair<Vec2i, LoadedChunk2> kvp in SubworldChunks)
        {
           
            kvp.Value.gameObject.SetActive(false);
            ChunkLoader.AddToChunkBuffer(kvp.Value);
        }
        SubworldChunks.Clear();
    }

    #endregion

    #region util and get and sets
    /// <summary>
    /// Checks if the specified chunk coordinate is currently loaded, 
    /// checking both the world, and checking if a subworld is loaded
    /// </summary>
    /// <param name="chunkPos"></param>
    /// <returns></returns>
    public bool IsCurrentChunkPositionLoaded(Vec2i chunkPos)
    {
        if (InSubworld)
        {
            return SubworldChunks.ContainsKey(chunkPos);
        }
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
        if (r.x >= World.RegionCount || r.z >= World.RegionCount || r.x < 0 || r.z < 0)
        {
            return null;
        }
        //Check loaded regions for this region
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

        if (GameManager.ChunkRegionGenerator == null)
            return;

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
        if (LoadedRegions[rPos.x, rPos.z] == null)
            return;
        GameManager.PathFinder.LoadRegion(LoadedRegions[rPos.x, rPos.z]);

    }


   


    /// <summary>
    /// Calculates the LOD a chunk should have, given their relative distance to the player
    /// </summary>
    /// <param name="distanceSqr">The square of the distance between the player, and the chunk we are testing</param>
    /// <returns></returns>
    private int CalculateLOD(int distanceSqr)
    {

        //Iterate the distances for each i'th LOD
        for (int i=0; i<LOD_Distances.Length; i++)
        {
            //If test distance is larger than the specified LOD distance, then we have surpassed the LOD of this chunk
            if(distanceSqr < LOD_Distances[i]* LOD_Distances[i])
            {
                return i+1;
            }
        }


        return LOD_Distances.Length-1;

    }
    /// <summary>
    /// Finds the LOD of the 4 neighboring chunks for the specified position
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public int[] CalculateNeighborLODs(Vec2i v)
    {
        if(LoadedChunksCentre == null)
            return new int[]{ 1,1,1,1};

        int[] lods = new int[4];

        for(int i=0; i<4; i++)
        {
            Vec2i v_ = v + Vec2i.QUAD_DIR[i];
            lods[i] = CalculateLOD(v_.QuickDistance(LoadedChunksCentre));
        }
        return lods;
    }

    /// <summary>
    /// Checks the <see cref="LoadedChunks"/> dictionary for the relevent chunk position
    /// Returns null if not present
    /// </summary>
    /// <param name="chunk">The coordinate of the chunk we wish to find</param>
    /// <returns></returns>
    public LoadedChunk2 GetLoadedChunk(Vec2i chunk)
    {
        LoadedChunk2 lChunk;
        if (InSubworld)
        {

            if (SubworldChunks.TryGetValue(chunk, out lChunk))
                return lChunk;
        }
        if(LoadedChunks.TryGetValue(chunk, out lChunk))
        {
            return lChunk;
        }
        return null;

    }
    /// <summary>
    /// Returns the 3 neighbors for this chunk as an array
    /// <list type="bullet">
    ///     <item>ChunkData[0] = Chunk[x    , z + 1]</item>
    ///     <item>ChunkData[1] = Chunk[x + 1, z + 1]</item>
    ///     <item>ChunkData[2] = Chunk[x + 1, z    ]</item>
    /// </list>
    /// Checks if a subworld is currently loaded. If it is, then we 
    /// find chunks by accessing <see cref="CurrentSubworld.GetChunkSafe"/>
    /// </summary>
    /// <param name="c">Position at which to find neigbors</param>
    /// <returns></returns>
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
            return new ChunkData[] { GetChunk(c.x, c.z+1, true), GetChunk(c.x+1, c.z + 1, true),
                                      GetChunk(c.x+1, c.z, true),/* GetChunk(c.x+1, c.z - 1, false),
                                      GetChunk(c.x, c.z-1, false), GetChunk(c.x-1, c.z-1, false)  ,
                                      GetChunk(c.x-1,c.z, false), GetChunk(c.x-1, c.z+1 , false)*/};
        }

        else
        {
            return null;
        }

    }
    #endregion
}