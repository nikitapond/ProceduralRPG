using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class IngameGUI : MonoBehaviour
{
    public Text NPCText;

    public Image HealthBar;
    public Image ManaBar;
    public Image StaminaBar;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        EntityCombatManager playerCombat = PlayerManager.Instance.Player.CombatManager;
        float healthPCT = playerCombat.CurrentHealth / playerCombat.MaxHealth;
        float manaPCT = playerCombat.EntitySpellManager.CurrentMana / playerCombat.EntitySpellManager.MaxMana;
        float staminaPCT = playerCombat.CurrentStamina / playerCombat.MaxStamina;
        HealthBar.fillAmount = healthPCT;
        ManaBar.fillAmount = manaPCT;
        StaminaBar.fillAmount = staminaPCT;
    }


    public void SetNPC(NPC npc)
    {
        if(npc == null)
        {
            NPCText.text = "";
            NPCText.transform.parent.gameObject.SetActive(false);
        }
        else
        {
            NPCText.transform.parent.gameObject.SetActive(true);
            NPCText.text = "";
            NPCText.text += npc.Name + "\n";
            NPCText.text += npc.EntityRelationshipManager.Personality.ToString();
            NPCText.text += "Subworld?: " + npc.GetSubworld();
        }
    }
}
