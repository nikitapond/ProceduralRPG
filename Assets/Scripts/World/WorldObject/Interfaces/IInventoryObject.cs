using UnityEngine;
using UnityEditor;

public interface IInventoryObject
{
    Inventory GetInventory();
    void OnRemoveItem(Item item);
    void OnAddItem(Item item);

    bool CanAddItem(Item item);
}