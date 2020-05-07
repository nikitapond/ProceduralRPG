using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using MarchingCubesProject;
public class BuildingGenTest : MonoBehaviour
{

    AstarPath path;
    public GameObject ChunkPrefab;
    public GameObject ChunkVoxelPrefab;

    private List<Vector3> CurrentVerticies;
    private List<int> CurrentTriangles;
    private List<Vector2> CurrentUVs;
    private List<Color> CurrentColours;

    private MarchingCubes MarchingCubes;

    ChunkData2[,] Chunks;

    void Awake()
    {
        CurrentVerticies = new List<Vector3>(World.ChunkSize * World.ChunkSize);
        CurrentTriangles = new List<int>(World.ChunkSize * World.ChunkSize);
        CurrentUVs = new List<Vector2>(World.ChunkSize * World.ChunkSize);
        CurrentColours = new List<Color>(World.ChunkSize * World.ChunkSize);
        MarchingCubes = new MarchingCubes(-0.5f);

        ResourceManager.LoadAllResources();
        path = GetComponent<AstarPath>();

    }

    private void Start()
    {
        
        int Size = 8;
        int seed = System.DateTime.Now.Millisecond;
        Debug.Log(seed);
        GenerationRandom genRan = new GenerationRandom(seed);
   

        TestBuildingBuilder tbb = new TestBuildingBuilder(new Vec2i(0, 0), new Vec2i(Size, Size));


        for(int x=0; x<Size/2; x++)
        {
            for (int z = 0; z < Size / 2; z++)
            {
              //  if (x != 0 || z != 0)
                //    continue;
                int xP = x * World.ChunkSize*2;
                int zP = z * World.ChunkSize*2;
                Building b = BuildingGenerator.CreateBuilding(genRan, out BuildingVoxels vox, Building.HOUSE);
                tbb.AddBuilding(b, vox, new Vec2i(xP+1,zP+1));
            }
        }

      
/*
        Building b = BuildingGenerator.CreateBuilding(genRan, out BuildingVoxels vox, Building.HOUSE);
        Building b1 = BuildingGenerator.CreateBuilding(genRan, out BuildingVoxels vox1, Building.BLACKSMITH);

        Building b2 = BuildingGenerator.CreateBuilding(genRan, out BuildingVoxels vox2, Building.HOUSE);
        Building b3 = BuildingGenerator.CreateBuilding(genRan, out BuildingVoxels vox3, Building.HOUSE);
        Building b4 = BuildingGenerator.CreateBuilding(genRan, out BuildingVoxels vox4, Building.HOUSE);
        
        //tbb.AddBuilding(b, vox, new Vec2i(10, 10));
        tbb.AddBuilding(b1, vox1, new Vec2i(40, 40));
        tbb.AddBuilding(b2, vox2, new Vec2i(60, 60));
        tbb.AddBuilding(b3, vox3, new Vec2i(60, 40));
        tbb.AddBuilding(b4, vox4, new Vec2i(40, 60));
        */
        List<ChunkData2> chunks = tbb.ToChunkData();

        Chunks = new ChunkData2[Size, Size];

        foreach (ChunkData2 cd in chunks)
        {
            Chunks[cd.X, cd.Z] = cd;
        }

        foreach (ChunkData2 cd in Chunks)
        {
            PreLoadedChunk plc = GeneratePreLoadedChunk(cd);

            CreateChunk(plc, cd);
        }

        for(int x=0; x<20; x++)
        {
            //Debug.Log(tbb.GetVoxelNode(x,1,15);
        }






    }


    public Vector2[] CreateUV(PreMesh pmesh)
    {
        Vector2[] UV = new Vector2[pmesh.Verticies.Length];
        float scaleFactor = .4f;
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

    // Update is called once per frame
    void Update()
    {
        
    }


    private ChunkData2 GetChunk(int x, int z)
    {
        if (x > 0 && z > 0 && x < Chunks.GetLength(0) && z < Chunks.GetLength(1))
            return Chunks[x, z];
        return null;
    }
    private ChunkData2[] GetNeighbors(Vec2i v)
    {
        return new ChunkData2[] { GetChunk(v.x, v.z + 1), GetChunk(v.x + 1, v.z + 1), GetChunk(v.x + 1, v.z) };
    }
    /// <summary>
    /// Takes a 'preloadedchunk' and forms a loaded chunk 
    /// This must be called from the main thread.
    /// </summary>
    /// <param name="pChunk"></param>
    /// <returns></returns>
    private LoadedChunk2 CreateChunk(PreLoadedChunk pChunk, ChunkData2 cd)
    {
        GameObject cObj = Instantiate(ChunkPrefab);
        cObj.transform.parent = transform;
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
                voxelObj.transform.localPosition = Vector3.up * (pChunk.ChunkData.BaseHeight+0.75f);

                voxelMf.mesh.RecalculateNormals();
            }
        }
        if(cd.WorldObjects != null)
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

    private PreLoadedChunk GeneratePreLoadedChunk(ChunkData2 chunk)
    {
        //Null till we integrate fully
        //ChunkData2[] neighbors = null;

        ChunkData2[] neighbors = GetNeighbors(new Vec2i(chunk.X, chunk.Z));


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
        //Debug.Log("[ChunkLoader] Terrain mesh for " + chunk + " created - " + CurrentVerticies.Count + " verticies");
        //Create the base pre-loaded chunk
        PreLoadedChunk preChunk = new PreLoadedChunk(new Vec2i(chunk.X, chunk.Z), terrainMesh, chunk);

        Debug.Log("Pre loaded chunk started, now for voxels");

        //if we have no voxel data, return just the terrain map
        if (chunk.VoxelData == null)
        {
            Debug.Log("Chunk has no voxels");
            return preChunk;

        }



        foreach (Voxel v in MiscUtils.GetValues<Voxel>())
        {
            if (v == Voxel.none)
                continue;

            if(!chunk.VoxelData.HasVoxel(v))
            {
               // Debug.Log("Chunk " + chunk + " does not have the voxel " + v);
                continue;
            }

            CurrentVerticies.Clear();
            CurrentTriangles.Clear();
            CurrentColours.Clear();
            CurrentUVs.Clear();
           // Debug.Log("starting march");

            //Generate the voxel mesh
            MarchingCubes.Generate(chunk.VoxelData.Voxels, null, v, World.ChunkSize + 1, World.ChunkHeight + 1, World.ChunkSize + 1, CurrentVerticies, CurrentTriangles);
            PreMesh voxelMesh = new PreMesh();
            voxelMesh.Verticies = CurrentVerticies.ToArray();
            voxelMesh.Triangles = CurrentTriangles.ToArray();
            voxelMesh.UV = CreateUV(voxelMesh);
            //Add it the the pre loaded chunk
            preChunk.VoxelMesh.Add(v, voxelMesh);
            /*
            //If the chunk has this type of voxel in it
            if (chunk.VoxelData.VoxelTypeBounds.TryGetValue(v, out VoxelBounds vb))
            {
                //Clear all lists to prepair
                CurrentVerticies.Clear();
                CurrentTriangles.Clear();
                CurrentColours.Clear();
                CurrentUVs.Clear();
                Debug.Log("starting march");

                //Generate the voxel mesh
                MarchingCubes.Generate(chunk.VoxelData.Voxels, vb, v, World.ChunkSize+1, World.ChunkHeight+1, World.ChunkSize+1, CurrentVerticies, CurrentTriangles);
                PreMesh voxelMesh = new PreMesh();
                voxelMesh.Verticies = CurrentVerticies.ToArray();
                voxelMesh.Triangles = CurrentTriangles.ToArray();
                voxelMesh.UV = CreateUV(voxelMesh);
                //Add it the the pre loaded chunk
                preChunk.VoxelMesh.Add(v, voxelMesh);
            }*/

        }



        return preChunk;
    }
}
