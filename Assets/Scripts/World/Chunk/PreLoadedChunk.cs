using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
/// <summary>
/// A holder object that holds onto
/// the thread generated details of the chunk.
/// This data is generated in <see cref="ChunkLoader"/>,
/// when the chunk has finished pre-loading, we load in the chunk itself.
/// </summary>
public class PreLoadedChunk
{
    public Vec2i Position { get; private set; }
    public PreMesh TerrainMesh { get; private set; }
    public Dictionary<Voxel, PreMesh> VoxelMesh { get; private set; }
    public float[,] heights;
    public ChunkData ChunkData { get; private set; }

    public LoadedChunk2 LoadedChunk { get; private set; }

    public PreLoadedChunk(Vec2i cPos, PreMesh terrain, ChunkData cDat)
    {
        Position = cPos;

        TerrainMesh = terrain;
        VoxelMesh = new Dictionary<Voxel, PreMesh>();
        ChunkData = cDat;
    }

    public static Mesh CreateMesh(PreMesh pmesh)
    {
        Mesh mesh = new Mesh();
        mesh.vertices = pmesh.Verticies;
        mesh.triangles = pmesh.Triangles;
        if (pmesh.Colours != null)
            mesh.colors = pmesh.Colours;
        if (pmesh.UV != null)
        {
            mesh.uv = pmesh.UV;
        }
        if (pmesh.Normals != null)
        {
            mesh.normals = pmesh.Normals;
        }
            
            

        return mesh;
    }

    public void SetLoadedChunk(LoadedChunk2 lc)
    {
        LoadedChunk = lc;
    }

}

public class PreMesh
{
    public Vector3[] Verticies;
    public int[] Triangles;
    public Vector2[] UV;
    public Color[] Colours;
    public Vector3[] Normals;

    public void RecalculateNormals()
    {

        Normals = new Vector3[Verticies.Length];

        for (int t = 0; t < Triangles.Length; t += 3)
        {
            Vector3 AB = Verticies[Triangles[t + 1]] - Verticies[Triangles[t]];
            Vector3 AC = Verticies[Triangles[t + 2]] - Verticies[Triangles[t]];
            Vector3 norm = Vector3.Cross(AB, AC);
            norm.Normalize();
            Normals[Triangles[t]] += norm;
            Normals[Triangles[t + 1]] += norm;
            Normals[Triangles[t + 2]] += norm;
        }
        for (int i = 0; i < Normals.Length; i++)
        {
            Normals[i].Normalize();
        }


    }
}
