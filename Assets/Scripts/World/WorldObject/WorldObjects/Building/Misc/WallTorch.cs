using UnityEngine;
using UnityEditor;
[System.Serializable]
public class WallTorch : WorldObjectData
{
    public override WorldObjects ID => WorldObjects.WALL_TORCH;

    public override string Name => "Wall torch";

    public override SerializableVector3 Size => Vector3.one;

    public override bool IsCollision => false;

    public override bool AutoHeight => false;
}