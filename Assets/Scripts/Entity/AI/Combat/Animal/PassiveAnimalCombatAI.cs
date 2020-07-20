using UnityEngine;
using UnityEditor;

[System.Serializable]
public class PassiveAnimalCombatAI : EntityCombatAI
{
    public override void OnDealDamage(Entity source)
    {


        Vec2i runPos = Entity.TilePos + GameManager.RNG.RandomVec2i(10, 20) * GameManager.RNG.RandomSign();
        Entity.EntityAI?.TaskAI.SetTask(new EntityTaskGoto(Entity, runPos, priority: 10, running: true, taskDesc:"Running from combat"));

    }

    public override void Update()
    {
    }

    public override void WorldCombatEvent(WorldCombat wce)
    {
        //Entity is passive, we do nothing on comat events
    }

    /// <summary>
    /// No weapon for a passive animal sadly.
    /// </summary>
    protected override void ChooseEquiptWeapon()
    {

    }

    protected override bool ShouldCombat(Entity entity)
    {
        return false;
    }

    protected override bool ShouldRun(Entity entity)
    {
        //We always run
        return true;
    }
}