using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class GridPointTest : MonoBehaviour
{
    private Button Button;
    private Text Text;
    private GridPoint p;
    public void SetPoint(GridPoint p)
    {

        bool isActive = false;
        this.p = p;
        Text = GetComponentInChildren<Text>();

        if(p.Shell != null)
        {
            Button = GetComponent<Button>();
            isActive = true;
            ColorBlock b = Button.colors;
            Color c = Color.black;
            if(p.Shell is SettlementShell)
            {
                SettlementShell ss = p.Shell as SettlementShell;
                if(ss.Type == SettlementType.CAPITAL)
                {
                    c = Color.yellow;
                    Text.text = "CA";
                }else if(ss.Type == SettlementType.CITY)
                {
                    c = Color.magenta;
                    Text.text = "CI";
                }
                else if(ss.Type == SettlementType.TOWN)
                {
                    c = Color.blue;
                    Text.text = "TO";
                }
                else if(ss.Type == SettlementType.VILLAGE)
                {
                    c = Color.green;
                    Text.text = "VI";
                }
            }
            else if (p.Shell is TacticalLocationShell)
            {
                TacticalLocationShell ss = p.Shell as TacticalLocationShell;
                if (ss.Type ==TacLocType.fort)
                {
                    c = Color.red;
                    Text.text = "FO";
                }
                else if (ss.Type == TacLocType.tower) { 
                    c = Color.cyan;
                    Text.text = "TO";
                }               
            }else if(p.Shell is ChunkStructureShell)
            {
                ChunkStructureShell css = p.Shell as ChunkStructureShell;
                if(css.Type == ChunkStructureType.banditCamp)
                {
                    c = Color.red;
                    Text.text = "BA";
                }else if(css.Type == ChunkStructureType.vampireNest)
                {
                    c = new Color(0.5f, 0, 0.5f);
                    Text.text = "VA";
                }
                else if (css.Type == ChunkStructureType.kithenaCatacomb)
                {
                    c = new Color(0, 0.5f, 0.5f);
                    Text.text = "KI";
                }
                else if (css.Type == ChunkStructureType.ancientTemple)
                {
                    c = new Color(0, 0.5f, 0.5f);
                    Text.text = "AT";
                }
                else
                {
                    c = Color.red;
                    Text.text = "DA";
                }
            }


            b.normalColor = c;
            Button.colors = b;
        }


        this.gameObject.SetActive(isActive);
    }

    private Color FromDes(float des)
    {
        if (des < 1)
            return Color.red;
        if (des < 2)
            return Color.blue;
        return Color.yellow;
    }

    public void OnClick()
    {

        
            if(p.Economy != null)
            {
                Debug.Log(p.Economy.ToString());
                    
            }

      
    }

    private string ListToString<T>(List<T> list)
    {
        string v = "";
        foreach(T t in list)
        {
            v += t.ToString() + ",";
        }
        return v;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
