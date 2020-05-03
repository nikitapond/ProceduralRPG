using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryGUI2 : MonoBehaviour
{
    private Player Player;

    private ItemTag CurrentInventoryItemTag;

    public DisplayInventoryGUI MainInventory;
    public DisplayInventoryGUI SecondInventory;

    public ItemButton ItemButtonPrefab;

    /// <summary>
    /// Called when the inventory is opened
    /// </summary>
    private void OnEnable()
    {
        Player = GameManager.PlayerManager.Player;
        CurrentInventoryItemTag = ItemTag.WEAPON;

        MainInventory.gameObject.SetActive(true);
        MainInventory.SetInventory(Player.Inventory);
        MainInventory.SetItemTag(CurrentInventoryItemTag);
        MainInventory.SetMain(true);

        //Check if there are any inventories near the player to display
        WorldObjectData invObj = GameManager.WorldManager.World.InventoryObjectNearPoint(Vec2i.FromVector3(Player.Position));
        if(invObj == null || !(invObj is IInventoryObject))
        {
            //If no object is found, turn secondary inventory off
            SecondInventory.gameObject.SetActive(false);
        }
        else {
            Inventory inv = (invObj as IInventoryObject).GetInventory();
            SecondInventory.SetInventory(inv);
            SecondInventory.SetItemTag(CurrentInventoryItemTag);
            SecondInventory.SetMain(false);
            SecondInventory.gameObject.SetActive(true);
        }
    }
  

    public Inventory GetSecondInventory()
    {
        if(SecondInventory.Inventory == null)
        {
            Vec2i playerPos = Vec2i.FromVector2(Player.Position2);
            //Get current object on player position
            WorldObjectData currentSpace = GameManager.WorldManager.World.GetWorldObject(playerPos);
            if(currentSpace == null)
            {
                //if there is no object here, we create one
                LootSack lootsack = new LootSack(playerPos.AsVector3());
                GameManager.WorldManager.AddNewObject(lootsack);
                SecondInventory.SetInventory(lootsack.GetInventory());

                SecondInventory.gameObject.SetActive(true);

                return SecondInventory.Inventory;
            }
            //If current tile is taken, we check all the surrounding tiles
            Vec2i[] surrounding = GameManager.WorldManager.World.EmptyTilesAroundPoint(playerPos);
            foreach(Vec2i v in surrounding)
            {
                WorldObjectData surSpace = GameManager.WorldManager.World.GetWorldObject(v);
                if(surSpace == null)
                {
                    //if there is no object here, we create one
                    LootSack lootsack = new LootSack(v.AsVector3());
                    GameManager.WorldManager.AddNewObject(lootsack);
                    SecondInventory.SetInventory(lootsack.GetInventory());

                    SecondInventory.gameObject.SetActive(true);

                    return SecondInventory.Inventory;
                }
            }
            //If there is no space near, we return null
            return null;
        }
        else
        {
            //If the inventory is not null, we add to it
            return SecondInventory.Inventory;
        }
    }

    /// <summary>
    /// When an item tag button is clicked, we 
    /// find the ItemTag relating to the button and set it as the
    /// currently visible item tag
    /// </summary>
    /// <param name="button"></param>
    public void OnButtonClick(int button)
    {
        CurrentInventoryItemTag = (ItemTag)button;
        MainInventory.SetItemTag(CurrentInventoryItemTag);
        SecondInventory.SetItemTag(CurrentInventoryItemTag);

    }

}
