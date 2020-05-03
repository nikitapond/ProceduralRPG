using UnityEngine;
using UnityEditor;

public abstract class Animal : Entity
{
    private bool closeEnough;
    public Animal(string name = "un-named_entity", string entityGameObjectSource = "default" ,bool isFixed = false) 
        : base(new PassiveAnimalCombatAI(), new CreatureTaskAI(), new EntityMovementData(), name:name, isFixed: isFixed)
    {

    }


    protected override void KillInternal()
    {
        //If no items, we do nothing
        if (Inventory.GetItems().Count == 0)
            return;
        
        LootSack lootSack = new LootSack(Vec2i.FromVector3(Position).AsVector3());
        foreach(ItemStack it in Inventory.GetItems())
        {
            lootSack.GetInventory().AddItemStack(it);
        }

        Vec2i pos = Vec2i.FromVector3(Position);
        GameManager.WorldManager.AddNewObject(lootSack);

    }
}