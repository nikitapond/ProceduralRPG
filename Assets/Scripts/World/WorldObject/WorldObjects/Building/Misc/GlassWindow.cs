using UnityEngine;
using UnityEditor;
[System.Serializable]
public class GlassWindow : WorldObjectData
{
    public GlassWindow(Vector3 scale, float rotation = 0) : base(rotation)
    {
        Scale = scale;
    }

    public GlassWindow(Vector3 position, Vector3 scale,  float rotation = 0) : base(position, rotation)
    {
        Scale = scale;
    }
    public override bool AutoHeight => false;
    public override WorldObjects ID => WorldObjects.GLASS_WINDOW;

    public override string Name => "Glass Window";

    private SerializableVector3 BaseSize => Vector3.one;


    public override SerializableVector3 Size => Vector3.Scale(BaseSize, Scale);

    public override bool IsCollision => true;

    public void SetScale(Vector3 scale)
    {
        Scale = scale;
    }
}