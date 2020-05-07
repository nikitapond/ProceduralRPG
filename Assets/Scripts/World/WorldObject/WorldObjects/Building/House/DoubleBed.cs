using UnityEngine;
using UnityEditor;
[System.Serializable]
public class DoubleBed : WorldObjectData
{
    public override WorldObjects ID => WorldObjects.DOUBLE_BED;

    public override string Name => "Double Bed";

    public override SerializableVector3 Size => new Vector3(2, 0.5f, 2.5f);

    public override bool IsCollision => true;

    public override bool AutoHeight => throw new System.NotImplementedException();
}