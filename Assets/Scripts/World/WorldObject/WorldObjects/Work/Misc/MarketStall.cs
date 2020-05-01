using UnityEngine;
using UnityEditor;

[System.Serializable]
public class MarketStall : WorkEquiptmentData, IMultiTileObject
{
    public MarketStall(Vec2i worldPosition, WorldObjectMetaData meta=null) : base(worldPosition, meta, new Vec2i(2,1))
    {
    }

    public override WorldObjects ObjID => WorldObjects.MARKET_STALL;

    public override string Name => "Market Stall";
    private IMultiTileObjectChild[,] Children;
    public IMultiTileObjectChild[,] GetChildren()
    {
        if (Children != null)
            return Children;

        Children = new EmptyObjectBase[2, 1];
        Children[1, 0] = new EmptyObjectBase(WorldPosition + new Vec2i(1, 0), parent: this);
        return Children;
    }

    public override WorldObjectData Copy(Vec2i pos)
    {
        if (pos == null)
            pos = WorldPosition;

        return new MarketStall(pos, this.MetaData);

    }
}