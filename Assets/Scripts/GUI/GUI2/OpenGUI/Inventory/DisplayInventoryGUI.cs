using UnityEngine;
using UnityEditor;

public class DisplayInventoryGUI : MonoBehaviour
{
    public InventoryGUI2 Parent;

    public GameObject InventorySpace;


    public Inventory Inventory { get; private set; }
    private ItemTag CurrentItemTag;


    public bool IsMain { get; private set; }


    public void SetMain(bool main)
    {
        IsMain = main;
    }

    public void SetInventory(Inventory inventory)
    {
        Inventory = inventory;

        SetItemTag(CurrentItemTag);       
    }

    /// <summary>
    /// Takes the selected item and attempts to equipt it
    /// If a piece of equiptment is already in the relevent equiptment slot,
    /// it un-equipts it and adds it to the inventory.
    /// </summary>
    /// <param name="itBut"></param>
    public void EquiptItem(ItemButton itBut)
    {
        Debug.Log("equipting");
        //If not equiptable, we do nothing (return)
        if (!(itBut.Item is EquiptableItem))
        {
            Debug.Log("not equiptable");
            return;
        }
            
        //Remove it from out inventory, and equipt it
        Inventory.RemoveItem(itBut.Item);
        Item toAdd = PlayerManager.Instance.Player.EquiptmentManager.EquiptItem(itBut.Item);
        Destroy(itBut.gameObject);
        //If the relevent slot had an item in before
        if(toAdd != null)
        {
            //Add item to inventory
            Inventory.AddItem(toAdd);
            //If its the current display item tag, add it to the display
            if (toAdd.HasTag(CurrentItemTag))
                AddItemToSpace(toAdd);
        }

        if (!IsMain)
        {
            if (Inventory.IsEmpty)
            {
                Inventory = null;
                gameObject.SetActive(false);
            }
            GameManager.EventManager.InvokeNewEvent(new PlayerPickupItem(itBut.Item));
        }

    }

    /// <summary>
    /// Removes the item from the inventory.
    /// Checks if an open inventory is present to swap item
    /// </summary>
    /// <param name="itBut"></param>
    public void RemoveItemFromInventory(ItemButton itBut)
    {

        if (IsMain)
        {//If this is the main inventory, we find a secondary inventory
            Inventory second = Parent.GetSecondInventory();
            //if this inventory is null, we return as we cannot remove this item
            if (second == null)
                return;
            //We add the item to both the invetory, and to its thingy
            Parent.SecondInventory.Inventory.AddItem(itBut.Item);
            Parent.SecondInventory.AddItemToSpace(itBut.Item);
            this.Inventory.RemoveItem(itBut.Item);
            Destroy(itBut.gameObject);
        }
        else
        {
            //if this is the second inventory, we add the object to the main inventory          

            Parent.MainInventory.AddItemToSpace(itBut.Item);
            Parent.MainInventory.Inventory.AddItem(itBut.Item);

            Destroy(itBut.gameObject);
            Inventory.RemoveItem(itBut.Item);

            if (Inventory.IsEmpty)
            {
                Inventory = null;
                gameObject.SetActive(false);
            }
            GameManager.EventManager.InvokeNewEvent(new PlayerPickupItem(itBut.Item));


        }



    }


    public void SetItemTag(ItemTag nTag)
    {
        CurrentItemTag = nTag;
        Clear();
        if (Inventory == null)
            return;
        foreach(ItemStack itst in Inventory.GetItems())
        {
            if (itst.Item.HasTag(CurrentItemTag))
            {
                AddItemToSpace(itst.Item);
            }
        }

    }


    private void AddItemToSpace(Item item, int count=1)
    {
        GameObject but = Instantiate(Parent.ItemButtonPrefab.gameObject);
        ItemButton itb = but.GetComponent<ItemButton>();
        itb.SetItem(item);
        but.transform.SetParent(InventorySpace.transform, false);
        itb.SetDisplayInventory(this);
    }


    private void OnDisable()
    {
        Clear();
    }

    private void Clear()
    {
        foreach (Transform child in InventorySpace.transform)
        {
            Destroy(child.gameObject);
        }
    }
}