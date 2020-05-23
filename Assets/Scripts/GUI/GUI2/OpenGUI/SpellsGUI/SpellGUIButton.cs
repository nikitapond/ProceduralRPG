using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class SpellGUIButton : MonoBehaviour
{
    public TextMeshProUGUI Name;
    public TextMeshProUGUI Description;
    public Image DisplayImage;

    private Spell Spell;

    public void SetSpell(Spell spell)
    {
        Name.text = spell.Name;
        Description.text = spell.Description;
        Spell = spell;
    }


    public void OnClick()
    {
        Debug.Log("Equipting spell");
        PlayerManager.Instance.Player.CombatManager.EntitySpellManager.EquiptSpell(Spell.ID, LoadedEquiptmentPlacement.weaponHand);
    }

}
