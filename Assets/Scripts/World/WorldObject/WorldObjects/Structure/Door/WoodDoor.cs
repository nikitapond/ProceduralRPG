using UnityEngine;
using UnityEditor;
using System;
[System.Serializable]
public class WoodDoor : WorldObjectData, IOnEntityInteract
{
    bool IsOpen;
    private Delegate OnDoorOpen;
    public delegate void ExternalOnEntityInteract(Entity entity);
    private float OpenDirection;
    public WoodDoor(Vec2i worldPosition, WorldObjectMetaData meta = null, ExternalOnEntityInteract onDoorOpen =null , float openDirection=1) : base(worldPosition, meta, null)
    {
        OnDoorOpen = onDoorOpen;
        OpenDirection = openDirection;
    }

    public override WorldObjectData Copy(Vec2i pos)
    {
        if (pos == null)
            pos = WorldPosition;

        return new WoodDoor(pos, this.MetaData, (ExternalOnEntityInteract)OnDoorOpen,OpenDirection );

    }

    public override WorldObjects ObjID => WorldObjects.DOOR;

    public override string Name => "Door";

    public void OnEntityInteract(Entity entity)
    {
        if(OnDoorOpen != null)
        {
            OnDoorOpen.DynamicInvoke(entity);
            return;
        }
        Debug.Log("open");
        IsOpen = !IsOpen;

        if (IsOpen)
        {
            LoadedObject.transform.rotation = LoadedObject.transform.rotation * Quaternion.Euler(0, OpenDirection*90, 0);
        }
        else
        {
            LoadedObject.transform.rotation = LoadedObject.transform.rotation * Quaternion.Euler(0, -OpenDirection*90, 0);
        }
    }
}