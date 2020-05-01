using UnityEngine;
using UnityEditor;
[System.Serializable]
public class WoodSpikeWall : WorldObjectData
{


    public override WorldObjects ObjID => WorldObjects.WOOD_SPIKE;

    public override string Name => "Wood Spike";

    public WoodSpikeWall(Vec2i worldPosition) : base(worldPosition, null, null)
    {
    }

    public override WorldObjectData Copy(Vec2i pos)
    {
        if (pos == null)
            pos = WorldPosition;

        return new WoodSpikeWall(pos);

    }
}