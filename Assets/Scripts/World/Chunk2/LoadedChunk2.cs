using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
public class LoadedChunk2 : MonoBehaviour
{

    public Vec2i Position { get; private set; }
    public ChunkData2 Chunk;

    private void Start()
    {
       // GetComponent<NavMeshSurface>().BuildNavMesh();
    }

    public void SetChunk(ChunkData2 cd)
    {
        Position = new Vec2i(cd.X, cd.Z);
        Chunk = cd;
    }
}