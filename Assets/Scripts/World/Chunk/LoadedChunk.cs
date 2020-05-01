using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
public class LoadedChunk : MonoBehaviour
{

    public static MarchingCubesProject.MarchingCubes MarchingCubes = new MarchingCubesProject.MarchingCubes(-0.5f);

    private static List<Vector3> VertexBuffer = new List<Vector3>(World.ChunkSize * World.ChunkSize * 9);
    private static List<int> TriBuffer = new List<int>(World.ChunkSize * World.ChunkSize * 9);
    private static List<Color> ColBuffer = new List<Color>(World.ChunkSize * World.ChunkSize * 9);

    public ChunkData Chunk { get; private set; }

    public WorldObject[,] LoadedWorldObjects { get; private set; }

    /// <summary>
    /// Called when component is added to object.
    /// Here we initiate some of the variables used for the loaded chunk, 
    /// such as adding the mesh and rendering stuff
    /// </summary>
    private void Awake()
    {
        /*gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>();
        gameObject.GetComponent<MeshRenderer>().material = ResourceManager.GetMaterial("Chunk");
        gameObject.AddComponent<MeshCollider>();*/
    }



    public void SetChunkData(ChunkData chunk, ChunkData[] neighbors, bool forceLoad=false)
    {
        Debug.BeginDeepProfile("set_data");




        //Debug.BeginProfile("set_data");
        Chunk = chunk;
        transform.position = new Vector3(Chunk.X * World.ChunkSize, 0, Chunk.Z * World.ChunkSize);
        LoadedWorldObjects = new WorldObject[World.ChunkSize, World.ChunkSize];

        Debug.BeginDeepProfile("chunk_create_mesh");

        float[] cube = new float[(World.ChunkSize+1) * (World.ChunkSize+1) * (World.ChunkHeight+1)];

        Color[,] colourMap = new Color[World.ChunkSize + 1, World.ChunkSize + 1];

        for (int x = 0; x < World.ChunkSize+1; x++)
        {
            for (int z = 0; z < World.ChunkSize+1; z++)
            {

                float height = chunk.BaseHeight;
                if (x == World.ChunkSize && z == World.ChunkSize)
                {
                    if (neighbors != null && neighbors[1] != null)
                    {
                        height = neighbors[1].BaseHeight;
                        if (neighbors[1].GetObject(0, 0) != null && neighbors[1].GetObject(0, 0).GroundHeight != -1)
                            height = neighbors[1].GetObject(0, 0).GroundHeight;
                        else if(neighbors[1].Heights != null)
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
                        if (neighbors[2].GetObject(0, z) != null && neighbors[2].GetObject(0, z).GroundHeight != -1)
                            height = neighbors[2].GetObject(0, z).GroundHeight;
                        else if(neighbors[2].Heights != null)
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
                        if (neighbors[0].GetObject(x, 0) != null && neighbors[0].GetObject(x, 0).GroundHeight != -1)
                            height = neighbors[0].GetObject(x, 0).GroundHeight;
                        else if(neighbors[0].Heights != null)
                            height = neighbors[0].Heights[x, 0];

                        colourMap[x, z] = neighbors[0].GetTile(x, 0).GetColor();

                    }
                    else { 

                        height = chunk.Heights != null ? chunk.Heights[x, z - 1] : height;
                        colourMap[x, z] = chunk.GetTile(x, z-1).GetColor();

                    }
                }
                else
                {
                    if(chunk.GetObject(x, z)!= null &&chunk.GetObject(x, z).GroundHeight != -1)
                    {
                        height = chunk.GetObject(x, z).GroundHeight;
                    }
                    else if (chunk.Heights != null)
                    {
                        height = chunk.Heights[x, z];
                    }
                    colourMap[x, z] = chunk.GetTile(x, z).GetColor();
                }
                for (int y = 0; y < height + 1; y++)
                {
                    int idx = x + y * (World.ChunkSize+1) + z * (World.ChunkHeight+1) * (World.ChunkSize+1);

                    cube[idx] = -2;
                }
            }
        }

        int waterDepth = -5;
        //List<Vector3> verticies = new List<Vector3>(World.ChunkSize*World.ChunkSize*4);
        //List<int> triangles = new List<int>(World.ChunkSize * World.ChunkSize * 4);
        //List<Color> colours = new List<Color>(World.ChunkSize * World.ChunkSize * 4);
        VertexBuffer.Clear();
        TriBuffer.Clear();
        ColBuffer.Clear();
        MarchingCubes.Generate(cube, World.ChunkSize+1, World.ChunkHeight+1, World.ChunkSize+1, VertexBuffer, TriBuffer);


        Debug.EndDeepProfile("chunk_create_mesh");
        Debug.BeginDeepProfile("chunk_set_mesh");
        MeshFilter meshFilt = GetComponent<MeshFilter>();

        meshFilt.mesh = new Mesh();
        meshFilt.mesh.vertices = VertexBuffer.ToArray();
        meshFilt.mesh.triangles = TriBuffer.ToArray();
        meshFilt.mesh.RecalculateNormals();
        
        for(int i=0; i< VertexBuffer.Count; i++)
        {
            int x = (int)VertexBuffer[i].x;
            int z = (int)VertexBuffer[i].z;
            ColBuffer.Add(colourMap[x, z]);
/*
            float dot = Vector3.Dot(meshFilt.mesh.normals[i], Vector3.up);
            if(dot > 0.5f)
            {
                ColBuffer.Add(colourMap[x, z]);
            }
            else
            {
                ColBuffer.Add(Color.grey);
            }*/
        }
        meshFilt.mesh.colors = ColBuffer.ToArray();
        


        GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        gameObject.GetComponent<MeshRenderer>().material = ResourceManager.GetMaterial("Chunk");
        
        Vector3[] colVerts = new Vector3[] {new Vector3(0,0,0), new Vector3(0,0,World.ChunkSize),
            new Vector3(World.ChunkSize, 0, World.ChunkSize), new Vector3(World.ChunkSize, 0, 0)};
        int[] colTris = new int[] { 0, 1, 2, 0, 2, 3 };
        Mesh colMesh = GetComponent<MeshCollider>().sharedMesh = new Mesh();


        colMesh.vertices = meshFilt.mesh.vertices;
        colMesh.triangles = meshFilt.mesh.triangles;


       // GetComponent<MeshCollider>().enabled = false;
        this.name = this.name + chunk.DEBUG;
        Debug.EndDeepProfile("chunk_set_mesh");

        Vec2i chunkWorldPos = new Vec2i(chunk.X, chunk.Z) * World.ChunkSize;
        if (Chunk.Objects == null)
        {
            Debug.EndDeepProfile("set_data");
            return;
        }

        Debug.BeginDeepProfile("chunk_set_obj");
        for (int x=0; x<World.ChunkSize; x++)
        {
            for(int z=0; z<World.ChunkSize; z++)
            {
                float height = chunk.Heights != null ? chunk.Heights[x, z]+1f : chunk.BaseHeight+1;
                WorldObjectData wObj = Chunk.GetObject(x, z);                    
                if(wObj != null)
                {                  
                    wObj.SetPosition(new Vec2i(chunk.X * World.ChunkSize + x, chunk.Z * World.ChunkSize + z));
                    //Debug.BeginDeepProfile("CreateWorldObj");
                    //objs.Add(wObj);
                    WorldObject loaded = wObj.CreateWorldObject(transform);
                    loaded.transform.position += Vector3.up* height;
                    LoadedWorldObjects[x, z] = loaded;
                }
            }
        }
        Debug.EndDeepProfile("chunk_set_obj");
        Debug.EndDeepProfile("set_data");

        //Debug.EndProfile();
    }


}
#region OLD_CHUNK_CODE
/*
int tri = 0;
bool hasWater = false;
///Iterate every point in chunk, define terrain mesh based on tiles.
#region mesh_gen
for (int z=0; z<World.ChunkSize; z++)
{
    for(int x=0; x<World.ChunkSize; x++)
    {
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        Tile t = Chunk.GetTile(x, z);
        Color c = t == null ? Tile.GRASS.GetColor() : t.GetColor();
        int y = 0;
        //Check if this tile is water, if so then height=-1
        if (t == Tile.WATER)
        {
            hasWater = true;
            y = waterDepth;
        }


        verticies.Add(new Vector3(x, y, z));
        colours.Add(c);
        //If we are within non-extremes of chunk
        if (x<World.ChunkSize-1 && z < World.ChunkSize - 1)
        {

            verticies.Add(new Vector3(x, chunk.GetTile(x, z + 1)==Tile.WATER? waterDepth : 0, z + 1));
            colours.Add(chunk.GetTile(x, z+1).GetColor());

            verticies.Add(new Vector3(x + 1, chunk.GetTile(x+1, z + 1) == Tile.WATER ? waterDepth : 0, z + 1));
            colours.Add(chunk.GetTile(x+1, z + 1).GetColor());

            verticies.Add(new Vector3(x + 1, chunk.GetTile(x + 1, z) == Tile.WATER ? waterDepth : 0, z));
            colours.Add(chunk.GetTile(x + 1, z).GetColor());

        }
        else
        {
            //We are at an extreme
            //Check x extreme, if we are not in x extreme then this must be Z extreme
            if(x < World.ChunkSize - 1)
            {
                //Z EXTREME

                Tile t1 = neighbors[0] != null ? neighbors[0].GetTile(x, 0) : null;
                Tile t2 = neighbors[0] != null ? neighbors[0].GetTile(x+1, 0) : null;
                Tile t3 = chunk.GetTile(x + 1, z);
                if (t1 == null)
                {
                    verticies.Add(new Vector3(x, y, z + 1));
                    colours.Add(c);
                }else
                {
                    if(t1 == Tile.WATER)
                        verticies.Add(new Vector3(x, waterDepth, z + 1));
                    else
                        verticies.Add(new Vector3(x, 0, z + 1));

                    colours.Add(t1.GetColor());
                }

                if (t2 == null)
                {
                    verticies.Add(new Vector3(x+1, y, z + 1));
                    colours.Add(c);
                }
                else
                {
                    if (t2 == Tile.WATER)
                        verticies.Add(new Vector3(x+1, waterDepth, z + 1));
                    else
                        verticies.Add(new Vector3(x+1, 0, z + 1));

                    colours.Add(t2.GetColor());
                }
                if(t3 == null)
                {
                    verticies.Add(new Vector3(x + 1, y, z));
                    colours.Add(c);
                }
                else
                {
                    if (t3 == Tile.WATER)
                        verticies.Add(new Vector3(x + 1, waterDepth, z));
                    else
                        verticies.Add(new Vector3(x + 1, 0, z));

                    colours.Add(t3.GetColor());
                }


            }
            else if(z < World.ChunkSize - 1)
            {
                //X EXTREME
                Tile t1 = chunk.GetTile(x, z+1);
                Tile t2 = neighbors[2] != null ? neighbors[2].GetTile(0, z+1) : null;
                Tile t3 = neighbors[2] != null ? neighbors[2].GetTile(0, z) : null;
                if (t1 == null)
                {
                    verticies.Add(new Vector3(x, y, z + 1));
                    colours.Add(c);
                }
                else
                {
                    if (t1 == Tile.WATER)
                        verticies.Add(new Vector3(x, waterDepth, z + 1));
                    else
                        verticies.Add(new Vector3(x, 0, z + 1));

                    colours.Add(t1.GetColor());
                    //colours.Add(Color.red);

                }

                if (t2 == null)
                {
                    verticies.Add(new Vector3(x + 1, y, z + 1));
                    colours.Add(c);
                }
                else
                {
                    if (t2 == Tile.WATER)
                        verticies.Add(new Vector3(x + 1, waterDepth, z + 1));
                    else
                        verticies.Add(new Vector3(x + 1, 0, z + 1));
                    //colours.Add(Color.magenta);
                    colours.Add(t2.GetColor());
                    //colours.Add(Color.yellow);
                }
                if (t3 == null)
                {
                    verticies.Add(new Vector3(x + 1, y, z));
                    colours.Add(c);
                }
                else
                {
                    if (t3 == Tile.WATER)
                        verticies.Add(new Vector3(x + 1, waterDepth, z));
                    else
                        verticies.Add(new Vector3(x + 1, 0, z));

                    colours.Add(t3.GetColor());

                }
            }
            else
            {
                //BOTH EXTREME
                Tile t1 = neighbors[0] != null ? neighbors[0].GetTile(x, 0) : null;
                Tile t2 = neighbors[1] != null ? neighbors[1].GetTile(0, 0) : null;
                Tile t3 = neighbors[2] != null ? neighbors[2].GetTile(0, z) : null;
                if (t1 == null)
                {
                    verticies.Add(new Vector3(x, y, z + 1));
                    colours.Add(c);
                }
                else
                {
                    if (t1 == Tile.WATER)
                        verticies.Add(new Vector3(x, waterDepth, z + 1));
                    else
                        verticies.Add(new Vector3(x, 0, z + 1));

                    colours.Add(t1.GetColor());
                }

                if (t2 == null)
                {
                    verticies.Add(new Vector3(x + 1, y, z + 1));
                    colours.Add(c);
                }
                else
                {
                    if (t2 == Tile.WATER)
                        verticies.Add(new Vector3(x + 1, waterDepth, z + 1));
                    else
                        verticies.Add(new Vector3(x + 1, 0, z + 1));

                    colours.Add(t2.GetColor());
                }
                if (t3 == null)
                {
                    verticies.Add(new Vector3(x + 1, y, z));
                    colours.Add(c);
                }
                else
                {
                    if (t3 == Tile.WATER)
                        verticies.Add(new Vector3(x + 1, waterDepth, z ));
                    else
                        verticies.Add(new Vector3(x + 1, 0, z));

                    colours.Add(t3.GetColor());
                }

            }
        }


        triangles.Add(tri);
        triangles.Add(tri+1);
        triangles.Add(tri+2);
        triangles.Add(tri);
        triangles.Add(tri + 2);
        triangles.Add(tri + 3);
        tri += 4;




    }
}

if (!hasWater)
{
    foreach (Vector3 v in verticies)
    {
        if (v.y != 0)
        {
            hasWater = true;
            break;
        }
    }
}
Debug.EndDeepProfile("chunk_create_mesh");
Debug.BeginDeepProfile("chunk_set_mesh");
MeshFilter meshFilt = GetComponent<MeshFilter>();

meshFilt.mesh = new Mesh();
meshFilt.mesh.vertices = verticies.ToArray();
meshFilt.mesh.triangles = triangles.ToArray();
meshFilt.mesh.colors = colours.ToArray();
meshFilt.mesh.normals = normals.ToArray();
*/
#endregion