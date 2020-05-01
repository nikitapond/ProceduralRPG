using UnityEngine;
using UnityEditor;
[System.Serializable]
public class BrickWall : WorldObjectData
{
    private int Height;
    public BrickWall(Vec2i worldPosition, WorldObjectMetaData meta = null, Vec2i size = null) : base(worldPosition, meta, size)
    {
    }


    public override string Name => "brick_wall";

    public override WorldObjects ObjID => WorldObjects.WALL;
    public override WorldObjectData Copy(Vec2i pos)
    {
        if (pos == null)
            pos = WorldPosition;

        return new BrickWall(pos, this.MetaData, this.Size);

    }
    public override WorldObject CreateWorldObject(Transform transform = null)
    {
        WorldObject obj = base.CreateWorldObject(transform);
        if (Height == 1)
        {

        }

        foreach(Transform t in transform)
        {
            t.gameObject.layer = 8;
        }
        return obj;
    }

    public override void OnObjectLoad(WorldObject obj)
    {
        obj.GetComponentInChildren<MeshRenderer>().material = ResourceManager.GetMaterial("brick");
    }
}