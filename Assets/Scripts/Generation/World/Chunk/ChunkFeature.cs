using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public abstract class ChunkFeature
{
    public Vec2i Pos { get; private set; }
    public ChunkFeature(Vec2i cPos)
    {
        Pos = cPos;
    }

    public abstract float MovementCost();

}
public class ChunkRiverNode : ChunkFeature
{

    public List<ChunkRiverNode> FlowIn;
    public List<ChunkRiverNode> FlowOut;

    public ChunkRiverNode(Vec2i cPos) : base(cPos)
    {
        FlowIn = new List<ChunkRiverNode>(1);
        FlowOut = new List<ChunkRiverNode>(1);
    }

    public override float MovementCost()
    {
        return Mathf.Infinity;
    }

    public bool HasBridge;

}
public class ChunkRiverBridge : ChunkFeature
{
    public ChunkRiverBridge(Vec2i cPos) : base(cPos)
    {

    }
    public override float MovementCost()
    {
        return 0; 
    }
}
public class ChunkRoad : ChunkFeature
{

    public RoadType Type { get; private set; }
    public ChunkRoad(Vec2i cPos, RoadType road) : base(cPos)
    {
        Type = road;
    }


    public enum RoadType
    {
        Dirt, Paved
    }

    public override float MovementCost()
    {
        if (Type == RoadType.Dirt)
            return 100;
        return 50;
    }
}
public class ChunkLake : ChunkFeature
{
    public ChunkLake(Vec2i cPos) : base(cPos)
    {
    }

    public override float MovementCost()
    {
        return Mathf.Infinity;
    }
}
