using UnityEngine;
using UnityEditor;

public class BuildingInternalNoWalkFloor : WorldObjectData
{
    public override WorldObjects ID => WorldObjects.NO_WALK_ZONE;

    public override string Name => "No Walk";

    private SerializableVector3 Size_;
    public override SerializableVector3 Size => Size_;

    public override bool IsCollision => true;

    public override bool AutoHeight => throw new System.NotImplementedException();

    public BuildingInternalNoWalkFloor(Vector3 position, Vector3 size) : base(position)
    {
        Size_ = size;
    }

    public void SetSize(Vector3 v)
    {
        Size_ = v;
        if (IsLoaded)
        {
            //Get collider and set correct size
            BoxCollider bc = LoadedObject.GetComponent<BoxCollider>();
            bc.size = Size_;
            bc.center = Size_ / 2;
        }

    }

    public override void OnObjectLoad(WorldObject obj)
    {
        //Get collider and set correct size
        BoxCollider bc = obj.GetComponent<BoxCollider>();
        bc.size = Size_;
        bc.center = Size_ / 2;
        base.OnObjectLoad(obj);
    }
}