using UnityEngine;
using UnityEditor;
[System.Serializable]
public class Chest : WorldObjectData
{
    public override WorldObjects ID => WorldObjects.CHEST;

    public override string Name => "Chest";

    public override SerializableVector3 Size => Vector3.one;

    public override bool IsCollision => true;

    public override bool AutoHeight => throw new System.NotImplementedException();
}