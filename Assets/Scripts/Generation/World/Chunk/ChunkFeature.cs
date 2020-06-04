using UnityEngine;
using UnityEditor;

public abstract class ChunkFeature
{


    public abstract float MovementCost();

}
public class ChunkRiverNode : ChunkFeature
{
    public override float MovementCost()
    {
        return Mathf.Infinity;
    }

    public bool HasBridge;

}
public class ChunkRoad : ChunkFeature
{

    public RoadType Type { get; private set; }
    public ChunkRoad(RoadType road)
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
