using UnityEngine;
using UnityEditor;
[System.Serializable]
public class LootSack : WorldObjectData, IInventoryObject
{

    private Inventory Inventory { get; }
    public LootSack(Vec2i worldPosition, WorldObjectMetaData meta = null, Vec2i size = null) : base(worldPosition, meta, size)
    {
        Inventory = new Inventory();
        Inventory.SetWorldObject(this);
    }


    public override WorldObjectData Copy(Vec2i pos)
    {
        if (pos == null)
            pos = WorldPosition;

        return new LootSack(pos);
        

    }
    public override string Name => "Loot Sack";

    public override WorldObjects ObjID => WorldObjects.LOOT_SACK;

    public Inventory GetInventory()
    {
        return Inventory;
    }

    public void OnRemoveItem()
    {

        if (Inventory.IsEmpty)
            Inventory.Dispose();
        GameManager.WorldManager.DestroyWorldObject(WorldPosition);
    }

    public void OnAddItem()
    {
    }
}