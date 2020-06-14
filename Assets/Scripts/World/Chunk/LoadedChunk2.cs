using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
public class LoadedChunk2 : MonoBehaviour
{

    public Vec2i Position { get; private set; }

    //The chunk data for this chunk
    public ChunkData Chunk { get; private set; }
    //The current LOD for this chunk
    public int LOD { get; private set; }
    //True if we have generated the voxels for this loaded chunk
    public bool HasGeneratedVoxels { get; private set; }
    //True if we have loaded in world objects
    public bool HasLoadedWorldObjects { get; private set; }


    public PreLoadedChunk PreLoaded { get; private set; }


    public void SetChunkAndLOD(ChunkData cd, int lod)
    {
        Chunk = cd;
        LOD = lod; 
        Position = new Vec2i(cd.X, cd.Z);


        ChunkRegionManager.Instance.ChunkLoader.AddToGenerationQue(this);

    }

    public void SetChunk(ChunkData cd)
    {
        if (cd == Chunk)
            return;


        Position = new Vec2i(cd.X, cd.Z);
        Chunk = cd;

        ChunkRegionManager.Instance.ChunkLoader.AddToGenerationQue(this);

    }

    public void SetLOD(int lod)
    {

        if (lod == LOD)
            return;

        LOD = lod;

        ChunkRegionManager.Instance.ChunkLoader.AddToGenerationQue(this);

    }

    public void SetHasGeneratedVoxels(bool has)
    {
        HasGeneratedVoxels = has;
    }
    public void SetHasLoadedWorldObjects(bool has)
    {
        HasLoadedWorldObjects = has;
    }

    public void SetPreLoadedChunk(PreLoadedChunk prc) {
        PreLoaded = prc;
    }


    public override bool Equals(object other)
    {
        if (other == null || (other as LoadedChunk2) == null) return false;
        LoadedChunk2 lc = other as LoadedChunk2;
        return lc.Position == Position;
    }
}