using UnityEngine;
using UnityEditor;

public abstract class TacticalLocation : ChunkStructure
{
    public TacticalLocation(Vec2i cPos, Vec2i cSize) : base(cPos, cSize)
    {
    }

    public abstract TacLocType Type { get; }
}

public enum TacLocType
{
    fort, tower
}