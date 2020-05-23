using UnityEngine;
using UnityEditor;
[System.Serializable]
public class Bandit : HumanoidEntity
{

    public Bandit() :base (new BasicHumanoidCombatAI(), new CreatureTaskAI(), new EntityMovementData(4,8, 4))
    {
        EquiptmentManager.AddDefaultItem(new Trousers(new ItemMetaData().SetColor(Color.blue)));
        EquiptmentManager.AddDefaultItem(new Shirt(new ItemMetaData().SetColor(Color.green)));
        Inventory.AddItem(new SteelLongSword());
    }



    public override string EntityGameObjectSource => "human";

    protected override void KillInternal()
    {
    }
}