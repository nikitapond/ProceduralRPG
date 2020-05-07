using UnityEngine;
using UnityEditor;
[System.Serializable]
public class WeaponStand : WorldObjectData
{
    public override WorldObjects ID => WorldObjects.WEAPON_STAND;

    public override string Name => "Weapon Stand";

    public override SerializableVector3 Size => new Vector3(1,2,2);

    public override bool IsCollision => true;

    public override bool AutoHeight => throw new System.NotImplementedException();

    protected override void OnConstructor()
    {
        Scale = new Vector3(2, 1, 1);
        
    }

}