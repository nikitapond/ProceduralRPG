using UnityEngine;
using UnityEditor;
[System.Serializable]
public class BanditGaurdTower : WorkEquiptmentData, IMultiTileObject
{
    public BanditGaurdTower(Vec2i worldPosition) : base(worldPosition, null, size:new Vec2i(3,3))
    {
    }

    public override WorldObject CreateWorldObject(Transform transform = null)
    {
        WorldObject obj = base.CreateWorldObject(transform);
        obj.transform.localScale = new Vector3(Size.x, 1, Size.z);
        return obj;
    }

    public override WorldObjects ObjID => WorldObjects.BANDIT_GAURD_TOWER;

    public override string Name => "Bandit Gaurd Tower";

    public override WorldObjectData Copy(Vec2i pos = null)
    {
        if (pos == null)
            pos = WorldPosition;

        return new BanditGaurdTower(pos);
    }
    private IMultiTileObjectChild[,] Children;
    public IMultiTileObjectChild[,] GetChildren()
    {
        if (Children != null)
        {
            return Children;
        }
        Children = new IMultiTileObjectChild[Size.x, Size.z];
        for (int x = 0; x < Size.x; x++)
        {
            for (int z = 0; z < Size.z; z++)
            {
                if (x == 0 && z == 0)
                    continue;
                Children[x, z] = new EmptyObjectBase(WorldPosition + new Vec2i(x, z), parent: this);
            }
        }
        return Children;
    }
}