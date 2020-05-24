using UnityEngine;
using UnityEditor;
using System.Threading;
using System.Collections.Generic;
using MarchingCubesProject;
public class ChunkLoader : MonoBehaviour
{
    public GameObject ChunkPrefab;
    public GameObject ChunkVoxelPrefab;

    private Object ChunkToLoadLock;
    private Object PreLoadedChunkLock;
    private Object ObjectsToLoadLock;
    private Thread MainThread;

    private volatile bool ForceLoad;

    private MarchingCubes MarchingCubes;

    private List<Vec2i> ChunksToLoadPositions;
    private List<ChunkData> ChunksToLoad;
    private List<PreLoadedChunk> PreLoadedChunks;
    private List<WorldObjectData> ObjectsToLoad;


    private List<Vector3> CurrentVerticies;
    private List<int> CurrentTriangles;
    private List<Vector2> CurrentUVs;
    private List<Color> CurrentColours;

    private ChunkRegionManager ChunkRegionManager;

    private void Awake()
    {
        ChunkToLoadLock = new Object();
        ChunksToLoad = new List<ChunkData>(60);
        ChunksToLoadPositions = new List<Vec2i>(60);

        PreLoadedChunkLock = new Object();
        PreLoadedChunks = new List<PreLoadedChunk>(60);

        ObjectsToLoadLock = new Object();
        ObjectsToLoad = new List<WorldObjectData>();

        CurrentVerticies = new List<Vector3>(World.ChunkSize * World.ChunkSize * 6);
        CurrentTriangles = new List<int>(World.ChunkSize * World.ChunkSize * 6);
        CurrentUVs = new List<Vector2>(World.ChunkSize * World.ChunkSize * 6);
        CurrentColours = new List<Color>(World.ChunkSize * World.ChunkSize * 6);

        ChunkRegionManager = GetComponent<ChunkRegionManager>();

        MarchingCubes = new MarchingCubes(-0.5f);
        ForceLoad = false;
    }

    /// <summary>
    /// Calling this function forces the chunk loader to load all chunks currently in its list.
    /// This must be called from the main thread.
    /// </summary>
    public void ForceLoadAll()
    {
        //We set the force load variable to true.
        //this forces the chunk loader thread to stop when it can
        ForceLoad = true;
        //Wait for the thread to finish
        MainThread?.Join();

        Debug.BeginDeepProfile("force_chunk_load");
        //All threads have stopped now, so we are not required to be thread safe.
        //TODO - add some thread generation here to improve performance?
        Debug.Log("[ChunkLoader] Force load starting - " + ChunksToLoad.Count + " chunks to load", Debug.CHUNK_LOADING);
        foreach(ChunkData cd in ChunksToLoad)
        {
            PreLoadedChunks.Add(GeneratePreLoadedChunk(cd));
        }
        ChunksToLoad.Clear();
        Debug.Log("[ChunkLoader] Force load - " + PreLoadedChunks.Count + " chunks to create", Debug.CHUNK_LOADING);

        foreach (PreLoadedChunk plc in PreLoadedChunks)
        {
            //If for some reason this has already been generated, don't bother
            if (ChunkRegionManager.LoadedChunks.ContainsKey(plc.Position))
            {
                Debug.Log("[ChunkLoader] Loaded chunk at position " + plc.Position + " has already generated", Debug.CHUNK_LOADING);
                continue;
            }
                
            LoadedChunk2 lc = CreateChunk(plc);
            ChunkRegionManager.LoadedChunks.Add(lc.Position, lc);
        }
        PreLoadedChunks.Clear();
        ChunksToLoadPositions.Clear();
        ForceLoad = false;
        Debug.EndDeepProfile("force_chunk_load");

    }

    /// <summary>
    /// Main update loop.
    /// We check if any pre-loaded chunks are ready.
    /// if so, we create a new loaded mesh as required.
    /// </summary>
    void Update()
    {
        PreLoadedChunk toCreate=null;
        //Attempt to get a pre-loaded chunk so we can add it to the world.
        lock (PreLoadedChunkLock)
        {
            if(PreLoadedChunks.Count != 0)
            {
                toCreate = PreLoadedChunks[0];
                PreLoadedChunks.RemoveAt(0);
            }
        }

        if(toCreate != null)
        {
            LoadedChunk2 lc = CreateChunk(toCreate);
            ChunkRegionManager.LoadedChunks.Add(lc.Position, lc);
            //After this chunk has finished loading, we remove its position from the list of currently being generated chunks
            lock (ChunkToLoadLock)
            {
                ChunksToLoadPositions.Remove(lc.Position);
            }
        }

        if(ObjectsToLoad.Count > 0)
        {
            Debug.Log("[ChunkLoader] " + ObjectsToLoad.Count + " objects to load in");
            LoadSingleObject();
        }

    }


    private void LoadSingleObject(int count=0)
    {
        if (count > 10)
            return;

        WorldObjectData objData = null;


        lock (ObjectsToLoadLock) {
            objData = ObjectsToLoad[0];
            ObjectsToLoad.RemoveAt(0);
        }
        //Find the chunk for the first object to generate
        Vec2i chunk = World.GetChunkPosition(objData.Position);
        LoadedChunk2 lc2 = ChunkRegionManager.GetLoadedChunk(chunk);
        if (lc2 == null)
        {
            Debug.Log(chunk);
            Debug.Log("[ChunkLoader] Chunk for object is not loaded, attempting another object");
            //if the chunk is null, we throw this object to the end of the que and try again.  
            lock (ObjectsToLoadLock)
            {
                ObjectsToLoad.Add(objData);
            }
            LoadSingleObject(count+1);
            return;
        }
        Vec2i localPos = Vec2i.FromVector3(objData.Position.Mod(World.ChunkSize));
        float off = lc2.Chunk.GetHeight(localPos);
        WorldObject obj = WorldObject.CreateWorldObject(objData, lc2.transform, off + 0.7f);
        Debug.Log("Created object " + obj);
        obj.AdjustHeight();
    }

    /// <summary>
    /// Takes a 'preloadedchunk' and forms a loaded chunk 
    /// This must be called from the main thread.
    /// </summary>
    /// <param name="pChunk"></param>
    /// <returns></returns>
    private LoadedChunk2 CreateChunk(PreLoadedChunk pChunk)
    {
        GameObject cObj = Instantiate(ChunkPrefab);
        cObj.transform.parent = transform;

        cObj.name = "chunk_" + pChunk.Position;
        LoadedChunk2 loaded = cObj.GetComponent<LoadedChunk2>();

        MeshFilter mf = loaded.GetComponent<MeshFilter>();
        //Create the terrain mesh
        mf.mesh = PreLoadedChunk.CreateMesh(pChunk.TerrainMesh);
        mf.mesh.RecalculateNormals();
        MeshCollider mc = loaded.GetComponent<MeshCollider>();
        mc.sharedMesh = mf.mesh;
        //Iterate all voxels
        foreach (Voxel v in MiscUtils.GetValues<Voxel>())
        {
            if (v == Voxel.none)
                continue;
            if(pChunk.VoxelMesh.TryGetValue(v, out PreMesh pmesh))
            {
                GameObject voxelObj = Instantiate(ChunkVoxelPrefab);
                MeshFilter voxelMf = voxelObj.GetComponent<MeshFilter>();
                MeshRenderer voxelMr = voxelObj.GetComponent<MeshRenderer>();
                voxelMr.material = ResourceManager.GetVoxelMaterial(v);
                voxelMf.mesh = PreLoadedChunk.CreateMesh(pmesh);
                MeshCollider voxelMc = voxelObj.GetComponent<MeshCollider>();
                voxelMc.sharedMesh = voxelMf.mesh;
                voxelObj.transform.parent = cObj.transform;
                //voxelObj.transform.localPosition = Vector3.up * (pChunk.ChunkData.BaseHeight+0.7f);

                voxelMf.mesh.RecalculateNormals();
            }
        }

        cObj.transform.position = pChunk.Position.AsVector3() * World.ChunkSize;
        loaded.SetChunk(pChunk.ChunkData);
        return loaded;
    }


    /// <summary>
    /// Returns a list containing all chunks that are in the process of being loaded.
    /// </summary>
    /// <returns></returns>
    public List<Vec2i> GetCurrentlyLoadingChunks()
    {
        List<Vec2i> list;
        lock (ChunkToLoadLock)
        {
            list = new List<Vec2i>(ChunksToLoadPositions);
        }
        return list;
    }

    /// <summary>
    /// Adds the specified chunk to the list of chunks to be
    /// generated
    /// </summary>
    /// <param name="chunk"></param>
    public void LoadChunk(ChunkData chunk)
    {
        Vec2i position = new Vec2i(chunk.X, chunk.Z);
        Debug.Log("[ChunkLoader] Adding chunk " + position + " to chunk loader", Debug.CHUNK_LOADING);

        //Stay thread safe - add object 
        lock (ChunkToLoadLock)
        {
            ChunksToLoadPositions.Add(position);
            ChunksToLoad.Add(chunk);
            
        }
        if (MainThread == null || !MainThread.IsAlive)
        {
            //We start/restart the internal load loop
            MainThread = new Thread(() => InternalThreadLoop());
            Debug.Log("[ChunkLoader] Chunk Loader thread starting", Debug.CHUNK_LOADING);
            MainThread.Start();
        }
    }

    /// <summary>
    /// Safely removes a chunk that has been added to the load list.
    /// Returns true if ChunksToLoad contains the chunk
    /// returns false if not
    /// </summary>
    /// <param name="chunk"></param>
    public bool RemoveChunk(ChunkData chunk)
    {
        //Lock for thread safety
        lock (ChunkToLoadLock)
        {
            //Check if we have yet to generate
            if (ChunksToLoad.Contains(chunk))
            {
                ChunksToLoad.Remove(chunk);
                return true;
            }            

        }
        return false;
    }

    /// <summary>
    /// Called to get the main thread to start generating chunks.
    /// </summary>
    private void InternalThreadLoop()
    {
        bool shouldLive = true;
        ChunkData toLoad;
        Vec2i position;
        //Form loop that lasts while we generate all chunks
        while (shouldLive)
        {
            UnityEngine.Profiling.Profiler.BeginSample("chunk_pre_load");
            Debug.BeginDeepProfile("chunk_pre_load");
            //thread safety
            lock (ChunkToLoadLock)
            {
                //If the count is 0, then the thread has finished for now.
                if (ChunksToLoad.Count == 0)
                    return;
                //otherwise, we set the object
                toLoad = ChunksToLoad[0];
                ChunksToLoad.RemoveAt(0);
                position = new Vec2i(toLoad.X, toLoad.Z);
            }
            Debug.Log("[ChunkLoader] Chunk Loader starting to generate chunk " + toLoad, Debug.CHUNK_LOADING);

            //Create pre-generated chunk
            PreLoadedChunk preLoaded = GeneratePreLoadedChunk(toLoad);
            Debug.Log("[ChunkLoader] Finished creating PreChunk: " + toLoad, Debug.CHUNK_LOADING);

            //Thread safe add the preloaded chunk
            lock (PreLoadedChunkLock)
            {
                PreLoadedChunks.Add(preLoaded);
            }

    
            //Add all objects to be loaded.
            lock (ObjectsToLoadLock)
            {
                if(toLoad.WorldObjects != null)
                {
                    ObjectsToLoad.AddRange(toLoad.WorldObjects);
                    Debug.Log("Chunk " + toLoad.X + "," + toLoad.Z + " has " + toLoad.WorldObjects.Count + " objects");
                    foreach(WorldObjectData objDat in toLoad.WorldObjects)
                    {
                        Debug.Log(objDat);
                    }
                }
            }
            Debug.EndDeepProfile("chunk_pre_load");
            UnityEngine.Profiling.Profiler.EndSample();

            //If we have requested a force load, we exit the thread.
            if (ForceLoad)
            {
                Debug.Log("[ChunkLoader] Force load is starting, need to finish loading chunk " + new Vec2i(toLoad.X, toLoad.Z), Debug.CHUNK_LOADING);
                return;
            }
                
        }    
    }

    private void ClearBuffers()
    {
        CurrentVerticies.Clear();
        CurrentUVs.Clear();
        CurrentTriangles.Clear();
        CurrentColours.Clear();
    }

    private PreMesh GenerateMarchingCubesTerrain(ChunkData chunk)
    {
        ChunkData[] neighbors = ChunkRegionManager.GetNeighbors(new Vec2i(chunk.X, chunk.Z));


        float[] cube = new float[(World.ChunkSize + 1) * (World.ChunkSize + 1) * (World.ChunkHeight + 1)];
        Color[,] colourMap = new Color[World.ChunkSize + 1, World.ChunkSize + 1];

        //We iterate through the whole chunk, and create a cub map and colour map based on the
        //height map and tile map
        for (int x = 0; x < World.ChunkSize + 1; x++)
        {
            for (int z = 0; z < World.ChunkSize + 1; z++)
            {

                float height = chunk.BaseHeight;
                if (x == World.ChunkSize && z == World.ChunkSize)
                {
                    if (neighbors != null && neighbors[1] != null)
                    {
                        height = neighbors[1].BaseHeight;
                        if (neighbors[1].Heights != null)
                            height = neighbors[1].Heights[0, 0];
                        colourMap[x, z] = neighbors[1].GetTile(0, 0).GetColor();
                    }

                    else
                    {
                        height = chunk.Heights != null ? chunk.Heights[x - 1, z - 1] : height;
                        colourMap[x, z] = chunk.GetTile(x - 1, z - 1).GetColor();
                    }

                }
                else if (x == World.ChunkSize)
                {
                    if (neighbors != null && neighbors[2] != null)
                    {
                        height = neighbors[2].BaseHeight;
                        if (neighbors[2].Heights != null)
                            height = neighbors[2].Heights[0, z];

                        colourMap[x, z] = neighbors[2].GetTile(0, z).GetColor();

                    }
                    else
                    {
                        height = chunk.Heights != null ? chunk.Heights[x - 1, z] : height;
                        colourMap[x, z] = chunk.GetTile(x - 1, z).GetColor();

                    }
                }
                else if (z == World.ChunkSize)
                {
                    if (neighbors != null && neighbors[0] != null)
                    {
                        height = neighbors[0].BaseHeight;
                        if (neighbors[0].Heights != null)
                            height = neighbors[0].Heights[x, 0];

                        colourMap[x, z] = neighbors[0].GetTile(x, 0).GetColor();

                    }
                    else
                    {

                        height = chunk.Heights != null ? chunk.Heights[x, z - 1] : height;
                        colourMap[x, z] = chunk.GetTile(x, z - 1).GetColor();

                    }
                }
                else
                {
                    if (chunk.Heights != null)
                    {
                        height = chunk.Heights[x, z];
                    }
                    colourMap[x, z] = chunk.GetTile(x, z).GetColor();
                }
                for (int y = 0; y < height + 1; y++)
                {
                    int idx = x + y * (World.ChunkSize + 1) + z * (World.ChunkHeight + 1) * (World.ChunkSize + 1);

                    cube[idx] = -2;
                }
            }
        }

        CurrentVerticies.Clear();
        CurrentTriangles.Clear();
        CurrentColours.Clear();
        //March the terrain map
        MarchingCubes.Generate(cube, World.ChunkSize + 1, World.ChunkHeight + 1, World.ChunkSize + 1, CurrentVerticies, CurrentTriangles);

        //We iterate each triangle
        for (int i = 0; i < CurrentTriangles.Count; i += 3)
        {
            int tri1 = CurrentTriangles[i];
            int tri2 = CurrentTriangles[i + 1];
            int tri3 = CurrentTriangles[i + 2];

            //We find the average/mid point of this triangle
            Vector3 mid = (CurrentVerticies[tri1] + CurrentVerticies[tri2] + CurrentVerticies[tri3]) / 3;
            Vec2i tMid = Vec2i.FromVector3(mid);
            Color c = colourMap[tMid.x, tMid.z];

            CurrentColours.Add(c);
            CurrentColours.Add(c);
            CurrentColours.Add(c);
        }
        /*
        for (int i = 0; i < CurrentVerticies.Count; i++)
        {
            int x = (int)CurrentVerticies[i].x;
            int z = (int)CurrentVerticies[i].z;
            CurrentColours.Add(colourMap[x, z]);
            
        }*/

        //We create a thread safe mesh for the terrain
        PreMesh terrainMesh = new PreMesh();
        terrainMesh.Verticies = CurrentVerticies.ToArray();
        terrainMesh.Triangles = CurrentTriangles.ToArray();
        terrainMesh.Colours = CurrentColours.ToArray();

        return terrainMesh;
    }

    private PreMesh GenerateSmoothTerrain(ChunkData chunk, int LOD = 1) {

        int size = World.ChunkSize / LOD;
        Color[,] colourMap = new Color[size + 1, size + 1];
        ClearBuffers();
        ChunkData[] neighbors = ChunkRegionManager.GetNeighbors(new Vec2i(chunk.X, chunk.Z));

        for (int x=0; x<=size; x++)
        {
            for(int z=0; z <= size; z++)
            {
                float height = 0;
               


                if (x == size && z == size)
                {
                    if (neighbors != null && neighbors[1] != null)
                    {
                        height = neighbors[1].BaseHeight;
                        if (neighbors[1].Heights != null)
                            height = neighbors[1].Heights[0, 0];
                        colourMap[x, z] = neighbors[1].GetTile(0, 0).GetColor();
                    }

                    else
                    {
                       //height = chunk.Heights != null ? chunk.Heights[x - 1, z - 1] : height;
                        colourMap[x, z] = chunk.GetTile((x - 1)*LOD, (z - 1)*LOD).GetColor();
                    }

                }
                else if (x == size)
                {
                    if (neighbors != null && neighbors[2] != null)
                    {
                        
                        height = neighbors[2].BaseHeight;
                        if (neighbors[2].Heights != null)
                            height = neighbors[2].Heights[0, z*LOD];
                            
                        colourMap[x, z] = neighbors[2].GetTile(0, z*LOD).GetColor();

                    }
                    else
                    {
                        height = chunk.Heights != null ? chunk.Heights[(x - 1)*LOD, z*LOD] : height;
                        colourMap[x, z] = chunk.GetTile((x - 1)*LOD, z*LOD).GetColor();

                    }
                }
                else if (z ==size)
                {
                    if (neighbors != null && neighbors[0] != null)
                    {
                        height = neighbors[0].BaseHeight;
                        if (neighbors[0].Heights != null)
                            height = neighbors[0].Heights[x*LOD, 0];

                        colourMap[x, z] = neighbors[0].GetTile(x*LOD, 0).GetColor();

                    }
                    else
                    {

                       // height = chunk.Heights != null ? chunk.Heights[x, z - 1] : height;
                        colourMap[x, z] = chunk.GetTile(x*LOD, (z - 1)*LOD).GetColor();

                    }
                }
                else
                {
                    if (chunk.Heights != null)
                    {
                        height = chunk.Heights[x*LOD, z*LOD];
                    }
                    colourMap[x, z] = chunk.GetTile(x*LOD, z*LOD).GetColor();
                }
                CurrentColours.Add(colourMap[x, z]);
                CurrentVerticies.Add(new Vector3(x * LOD, height, z * LOD));
                /*
                for (int y = 0; y < height + 1; y++)
                {
                    int idx = x + y * (World.ChunkSize + 1) + z * (World.ChunkHeight + 1) * (World.ChunkSize + 1);

                    //cube[idx] = -2;
                }*/

            }
        }      
       

        int vert = 0;
        int tris = 0;

        for(int z=0; z<size; z++)
        {
            for (int x = 0; x < size; x++)
            {
                CurrentTriangles.Add(vert + 1);
                CurrentTriangles.Add(vert + size + 1);
                CurrentTriangles.Add(vert + 0);


                CurrentTriangles.Add(vert + size + 2);
                CurrentTriangles.Add(vert + size + 1);
                CurrentTriangles.Add(vert + 1);
                
                
                vert++;
                tris += 6;
            }
            vert++;
        }
        
        //We iterate each triangle
       /* for (int i = 0; i < CurrentTriangles.Count; i += 3)
        {
            int tri1 = CurrentTriangles[i];
            int tri2 = CurrentTriangles[i + 1];
            int tri3 = CurrentTriangles[i + 2];
            /*Vector3 vec1 = CurrentVerticies[i];
            Vector3 vec2 = CurrentVerticies[i+1];
            Vector3 vec3 = CurrentVerticies[i+2];

            //We find the average/mid point of this triangle
             Vector3 mid = (CurrentVerticies[tri1] + CurrentVerticies[tri2] + CurrentVerticies[tri3]) / 3;
            //Vector3 mid = (vec1 + vec2 + vec3) / 3;
            Vec2i tMid = Vec2i.FromVector3(mid);
            Color c = colourMap[tMid.x, tMid.z];

            CurrentColours.Add(c);
            CurrentColours.Add(c);
            CurrentColours.Add(c);
        }
        Debug.Log("Colours: " + CurrentColours.Count + " vert: " + CurrentVerticies.Count);*/

        PreMesh pmesh = new PreMesh();
        pmesh.Verticies = CurrentVerticies.ToArray();
        pmesh.Triangles = CurrentTriangles.ToArray();
        pmesh.Colours = CurrentColours.ToArray();
        return pmesh;
    }

    /// <summary>
    /// Creates a pre loaded chunk from chunk data.
    /// This entire function is run in a thread
    /// </summary>
    /// <param name="cData"></param>
    /// <returns></returns>
    private PreLoadedChunk GeneratePreLoadedChunk(ChunkData chunk, int lod=1)
    {


        //We create a thread safe mesh for the terrain
        // PreMesh terrainMesh = GenerateMarchingCubesTerrain(chunk);

        PreMesh terrainMesh = GenerateSmoothTerrain(chunk);

        Debug.Log("[ChunkLoader] Terrain mesh for " + chunk + " created - " + CurrentVerticies.Count + " verticies", Debug.CHUNK_LOADING);
        //Create the base pre-loaded chunk
        PreLoadedChunk preChunk = new PreLoadedChunk(new Vec2i(chunk.X, chunk.Z), terrainMesh, chunk);
        //if we have no voxel data, return just the terrain map
        if (chunk.VoxelData == null)
            return preChunk;

        

        foreach(Voxel v in MiscUtils.GetValues<Voxel>())
        {
            if (v == Voxel.none)
                continue;
            //If the chunk has this type of voxel in it
            if(chunk.VoxelData.VoxelTypeBounds.TryGetValue(v, out VoxelBounds vb)){

                //Clear all lists to prepair
                CurrentVerticies.Clear();
                CurrentTriangles.Clear();
                CurrentColours.Clear();
                CurrentUVs.Clear();
                //Generate the voxel mesh
                //MarchingCubes.Generate(chunk.VoxelData.Voxels, vb, v, World.ChunkSize+1, World.ChunkHeight+1, World.ChunkSize+1, CurrentVerticies, CurrentTriangles);
                //MarchingCubes.Generate(chunk.VoxelData.Voxels, vb, v, World.ChunkSize + 1, chunk.VoxelData.TotalHeight(), World.ChunkSize + 1, CurrentVerticies, CurrentTriangles);
                //MarchingCubes.Generate(chunk.VoxelData.AllVoxels, vb, v, World.ChunkSize + 1, chunk.VoxelData.TotalHeight(), World.ChunkSize + 1, CurrentVerticies, CurrentTriangles);

                MarchingCubes.Generate(chunk.VoxelData.AllVoxels, vb, v, World.ChunkSize + 1, chunk.VoxelData.TotalHeight(), World.ChunkSize + 1, CurrentVerticies, CurrentTriangles);


                PreMesh voxelMesh = new PreMesh();
                voxelMesh.Verticies = CurrentVerticies.ToArray();
                voxelMesh.Triangles = CurrentTriangles.ToArray();
                voxelMesh.UV = CreateUV(voxelMesh);
                //Add it the the pre loaded chunk
                preChunk.VoxelMesh.Add(v, voxelMesh);
            }

        }
        if(chunk.WorldObjects != null)
        {
            
            lock (ObjectsToLoadLock)
            {
                Debug.Log("[ChunkLoader] Chunk " + chunk.X + "," + chunk.Z + " has " + chunk.WorldObjects.Count + " objects to load", Debug.CHUNK_LOADING);
                ObjectsToLoad.AddRange(chunk.WorldObjects);
            }
           
        }



        return preChunk;
    }

    /// <summary>
    /// Generates the UV coordinates for a marching cube mesh
    /// </summary>
    /// <param name="pmesh"></param>
    /// <returns></returns>
    private Vector2[] CreateUV(PreMesh pmesh, float scaleFactor=0.5f)
    {
        Vector2[] UV = new Vector2[pmesh.Verticies.Length];
        for (int index = 0; index < pmesh.Triangles.Length; index += 3)
        {
            // Get the three vertices bounding this triangle.
            Vector3 v1 = pmesh.Verticies[pmesh.Triangles[index]];
            Vector3 v2 = pmesh.Verticies[pmesh.Triangles[index + 1]];
            Vector3 v3 = pmesh.Verticies[pmesh.Triangles[index + 2]];

            // Compute a vector perpendicular to the face.
            Vector3 normal = Vector3.Cross(v3 - v1, v2 - v1);

            // Form a rotation that points the z+ axis in this perpendicular direction.
            // Multiplying by the inverse will flatten the triangle into an xy plane.
            Quaternion rotation = Quaternion.Inverse(Quaternion.LookRotation(normal));

            // Assign the uvs, applying a scale factor to control the texture tiling.
            UV[pmesh.Triangles[index]] = (Vector2)(rotation * v1) * scaleFactor;
            UV[pmesh.Triangles[index + 1]] = (Vector2)(rotation * v2) * scaleFactor;
            UV[pmesh.Triangles[index + 2]] = (Vector2)(rotation * v3) * scaleFactor;

        }
        return UV;


    }
}

public struct LODBoundry
{
    public int ThisLOD;
    public int OtherLOD;
    public Vec2i Direction;
    public LODBoundry(int thisLOD, int otherLOD, Vec2i direction)
    {
        ThisLOD = thisLOD;
        OtherLOD = otherLOD;
        Direction = direction;
    }
}