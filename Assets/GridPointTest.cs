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
        if (p.HasSettlement)
        {
            Button = GetComponent<Button>();
            isActive = true;
            ColorBlock b = Button.colors;
            

            Color c = Color.magenta;
            if (p.SettlementShell.Type == SettlementType.CITY)
                c = Color.red;
            else if (p.SettlementShell.Type == SettlementType.TOWN)
                c = Color.green;
            else if (p.SettlementShell.Type == SettlementType.VILLAGE)
                c = Color.yellow;

            Text.text = p.SettlementShell.Type.ToString();

            b.normalColor = c;
            Button.colors = b;
        }
        else if(p.ChunkStructure != null)
        {
            Button = GetComponent<Button>();

            ColorBlock b = Button.colors;

            isActive = true;
            Color c = Color.magenta;
            if (p.ChunkStructure is BanditCamp)
            {
                c = Color.grey;
                Text.text = "Bandit";
            }             

            b.normalColor = c;
            Button.colors = b;
        }
        else
        {
           // this.gameObject.SetActive(false);
        }

        if (p.HasSet)
        {
            isActive = true;
            Button = GetComponent<Button>();

            ColorBlock b = Button.colors;


            Color c = Color.magenta;
            if (p.SETYPE == SettlementType.CITY)
                c = Color.red;
            else if (p.SETYPE == SettlementType.TOWN)
                c = Color.green;
            else if (p.SETYPE == SettlementType.VILLAGE)
                c = Color.yellow;

            //Text.text = p.SettlementShell.Type.ToString();

            b.normalColor = c;
            Button.colors = b;
        }
        if (p.HasTacLoc)
        {
            isActive = true;
            Button = GetComponent<Button>();

            ColorBlock b = Button.colors;


            Color c = Color.magenta;
            if (p.TACTYPE == TacLocType.fort)
            {
                c = Color.blue;
                Text.text = "FORT";
            }else if (p.TACTYPE == TacLocType.tower)
            {
                c = new Color(0.5f, 0, 0.5f);
                Text.text = "TOWER";
            }

            //Text.text = p.SettlementShell.Type.ToString();

            b.normalColor = c;
            Button.colors = b;
        }
       /*
        if(p.Desirability > 0.1f)
        {
            Button = GetComponent<Button>();

            ColorBlock b = Button.colors;

            float r = p.Desirability / 2f;
            Color c = FromDes(p.Desirability);

  


            b.normalColor = c;
            Button.colors = b;
        }*/
        if (p.IsCapital)
        {
            isActive = true;
            Button = GetComponent<Button>();

            ColorBlock b = Button.colors;


            Color c = Color.yellow;

            Text.text = "Cap";


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
