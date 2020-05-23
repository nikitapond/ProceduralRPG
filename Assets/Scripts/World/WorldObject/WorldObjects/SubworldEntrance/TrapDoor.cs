using UnityEngine;
using UnityEditor;
[System.Serializable]
public class TrapDoor : WorldObjectData, ISubworldEntranceObject
{
    public override WorldObjects ID => WorldObjects.TRAP_DOOR;

    public override string Name => "Trap door";

    public override SerializableVector3 Size => Vector3.one;

    public override bool IsCollision => true;

    public override bool AutoHeight => throw new System.NotImplementedException();

    public TrapDoor(Key key = null)
    {
        Key = key;
    }

    private int SubworldID;
    private Key Key;
    public int GetSubworldID()
    {
       return SubworldID;
    }

    public Key GetSubworldKey()
    {
        return Key;
    }

    public void SetSubworld(Subworld world)
    {
        SubworldID = world.SubworldID;
        
    }
}