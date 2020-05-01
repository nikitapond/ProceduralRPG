using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;

public class ChunkRegionManager : MonoBehaviour
{
    public static int LoadChunkRadius = 6;
    private Object ThreadSafe;
    public ChunkRegion[,] LoadedRegions { get; private set; }
    public Dictionary<Vec2i, LoadedChunk> LoadedChunks { get; private set; }

    private World World { get { return GameManager.WorldManager.World; } }
    private Player Player { get { return GameManager.PlayerManager.Player; } }
    public Vec2i LoadedChunksCentre { get; private set; }
    public Dictionary<Vec2i, LoadedChunk> SubworldChunks { get; private set; }
    private bool InSubworld;

    private List<ChunkData> ToLoadIn;
    private List<Vec2i> ToLoad;
    void Awake()
    {
        LoadedChunks = new Dictionary<Vec2i, LoadedChunk>();
        LoadedRegions = new ChunkRegion[World.RegionCount, World.RegionCount];

        SubworldChunks = new Dictionary<Vec2i, LoadedChunk>();
        ToLoadIn = new List<ChunkData>();
        ToLoad = new List<Vec2i>();
        InSubworld = false;
        ThreadSafe = new Object();
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
        if (playerChunk != LoadedChunksCentre)
        {
            LoadChunks(new Vec2i(playerChunk.x, playerChunk.z), LoadChunkRadius, forceLoad);

        }
        if (ToLoadIn.Count > 0)
        {
            ChunkLoadIn(1);
        }



    }


    public void LoadSubworldChunks(Subworld sub)
    {
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
        }
    }

    public void LeaveSubworld()
    {
        InSubworld = false;
        foreach (KeyValuePair<Vec2i, LoadedChunk> kvp in SubworldChunks)
        {
            Destroy(kvp.Value.gameObject);

        }
        SubworldChunks.Clear();
    }

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



    public ChunkData GetChunk(int x, int z)
    {
        return GetChunk(new Vec2i(x, z));
    }
    public ChunkData GetChunk(Vec2i v)
    {
        //Find region of chunk and check if valid within bounds
        Vec2i r = World.GetRegionCoordFromChunkCoord(v);
        if (r.x >= World.RegionCount || r.z >= World.RegionCount || r.x < 0 || r.z < 0)
        {
            return null;
        }
        ChunkRegion cr;
        lock (ThreadSafe)
        {
            cr = LoadedRegions[r.x, r.z];
        }
        //Get chunk region, check if it has been loaded
        if (cr == null)
        {
            //If it has not been laoded, then load it
            LoadRegion(r);

            if (cr == null)
                return null;
            lock (ThreadSafe)
            {
                cr = LoadedRegions[r.x, r.z];
            }
            
            GameManager.PathFinder.LoadRegion(cr);
        }
        ChunkData cDat;
        lock (ThreadSafe)
        {
            cDat = cr.Chunks[v.x % World.RegionSize, v.z % World.RegionSize]; 
        }
        return cDat;
    }
    public void LoadRegion(Vec2i rPos)
    {
        if (rPos.x >= World.RegionCount || rPos.z >= World.RegionCount || rPos.x < 0 || rPos.z < 0)
        {
            throw new System.Exception("Region " + rPos + " is not within world bounds");
        }
        //If valid, load and add to array
        LoadedRegions[rPos.x, rPos.z] = GameManager.LoadSave.LoadChunkRegion(rPos.x, rPos.z);
        if (LoadedRegions[rPos.x, rPos.z] == null || LoadedRegions[rPos.x, rPos.z].Generated == false)
        {
            LoadedRegions[rPos.x, rPos.z] = GameManager.ChunkRegionGenerator.ForceGenerateRegion(rPos);
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
        LoadedChunksCentre = middle;
        //Create lists to hold chunks to load and unload
        List<Vec2i> toLoad = new List<Vec2i>((LoadChunkRadius*2)* (LoadChunkRadius * 2));
        toLoad.AddRange(ToLoad);
        List<Vec2i> toUnload = new List<Vec2i>(100);
        //Iterate all loaded chunks and add to to unload
        foreach (KeyValuePair<Vec2i, LoadedChunk> kvp in LoadedChunks)
        {
            toUnload.Add(kvp.Key);
        }
        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                if (x + middle.x < 0 || x + middle.x >= World.WorldSize - 1 || z + middle.z < 0 || z + middle.z >= World.WorldSize - 1)
                    continue;
                Vec2i pos = new Vec2i(x + middle.x, z + middle.z);
                //If it is already loaed, remove it from the to remove
                if (toUnload.Contains(pos))
                    toUnload.Remove(pos);
                else if(!toLoad.Contains(pos))
                    toLoad.Add(pos);

            }
        }
        foreach (Vec2i v in toLoad)
        {
            if(LoadChunk(v, forceLoad))
            {
                ToLoad.Remove(v);
            }
        }
        List<LoadedChunk> toUnloadChunks = new List<LoadedChunk>(20);
        foreach (Vec2i v in toUnload)
        {
            LoadedChunk lc = LoadedChunks[v];
            LoadedChunks.Remove(v);
            Destroy(lc.gameObject);
            GameManager.EntityManager.UnloadChunk(v);
        }



    }

    public LoadedChunk GetLoadedChunk(Vec2i chunk)
    {
        return LoadedChunks[chunk];

    }

    public bool LoadChunk(Vec2i chunk, bool forceLoad = false)
    {
        //Check if chunk is loaded
        if (LoadedChunks.ContainsKey(chunk))
        {
            Debug.Log("Chunk " + chunk + " is already loaded!", Debug.CHUNK_LOADING);
            return true;
        }

        //Retrieve data    
        ChunkData data = GetChunk(chunk.x, chunk.z);

        if (data == null)
        {
            Debug.Log("Chunk " + chunk + " could not be found!", Debug.CHUNK_LOADING);
            return false;
        }
            

        if (forceLoad)
            LoadInSingleChunk(data);
        else if(!ToLoadIn.Contains(data))
        {
            Debug.Log("Added  " + chunk + " to load in", Debug.CHUNK_LOADING);
            ToLoadIn.Add(data);
        }
        return true;

    }

    private void ChunkLoadIn(int maxToGen)
    {
        if (ToLoadIn.Count == 0)
        {
            Debug.Log("none to load in");
            DebugGUI.Instance.ClearData("chunks_loading_in");
        }
        DebugGUI.Instance.SetData("chunks_loading_in", ToLoadIn.Count);
           
        if (ToLoadIn.Count < maxToGen)
        {
            for (int i = 0; i < ToLoadIn.Count; i++)
            {
                Debug.Log("Loading in " + ToLoadIn[i].X + "," + ToLoadIn[i].Z);
                LoadInSingleChunk(ToLoadIn[i]);
            }
            
            ToLoadIn.Clear();
        }
        else
        {

            for(int i=0; i<maxToGen; i++)
            {

                Debug.Log("Loading in " + ToLoadIn[i].X + "," + ToLoadIn[i].Z, Debug.CHUNK_LOADING);

                LoadInSingleChunk(ToLoadIn[i]);
                
            }
            ToLoadIn.RemoveRange(0, maxToGen);
        }

    }

    private void LoadInSingleChunk(ChunkData data)       
    {


        Debug.BeginDeepProfile("chunk_load");
        //Initiate chunk
        Vec2i chunk = new Vec2i(data.X, data.Z);

        if (LoadedChunks.ContainsKey(chunk))
            return;
        GameObject chunkObject = Instantiate(ResourceManager.ChunkPrefab);
        chunkObject.transform.parent = transform;
        chunkObject.name = "Chunk " + chunk;

        //LoadedChunk loadedChunk = chunkObject.AddComponent<LoadedChunk>();
        LoadedChunk loadedChunk = chunkObject.GetComponent<LoadedChunk>();

        ChunkData[] neigh = { GetChunk(chunk.x, chunk.z + 1), GetChunk(chunk.x + 1, chunk.z + 1), GetChunk(chunk.x + 1, chunk.z) };

        //ChunkData[] neigh = { null, null, null };


        loadedChunk.SetChunkData(data, neigh);

        LoadedChunks.Add(chunk, loadedChunk);

        GameManager.EntityManager.LoadChunk(World.ChunkBases[chunk.x, chunk.z], chunk);
        Debug.EndDeepProfile("chunk_load");
    }

    public void UnloadChunk(Vec2i chunk)
    {
        if (LoadedChunks.ContainsKey(chunk))
        {
            LoadedChunk loaded = LoadedChunks[chunk];
            LoadedChunks.Remove(chunk);
            GameManager.EntityManager.UnloadChunk(chunk);
            Destroy(loaded.gameObject);
        }
    }

    public void UnloadAllChunks()
    {
        List<Vec2i> chunkKeys = new List<Vec2i>();

        foreach (KeyValuePair<Vec2i, LoadedChunk> kpv in LoadedChunks)
        {
            chunkKeys.Add(kpv.Key);
            Destroy(kpv.Value.gameObject);
        }
        LoadedChunks.Clear();
        GameManager.EntityManager.UnloadChunks(chunkKeys);
    }
}