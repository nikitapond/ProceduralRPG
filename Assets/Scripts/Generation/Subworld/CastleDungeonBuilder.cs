using UnityEngine;
using UnityEditor;

public class CastleDungeonBuilder : SubworldBuilder
{
    public CastleDungeonBuilder(Vec2i worldEntrance, Vec2i chunkSize) : base(worldEntrance, chunkSize)
    {
    }

    public override void Generate(GenerationRandom ran)
    {
        SubworldEntrance = new Vec2i(5, 5);
    }
}