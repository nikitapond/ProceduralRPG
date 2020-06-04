using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class GridPointTest : MonoBehaviour
{

    private GridPoint p;
    public void SetPoint(GridPoint p)
    {
        this.p = p;
        if (p.HasSettlement)
        {
            ColorBlock b = GetComponent<Button>().colors;
            

            Color c = Color.magenta;
            if (p.SettlementShell.Type == SettlementType.CITY)
                c = Color.red;
            else if (p.SettlementShell.Type == SettlementType.TOWN)
                c = Color.green;
            else if (p.SettlementShell.Type == SettlementType.VILLAGE)
                c = Color.yellow;

            b.normalColor = c;
            GetComponent<Button>().colors = b;
        }
        else
        {
            this.gameObject.SetActive(false);
        }
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
