using UnityEngine;
using UnityEditor;

public class WoodSpikeWall : WorldObjectData
{
    public override WorldObjects ID => WorldObjects.WOOD_SPIKE;

    public override string Name => "Wood Spike";

    public override SerializableVector3 Size => new Vector3(0.7f, 2f, 0.7f);

    public override bool IsCollision => true;

    public override bool AutoHeight => throw new System.NotImplementedException();
}