using UnityEngine;
using UnityEditor;

public class LootSack : WorldObjectData, IInventoryObject
{
    public override WorldObjects ID => WorldObjects.LOOT_SACK;

    public override string Name => "Loot Sack";

    public override SerializableVector3 Size =>Vector3.one;

    public override bool IsCollision => false;

    public override bool AutoHeight => true;

    private Inventory Inventory;


    public LootSack(Vector3 position, float rotation = 0) : base(position, rotation)
    {

    }
    public LootSack(float rotation = 0) : base (rotation)
    {
        Position = Vector3.zero;
        Rotation = rotation;
    }

    protected override void OnConstructor()
    {
        Inventory = new Inventory();
        Inventory.SetWorldObject(this);
    }
    public Inventory GetInventory()
    {
        return Inventory;
    }

    public void OnAddItem(Item item)
    {
    }

    public void OnRemoveItem(Item item)
    {
        if (Inventory.IsEmpty)
        {
            WorldManager.Destroy(this.LoadedObject.gameObject);
        }
    }

    public bool CanAddItem(Item item)
    {
        return true;
    }
}