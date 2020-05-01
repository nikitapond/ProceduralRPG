using UnityEngine;
using UnityEditor;

public class PractiseDummy : WorkEquiptmentData
{
    public PractiseDummy(Vec2i worldPosition, WorldObjectMetaData meta, Vec2i size = null) : base(worldPosition, meta, size)
    {
    }

    public override WorldObjects ObjID => WorldObjects.PRACTISE_DUMMY;

    public override string Name => "Practise Dummy";

    public override WorldObjectData Copy(Vec2i pos)
    {
        if (pos == null)
            pos = WorldPosition;

        return new PractiseDummy(pos, this.MetaData, this.Size);

    }
}