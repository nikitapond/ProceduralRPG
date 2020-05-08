using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquiptmentGUI2 : MonoBehaviour
{

    public EquiptmentButton[] EquiptmentSlots;


    private Player Player;
    private void OnEnable()
    {
        Player = PlayerManager.Instance.Player;


        for(int i=0; i<7; i++)
        {

            Item item = Player.EquiptmentManager.GetEquiptItem((LoadedEquiptmentPlacement)i);
            EquiptmentSlots[i].SetButton(this, (LoadedEquiptmentPlacement)i, item);
        }

    }



    public void UnEquipt(LoadedEquiptmentPlacement slot)
    {
        Item itst = Player.EquiptmentManager.UnequiptItem(slot);
        Player.Inventory.AddItem(itst);
    }


}
