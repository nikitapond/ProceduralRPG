using UnityEngine;
using UnityEditor;

public class LoadedChunk2 : MonoBehaviour
{

    public Vec2i Position { get; private set; }
    public ChunkData2 Chunk;

    public void SetChunk(ChunkData2 cd)
    {
        Position = new Vec2i(cd.X, cd.Z);
        Chunk = cd;
    }
}