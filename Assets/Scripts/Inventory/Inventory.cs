using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
[System.Serializable]
public class Inventory
{
    public bool Disposed { get; private set; }
    [SerializeField]
    private List<ItemStack> Items;

    public IInventoryObject InventoryObject { get; private set; }

    public Inventory()
    {

        Items = new List<ItemStack>();
    }

    public bool IsEmpty { get { return Items.Count == 0; } }

    public void SetWorldObject(IInventoryObject invObj)
    {
        InventoryObject = invObj;
    }

    public List<ItemStack> GetItems()
    {
        return Items;
    }



    public void RemoveItemStack(ItemStack itemStack)
    {
        Debug.Log("removing item ");
        Items.Remove(itemStack);
        if (InventoryObject != null)
            InventoryObject.OnRemoveItem(itemStack.Item);
    }

    public void RemoveItem(Item item)
    {
        ItemStack itst = ContainsItemStack(item);
        if(itst != null)
        {
            itst.RemoveFromStack();
            if (itst.Count == 0)
                RemoveItemStack(itst);
        }
        itst?.RemoveFromStack();

    }

    public ItemStack ContainsItemStack(Item item)
    {
        if (item == null)
            return null;

        if(item is Key)
        {
            foreach (ItemStack itSt in Items)
            {
                if (itSt != null && itSt.Item != null)
                {
                    if(itSt.Item is Key)
                    {
                        if ((itSt.Item as Key).KeyID == (item as Key).KeyID)
                            return itSt;
                    }

                }
            }
        }
        else
        {

            foreach (ItemStack itSt in Items)
            {
                if (itSt != null && itSt.Item != null)
                {
                    if (itSt.Item == item || itSt.Item.Equals(item))
                    {
                        return itSt;
                    }

                }
            }
        }


        return null;
    }

    public void AddItemStack(ItemStack item)
    {
        foreach(ItemStack itSt in Items)
        {
            if(itSt.Item == item.Item)
            {
                itSt.AddToStack(item.Count);
                if (InventoryObject != null)
                    InventoryObject.OnAddItem(item.Item);
                return;
            }
        }
        Items.Add(item);
        if (InventoryObject != null)
            InventoryObject.OnAddItem(item.Item);
    }
    public void AddItem(Item item)
    {
        ItemStack toStack = ContainsItemStack(item);
        if(toStack != null)
        {
            if (InventoryObject != null)
                InventoryObject.OnAddItem(item);
            toStack.AddToStack();
        }
        if(toStack == null)
        {
            if (InventoryObject != null)
                InventoryObject.OnAddItem(item);
            toStack = new ItemStack(item);
            Items.Add(toStack);
        }
        

    }

    public void AddAll(Inventory inv)
    {
        foreach(ItemStack st in inv.Items)
        {
            AddItemStack(st);
        }
    }

    public void Dispose()
    {
        Items.Clear();
        Disposed = true;
    }


}