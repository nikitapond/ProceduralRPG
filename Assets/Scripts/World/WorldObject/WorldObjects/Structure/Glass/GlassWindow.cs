using UnityEngine;
using UnityEditor;
[System.Serializable]
public class GlassWindow : WorldObjectData
{
    public GlassWindow(Vec2i worldPosition, Vec2i direction) : base(worldPosition)
    {
        GetMetaData().Direction = direction;
    }

    public override WorldObjects ObjID => WorldObjects.GLASS_WINDOW;

    public override string Name => "Glass Window";


    public override WorldObjectData Copy(Vec2i pos)
    {
        if (pos == null)
            pos = WorldPosition;

        return new GlassWindow(pos, GetMetaData().Direction);

    }
}