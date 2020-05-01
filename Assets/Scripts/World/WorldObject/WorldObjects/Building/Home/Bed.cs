using UnityEngine;
using UnityEditor;

[System.Serializable]
public class Bed : WorldObjectData, IMultiTileObject
{
    public Bed(Vec2i worldPosition, WorldObjectMetaData meta = null) : base(worldPosition, meta, new Vec2i(1,2))
    {
    }

    public override WorldObjects ObjID => WorldObjects.BED;
    public override WorldObjectData Copy(Vec2i pos)
    {
        if (pos == null)
            pos = WorldPosition;

        return new Bed(pos, this.MetaData);

    }
    public override string Name => "Bed";
    private IMultiTileObjectChild[,] Children;
    public IMultiTileObjectChild[,] GetChildren()
    {
        if(Children != null)
        {
            return Children;
        }
        Children = new IMultiTileObjectChild[Size.x, Size.z];
        for(int x=0; x<Size.x; x++)
        {
            for(int z=0; z<Size.z; z++)
            {
                if (x == 0 && z == 0)
                    continue;
                Children[x, z] = new EmptyObjectBase(WorldPosition + new Vec2i(x, z), parent: this);
            }
        }
        return Children;
    }
}