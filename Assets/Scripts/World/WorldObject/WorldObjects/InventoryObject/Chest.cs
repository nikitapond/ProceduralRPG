using UnityEngine;
using UnityEditor;

[System.Serializable]
public class Chest : WorldObjectData, IInventoryObject, IOnEntityInteract
{
    private Inventory Inventory;
    public Chest(Vec2i worldPosition) : base(worldPosition, null, null)
    {
        Inventory = new Inventory();
        Inventory.SetWorldObject(this);
    }

    public override WorldObjects ObjID => WorldObjects.CHEST;

    public override string Name => "Chest";

    public override WorldObjectData Copy(Vec2i pos = null)
    {
        if (pos == null)
            pos = WorldPosition;
        return new Chest(pos);
    }

    public Inventory GetInventory()
    {
        return Inventory;
    }

    public void OnAddItem()
    {
        
    }

    public void OnEntityInteract(Entity entity)
    {
        //TODO - add sound ???
    }

    public void OnRemoveItem()
    {
        
    }
}