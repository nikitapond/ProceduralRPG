using UnityEngine;
using UnityEditor;

public class Door : WorldObjectData, ISubworldEntranceObject
{
    public override WorldObjects ID => WorldObjects.DOOR;

    public override string Name => "Door";

    public override SerializableVector3 Size => new Vector3(1.5f, 2.2f, 0.2f);

    public override bool IsCollision => true;

    public override bool AutoHeight => true;

    private int SubworldID;
    int ISubworldEntranceObject.GetSubworldID()
    {
        Debug.Log("Subworld: " + SubworldID);
        return SubworldID;
    }

    Key ISubworldEntranceObject.GetSubworldKey()
    {
        return null;
    }

    void ISubworldEntranceObject.SetSubworld(Subworld world)
    {

        SubworldID = world.SubworldID;
    }
}