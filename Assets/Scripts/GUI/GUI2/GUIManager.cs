using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUIManager : MonoBehaviour
{
    public static GUIManager Instance;

    public IngameGUI IngameGUI;
    public OpenGUI OpenGUI;
    public DialogGUI DialogGUI;
    public ShopGUI ShopGUI;
    public bool OpenGUIVisible { get; private set; }

    public Button[] Buttons;

    private void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        
        OpenGUIVisible = false;
        OpenGUI.gameObject.SetActive(false);
        DialogGUI.gameObject.SetActive(false);
    }


    public void SetVisible(bool vis)
    {
        OpenGUIVisible = vis;
        OpenGUI.gameObject.SetActive(vis);

    }

    public void StartDialog(NPC npc)
    {
        DialogGUI.gameObject.SetActive(true);
        DialogGUI.SetNPC(npc);
    }

    public void StartShop(NPC npc, Inventory inventory)
    {
        GameManager.SetPause(true);
        ShopGUI.gameObject.SetActive(true);
        ShopGUI.StartShop(npc, inventory);
        IngameGUI.gameObject.SetActive(false);
        DialogGUI.gameObject.SetActive(false);

    }

    public void EndShop()
    {
        //GameManager.SetPause(false);
        ShopGUI.gameObject.SetActive(false);
        IngameGUI.gameObject.SetActive(true);
        DialogGUI.gameObject.SetActive(true);


    }



}
