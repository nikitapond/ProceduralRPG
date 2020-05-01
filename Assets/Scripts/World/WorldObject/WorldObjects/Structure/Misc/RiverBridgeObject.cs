using UnityEngine;
using UnityEditor;
[System.Serializable]
public class RiverBridgeObject : WorldObjectData, IMultiTileObject
{
    private IMultiTileObjectChild[,] Children;
    private Vec2i Direction;
    public RiverBridgeObject(Vec2i minPoint, Vec2i maxPoint, Vec2i dir) : base(minPoint, null, maxPoint-minPoint)
    {
        Direction = dir;
    }

    public override WorldObjectData Copy(Vec2i pos)
    {
        if (pos == null)
            pos = WorldPosition;

        return new RiverBridgeObject(pos, WorldPosition + this.Size, Direction);

    }
    public override string Name => "Bridge";

    public override WorldObjects ObjID => WorldObjects.BRIDGE;

    public override WorldObject CreateWorldObject(Transform transform = null)
    {
        return base.CreateWorldObject(transform);
    }

    /// <summary>
    /// Checks if children have been generated. If not, then it generates them.
    /// </summary>
    /// <returns></returns>
    public IMultiTileObjectChild[,] GetChildren()
    {
        //Check if children have been generated. 
        if (Children != null)
            return Children;
        //If not, we create them.
        //Create empty array of children
        Children = new IMultiTileObjectChild[Size.x, Size.z];

        for(int x=1; x<Size.x-1; x++)
        {
            for(int z=1; z<Size.z-1; z++)
            {
                Children[x, z] = new BridgeBaseObject(this, WorldPosition + new Vec2i(x, z));
            }
        }

        if(Direction.x == 1)
        {
            for(int i=1; i < Size.z - 1; i++)
            {
                Children[0, i] = new BridgeBaseObject(this, WorldPosition + new Vec2i(0, i));
                Children[Size.x-1, i] = new BridgeBaseObject(this, WorldPosition + new Vec2i(Size.x - 1, i));
            }
            for(int i=0; i<Size.x; i++)
            {
                Children[i, 0] = new BridgeRampObject(this, WorldPosition + new Vec2i(i, 0));
                (Children[i, 0] as WorldObjectData).GetMetaData().Direction = new Vec2i(0, 1);
                Children[i, Size.z-1] = new BridgeRampObject(this, WorldPosition + new Vec2i(i, Size.z - 1));
                (Children[i, Size.z - 1] as WorldObjectData).GetMetaData().Direction = new Vec2i(0, -1);
            }
        }
        else
        {
            for (int i = 1; i < Size.x - 1; i++)
            {
                Children[i, 0] = new BridgeBaseObject(this, WorldPosition + new Vec2i(i, 0));
                Children[i, Size.z-1] = new BridgeBaseObject(this, WorldPosition + new Vec2i(i, Size.z - 1));
            }
            for (int i = 0; i < Size.z; i++)
            {
                Children[0, i] = new BridgeRampObject(this, WorldPosition + new Vec2i(0, i));
                (Children[0, i] as WorldObjectData).GetMetaData().Direction = new Vec2i(-1, 0);
                Children[Size.x-1, i] = new BridgeRampObject(this, WorldPosition + new Vec2i(Size.x - 1, i));
                (Children[Size.x - 1, i] as WorldObjectData).GetMetaData().Direction = new Vec2i(1, 0);
            }
        }

        return Children;
    }
}
[System.Serializable]
public class BridgeBaseObject : WorldObjectData, IMultiTileObjectChild
{
    private RiverBridgeObject Parent;
    public BridgeBaseObject(RiverBridgeObject parent, Vec2i worldPosition) : base(worldPosition, new Vector3(0,.1f,0),null, null)
    {
        Parent = parent;
    }

    public override WorldObjects ObjID => WorldObjects.BRIDGE_BASE;

    public override string Name => "Bridge Base";

    public IMultiTileObject Getparent()
    {
        return Parent;
    }
    public override WorldObjectData Copy(Vec2i pos)
    {
        if (pos == null)
            pos = WorldPosition;

        return new BridgeBaseObject(Parent, pos);

    }

    public override WorldObject CreateWorldObject(Transform transform = null)
    {
        WorldObject main = base.CreateWorldObject(transform);
        Water underWater = new Water(new Vec2i(0, 0));
        underWater.SetUVOffset(WorldPosition.AsVector2());
        underWater.CreateWorldObject(main.transform);
        return main;
    }
}
[System.Serializable]
public class BridgeRampObject : WorldObjectData, IMultiTileObjectChild
{
    private RiverBridgeObject Parent;
    public BridgeRampObject(RiverBridgeObject parent, Vec2i worldPosition) : base(worldPosition, null, null)
    {
        Parent = parent;
    }
    public override WorldObjectData Copy(Vec2i pos)
    {
        if (pos == null)
            pos = WorldPosition;

        return new BridgeRampObject(Parent, pos);

    }
    public override WorldObjects ObjID => WorldObjects.BRIDGE_RAMP;
    public override string Name => "Bridge ramp";

    public IMultiTileObject Getparent()
    {
        return Parent;
    }

    public override WorldObject CreateWorldObject(Transform transform = null)
    {
        WorldObject main = base.CreateWorldObject(transform);
        Water underWater = new Water(new Vec2i(0, 0));
        underWater.SetUVOffset(WorldPosition.AsVector2());
        underWater.CreateWorldObject(main.transform);
        return main;
    }
}