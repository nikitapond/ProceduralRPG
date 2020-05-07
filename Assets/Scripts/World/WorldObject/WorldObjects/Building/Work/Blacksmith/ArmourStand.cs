using UnityEngine;
using UnityEditor;
[System.Serializable]
public class ArmourStand : WorldObjectData
{
    public override WorldObjects ID => WorldObjects.ARMOUR_STAND;

    public override string Name => "Armour Stand";

    public override SerializableVector3 Size => new Vector3(1,2,1);

    public override bool IsCollision => true;

    public override bool AutoHeight => throw new System.NotImplementedException();

    protected override void OnConstructor()
    {
        Scale = new Vector3(1, 2, 1);
    }
}