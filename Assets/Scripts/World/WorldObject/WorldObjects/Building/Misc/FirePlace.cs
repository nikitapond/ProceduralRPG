using UnityEngine;
using UnityEditor;

[System.Serializable]
public class FirePlace : WorldObjectData
{
    public override WorldObjects ID => WorldObjects.FIRE_PLACE;

    public override string Name => "Fire Place";

    public override SerializableVector3 Size => new Vector3(2,5,2);

    public override bool IsCollision => true;

    public override bool AutoHeight => throw new System.NotImplementedException();
}