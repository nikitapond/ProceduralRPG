using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellsGUI : MonoBehaviour
{

    public GameObject SpellPrefab;

    public GameObject Content;

    void Start()
    {
        
    }

    private void OnEnable()
    {
        Dictionary<Spells, Spell> spells = PlayerManager.Instance.Player.CombatManager.EntitySpellManager.GetAllSpells();

        foreach(KeyValuePair<Spells, Spell> kvp in spells)
        {
            Spell spell = kvp.Value;
            GameObject obj = Instantiate(SpellPrefab);

            obj.transform.SetParent(Content.transform, false);
            obj.GetComponent<SpellGUIButton>().SetSpell(spell);

        }
    }


    private void Clear()
    {
        foreach(Transform t in Content.transform)
        {
            Destroy(t.gameObject);
        }
    }

    private void OnDisable()
    {
        Clear();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
