using UnityEngine;
using UnityEditor;
[System.Serializable]
public class Roof : WorldObjectData
{
    public Roof(Vec2i worldPosition, Vec2i size) : base(worldPosition, null, size)
    {
        GetMetaData().Height = 5;
    }

    public override WorldObjects ObjID => WorldObjects.ROOF;
    public override WorldObjectData Copy(Vec2i pos)
    {
        if (pos == null)
            pos = WorldPosition;

        return new Roof(pos, this.Size);

    }
    public override string Name => "Roof";
}