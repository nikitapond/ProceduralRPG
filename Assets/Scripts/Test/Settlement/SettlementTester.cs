using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MarchingCubesProject;
public class SettlementTester : MonoBehaviour
{

    public GameObject ChunkPrefab;
    public GameObject ChunkVoxelPrefab;


    public GameObject ChunkHolder;

    private MarchingCubes MarchingCubes;

    ChunkData[,] Chunks;


    private void Awake()
    {
        GameManager.RNG = new GenerationRandom(0);
        CurrentVerticies = new List<Vector3>(World.ChunkSize * World.ChunkSize);
        CurrentTriangles = new List<int>(World.ChunkSize * World.ChunkSize);
        CurrentUVs = new List<Vector2>(World.ChunkSize * World.ChunkSize);
        CurrentColours = new List<Color>(World.ChunkSize * World.ChunkSize);
        MarchingCubes = new MarchingCubes(-0.5f);

        ResourceManager.LoadAllResources();

        EventManager em = new EventManager();
        //GetComponentInChildren<EntityManager>().st
    }



    public float WorldHeightChunk(float x, float z)
    {


        float c = 5 + 1 * Mathf.PerlinNoise(x * 0.3f, z * 0.3f) * 20;

        //float c = 5 + (World.ChunkHeight - 5) * (1 - Mathf.Pow(Mathf.PerlinNoise(x * 0.01f, z * 0.01f), 2)); 
        //float radialScale = ((x - World.WorldSize / 2) * (x - World.WorldSize / 2) + (z - World.WorldSize / 2) * (z - World.WorldSize / 2))/ WorldRad;
        //c *= (1-Mathf.Clamp(radialScale, 0, 1));

        

        return c;
    }


    public float WorldHeight(int x, int z)
    {
        int cx = Mathf.FloorToInt((float)x / World.ChunkSize);
        int cz = Mathf.FloorToInt((float)z / World.ChunkSize);
        float px = ((float)(x % World.ChunkSize)) / World.ChunkSize;
        float pz = ((float)(z % World.ChunkSize)) / World.ChunkSize;
        return WorldHeightChunk(cx + px, cz + pz);
    }


    // Start is called before the first frame update
    void Start()
    {

        int seed = 0;
        SettlementBuilder setB = new SettlementBuilder(WorldHeight, new SettlementBase(new Vec2i(9, 9), 8, SettlementType.CAPITAL));
        setB.Generate(GameManager.RNG);

        Kingdom k = new Kingdom("king", new Vec2i(9, 9));
        Settlement set = new Settlement(k, "set", setB);
        k.AddSettlement(set);
        KingdomNPCGenerator npcGen = new KingdomNPCGenerator(null, k, EntityManager.Instance);
        npcGen.GenerateSettlementNPC(set);



        List<ChunkData> chunks = setB.ToChunkData();
        Chunks = new ChunkData[20, 20];

        foreach(ChunkData c in chunks)
        {
            Chunks[c.X, c.Z] = c;

            Debug.BeginDeepProfile("compress");
            c.VoxelData.Compress();
            Debug.EndDeepProfile("compress");


            Debug.BeginDeepProfile("de_compress");
            c.VoxelData.UnCompress();

            Debug.EndDeepProfile("de_compress");
        }
        foreach (ChunkData cd in Chunks)
        {
            if (cd == null)
                continue;
            PreLoadedChunk plc = GeneratePreLoadedChunk(cd);

            CreateChunk(plc, cd);
            EntityManager.Instance.LoadChunk(new Vec2i(cd.X, cd.Z));


        }

        Player player = new Player();
        PlayerManager.Instance.SetPlayer(player);


        
    }

    // Update is called once per frame
    void Update()
    {
        
    }



    public Vector2[] CreateUV(PreMesh pmesh, Vector2 offset, float scaleFactor)
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
            UV[pmesh.Triangles[index]] = (Vector2)(rotation * v1) * scaleFactor + offset;
            UV[pmesh.Triangles[index + 1]] = (Vector2)(rotation * v2) * scaleFactor + offset;
            UV[pmesh.Triangles[index + 2]] = (Vector2)(rotation * v3) * scaleFactor + offset;

        }
        return UV;
    }
    public Vector2[] CreateUV(PreMesh pmesh)
    {
        return CreateUV(pmesh, Vector2.zero, 0.5f);


    }


    private List<Vector3> CurrentVerticies;
    private List<int> CurrentTriangles;
    private List<Vector2> CurrentUVs;
    private List<Color> CurrentColours;
    private ChunkData GetChunk(int x, int z)
    {
        if (x > 0 && z > 0 && x < Chunks.GetLength(0) && z < Chunks.GetLength(1))
            return Chunks[x, z];
        return null;
    }
    private ChunkData[] GetNeighbors(Vec2i v)
    {
        return new ChunkData[] { GetChunk(v.x, v.z + 1), GetChunk(v.x + 1, v.z + 1), GetChunk(v.x + 1, v.z) };
    }
    /// <summary>
    /// Takes a 'preloadedchunk' and forms a loaded chunk 
    /// This must be called from the main thread.
    /// </summary>
    /// <param name="pChunk"></param>
    /// <returns></returns>
    private LoadedChunk2 CreateChunk(PreLoadedChunk pChunk, ChunkData cd)
    {
        GameObject cObj = Instantiate(ChunkPrefab);
        cObj.transform.parent = ChunkHolder.transform;
        cObj.transform.position = pChunk.Position.AsVector3() * World.ChunkSize;

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
            if (pChunk.VoxelMesh.TryGetValue(v, out PreMesh pmesh))
            {
                GameObject voxelObj = Instantiate(ChunkVoxelPrefab);
                MeshFilter voxelMf = voxelObj.GetComponent<MeshFilter>();
                voxelMf.mesh = PreLoadedChunk.CreateMesh(pmesh);
                Material voxMat = ResourceManager.GetVoxelMaterial(v);
                //Debug.Log(voxMat + " vox mat");
                voxelObj.GetComponent<MeshRenderer>().material = voxMat;
                MeshCollider voxelMc = voxelObj.GetComponent<MeshCollider>();
                voxelMc.sharedMesh = voxelMf.mesh;
                voxelObj.transform.parent = cObj.transform;
                voxelObj.transform.localPosition = Vector3.zero;

                voxelMf.mesh.RecalculateNormals();
            }
        }
        if (cd.WorldObjects != null)
        {
            foreach (WorldObjectData objDat in cd.WorldObjects)
            {
                WorldObject obj = WorldObject.CreateWorldObject(objDat, cObj.transform, 0.7f);
                //if(objDat.AutoHeight)
                //    obj.AdjustHeight();
                //obj.transform.position = objDat.Position;
                //obj.transform.localPosition = objDat.Position.Mod(World.ChunkSize);
            }
        }

        loaded.SetChunk(pChunk.ChunkData);
        return loaded;
    }



    /// <summary>
    /// Creates a pre loaded chunk from chunk data.
    /// This entire function is run in a thread
    /// </summary>
    /// <param name="cData"></param>
    /// <returns></returns>
    private PreLoadedChunk GeneratePreLoadedChunk(ChunkData chunk, int lod = 1)
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



        foreach (Voxel v in MiscUtils.GetValues<Voxel>())
        {
            if (v == Voxel.none)
                continue;
            //If the chunk has this type of voxel in it
            if (chunk.VoxelData.VoxelTypeBounds.TryGetValue(v, out VoxelBounds vb))
            {

                //Clear all lists to prepair
                CurrentVerticies.Clear();
                CurrentTriangles.Clear();
                CurrentColours.Clear();
                CurrentUVs.Clear();
                //Generate the voxel mesh
                //MarchingCubes.Generate(chunk.VoxelData.Voxels, vb, v, World.ChunkSize+1, World.ChunkHeight+1, World.ChunkSize+1, CurrentVerticies, CurrentTriangles);
                //MarchingCubes.Generate(chunk.VoxelData.Voxels, vb, v, World.ChunkSize + 1, chunk.VoxelData.TotalHeight(), World.ChunkSize + 1, CurrentVerticies, CurrentTriangles);



                MarchingCubes.Generate(chunk.VoxelData.AllVoxels, vb, v, World.ChunkSize + 1, chunk.VoxelData.TotalHeight(), World.ChunkSize + 1, CurrentVerticies, CurrentTriangles);
                PreMesh voxelMesh = new PreMesh();
                voxelMesh.Verticies = CurrentVerticies.ToArray();
                voxelMesh.Triangles = CurrentTriangles.ToArray();
                voxelMesh.UV = CreateUV(voxelMesh);
                //Add it the the pre loaded chunk
                preChunk.VoxelMesh.Add(v, voxelMesh);
            }

        }
        if (chunk.WorldObjects != null)
        {

            /*lock (ObjectsToLoadLock)
            {
                Debug.Log("[ChunkLoader] Chunk " + chunk.X + "," + chunk.Z + " has " + chunk.WorldObjects.Count + " objects to load", Debug.CHUNK_LOADING);

                ObjectsToLoad.AddRange(chunk.WorldObjects);
            }*/

        }



        return preChunk;
    }


    private void ClearBuffers()
    {
        CurrentVerticies.Clear();
        CurrentUVs.Clear();
        CurrentTriangles.Clear();
        CurrentColours.Clear();
    }


    private PreMesh GenerateSmoothTerrain(ChunkData chunk, int LOD = 1)
    {

        int size = World.ChunkSize / LOD;
        Color[,] colourMap = new Color[size + 1, size + 1];
        ClearBuffers();
        ChunkData[] neighbors = GetNeighbors(new Vec2i(chunk.X, chunk.Z));

        for (int x = 0; x <= size; x++)
        {
            for (int z = 0; z <= size; z++)
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
                        colourMap[x, z] = chunk.GetTile((x - 1) * LOD, (z - 1) * LOD).GetColor();
                    }

                }
                else if (x == size)
                {
                    if (neighbors != null && neighbors[2] != null)
                    {

                        height = neighbors[2].BaseHeight;
                        if (neighbors[2].Heights != null)
                            height = neighbors[2].Heights[0, z * LOD];

                        colourMap[x, z] = neighbors[2].GetTile(0, z * LOD).GetColor();

                    }
                    else
                    {
                        height = chunk.Heights != null ? chunk.Heights[(x - 1) * LOD, z * LOD] : height;
                        colourMap[x, z] = chunk.GetTile((x - 1) * LOD, z * LOD).GetColor();

                    }
                }
                else if (z == size)
                {
                    if (neighbors != null && neighbors[0] != null)
                    {
                        height = neighbors[0].BaseHeight;
                        if (neighbors[0].Heights != null)
                            height = neighbors[0].Heights[x * LOD, 0];

                        colourMap[x, z] = neighbors[0].GetTile(x * LOD, 0).GetColor();

                    }
                    else
                    {

                        // height = chunk.Heights != null ? chunk.Heights[x, z - 1] : height;
                        colourMap[x, z] = chunk.GetTile(x * LOD, (z - 1) * LOD).GetColor();

                    }
                }
                else
                {
                    if (chunk.Heights != null)
                    {
                        height = chunk.Heights[x * LOD, z * LOD];
                    }
                    colourMap[x, z] = chunk.GetTile(x * LOD, z * LOD).GetColor();
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

        for (int z = 0; z < size; z++)
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
}
