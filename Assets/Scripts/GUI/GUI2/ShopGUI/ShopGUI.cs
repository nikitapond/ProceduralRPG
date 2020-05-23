using UnityEngine;
using UnityEditor;

public class ShopGUI : MonoBehaviour
{

    private NPC NPC;
    private Inventory ShopInv;
    public void Cancel()
    {
        GUIManager.Instance.EndShop();
        GUIManager.Instance.StartDialog(NPC);
        //GUIManager.Instance.DialogGUI.gameObject.SetActive(true);
        //NPC.Dialog.StartDialog();
        //GUIManager.Instance.DialogGUI.DisplayCurrentNode();
    }

    public void Confirm()
    {

    }


    public void StartShop(NPC npc, Inventory inv)
    {
        NPC = npc;
        ShopInv = inv;
        GUIManager.Instance.DialogGUI.gameObject.SetActive(false);
    }
    void SetInventory(Inventory inventory)
    {

    }

}