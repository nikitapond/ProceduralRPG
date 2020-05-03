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

    public ChunkData2 ChunkData { get; private set; }
    public PreLoadedChunk(Vec2i cPos, PreMesh terrain, ChunkData2 cDat)
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
            Debug.Log("setting UV");
            mesh.uv = pmesh.UV;
        }
            

        return mesh;
    }

}

public struct PreMesh
{
    public Vector3[] Verticies;
    public int[] Triangles;
    public Vector2[] UV;
    public Color[] Colours;



}
