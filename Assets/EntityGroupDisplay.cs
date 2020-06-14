using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class EntityGroupDisplay : MonoBehaviour
{

    private Button Button;
    private Text Text;

    private EntityGroup Group;

    private bool IsMain = true;

    public void SetEntityGroup(EntityGroup c)
    {
        Button = GetComponent<Button>();
        Text = GetComponentInChildren<Text>();
        if (c is EntityGroupCaravan)
            SetCaravan(c as EntityGroupCaravan);
        if(c is EntityGroupVillageTrader)
        {
            switch (c.Type)
            {
                case EntityGroup.GroupType.VillageAnimalExport:
                    Text.text = "Animal";
                    break;
                case EntityGroup.GroupType.VillageFoodExport:
                    Text.text = "Food";
                    break;
                case EntityGroup.GroupType.VillageOreExport:
                    Text.text = "Ore";
                    break;
                case EntityGroup.GroupType.VillageWoodExport:
                    Text.text = "Wood";
                    break;
            }

        }
        ColorBlock b = Button.colors;
        float[] col = new GenerationRandom((int)(Time.time * 2052306)).RandomFloatArray(0, 1, 3);
        b.normalColor = new Color(col[0], col[1], col[2]);
        Button.colors = b;
        Group = c;
        IsMain = false;
    }
    private void SetCaravan(EntityGroupCaravan caravan)
    {
        Text.text = "Ca";

    }

    public void OnClick()
    {
        if (Group == null)
            return;
        if(Group.Type == EntityGroup.GroupType.Traders)
        {
            EntityGroupCaravan car = Group as EntityGroupCaravan;
            if(car != null)
            {
                string info = string.Format("Caravan is travelling from {0} to {1}, with the following trade: \n", car.StartChunk, car.EndChunk);
                // Debug.Log(string.Format("Caravan is travelling from {0} to {1}, with the following trade: ", car.StartChunk, car.EndChunk));

                foreach (KeyValuePair<EconomicItem, int> kvp in car.Trade)
                {
                    info += string.Format("{0} : {1} - £{2}\n", kvp.Key, kvp.Value, kvp.Value * kvp.Key.Value);
                }
                Debug.Log(info);
            }
            EntityGroupVillageTrader farm = Group as EntityGroupVillageTrader;
            if(farm != null)
            {
                string inv = "";
                foreach(KeyValuePair<EconomicItem, int> kvp in farm.EconomicInventory.GetAllItems())
                {
                    inv += string.Format("{0} - {1}\n", kvp.Key, kvp.Value);
                }
                Debug.Log(inv);
            }
            
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsMain && Group == null)
            Destroy(gameObject);
    }
}
