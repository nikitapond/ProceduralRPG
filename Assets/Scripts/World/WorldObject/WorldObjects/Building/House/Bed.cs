using UnityEngine;
using UnityEditor;
[System.Serializable]
public class Bed : WorldObjectData
{
    public override WorldObjects ID => WorldObjects.BED;

    public override string Name => "Bed";

    public override SerializableVector3 Size => new Vector3(1,1,2);

    public override bool IsCollision => true;
    public override bool AutoHeight => true;
    public Bed(Vector3 pos, float rotation=0):base (pos, rotation)
    {

    }
    public Bed(float rotation) : base(rotation) { }
}