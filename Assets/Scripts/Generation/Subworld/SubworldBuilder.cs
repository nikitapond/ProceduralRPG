using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public abstract class SubworldBuilder : BuilderBase
{


    public ISubworldEntranceObject Exit { get; protected set; }

    public Vec2i WorldEntrance { get; private set; }

    protected Vec2i SubworldEntrance;
    public SubworldBuilder(Vec2i worldEntrance, Vec2i chunkSize) : base(new Vec2i(0,0), chunkSize)
    {
        WorldEntrance = worldEntrance;

    }


    public Subworld ToSubworld()
    {
        ChunkData[,] chunks = new ChunkData[ChunkSize.x, ChunkSize.z];
        List<ChunkData> rawChunks = this.ToChunkData();
        foreach(ChunkData c in rawChunks)
        {
            chunks[c.X, c.Z] = c;
        }
        Subworld sw = new Subworld(chunks, SubworldEntrance, WorldEntrance);
        sw.Exit = Exit;
        return sw;
    }

}