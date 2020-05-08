using UnityEngine;
using UnityEditor;

public class ShopGUI : MonoBehaviour
{


    public void Cancel()
    {
        GUIManager.Instance.EndShop();
    }

    public void Confirm()
    {

    }

    void SetInventory(Inventory inventory)
    {

    }

}