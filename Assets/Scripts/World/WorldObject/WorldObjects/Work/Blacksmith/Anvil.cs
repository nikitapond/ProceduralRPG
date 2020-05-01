using UnityEngine;
using UnityEditor;
[System.Serializable]
public class Anvil : WorkEquiptmentData
{
    public Anvil(Vec2i worldPosition, WorldObjectMetaData meta=null, Vec2i size = null) : base(worldPosition, meta, size)
    {
    }


    public override string Name => "Anvil";
    public override WorldObjectData Copy(Vec2i pos)
    {
        if (pos == null)
            pos = WorldPosition;

        return new Anvil(pos, this.MetaData, this.Size);

    }
    public override WorldObjects ObjID => WorldObjects.ANVIL;
}