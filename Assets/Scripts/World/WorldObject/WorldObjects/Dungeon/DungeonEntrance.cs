using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonEntrance : WorldObjectData, ISubworldEntranceObject
{
    public override WorldObjects ID => WorldObjects.DUNGEON_ENTRANCE;

    public override string Name => "Dungeon Entrance";

    public override SerializableVector3 Size => new Vector3(2,2,2);

    public override bool IsCollision => true;
    public override bool AutoHeight => true;

    private int SubworldID;

    public int GetSubworldID()
    {
        return SubworldID;
    }

    public Key GetSubworldKey()
    {
        return null;
    }

    public void SetSubworld(Subworld world)
    {
        SubworldID = world.SubworldID;
    }
}
