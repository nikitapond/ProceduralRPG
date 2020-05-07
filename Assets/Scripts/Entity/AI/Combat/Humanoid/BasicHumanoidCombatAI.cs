using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
[System.Serializable]
public class BasicHumanoidCombatAI : EntityCombatAI
{




   




    protected override bool ShouldCombat(Entity entity)
    {
        if (entity is Player)
            return true;

        return false;
    }
    
    protected override bool ShouldRun(Entity entity)
    {
        //If we are on less that 10% health, we should run
        if (Entity.CombatManager.CurrentHealth / Entity.CombatManager.MaxHealth < 0.1f)
            return true;
        return false;
    }


  



    public override void WorldCombatEvent(WorldCombat wce)
    {
        if(CurrentCombatEvent == null || CurrentCombatEvent.IsComplete)
        {
            //If the combat event faction1 is the same as this entities faction, join the combat
            if (wce.Faction1 != null && wce.Faction1.Equals(Entity.EntityFaction))
            {
                wce.Team1.Add(Entity);
                CurrentTarget = GameManager.RNG.RandomFromList(wce.Team2);
                CurrentCombatEvent = wce;

            }
            else //If the combat event faction1 is the same as this entities faction, join the combat
            if (wce.Faction2 != null && wce.Faction2.Equals(Entity.EntityFaction))
            {
                wce.Team2.Add(Entity);
                CurrentTarget = GameManager.RNG.RandomFromList(wce.Team1);
                CurrentCombatEvent = wce;
            }
        }


    }

    /// <summary>
    /// Check if all equiptment is valid
    /// </summary>
    public override void CheckEquiptment()
    {
      
    }

    protected override void ChooseEquiptWeapon()
    {
        Vector2 combatDisp = CurrentTarget.Position2 - Entity.Position2;
        float distance = combatDisp.magnitude;
        EquiptmentManager eqMan = (Entity as HumanoidEntity).EquiptmentManager;
        Item currentlyEquipt = eqMan.GetEquiptItem(LoadedEquiptmentPlacement.weaponHand);
        //If we are unarmed
        if (currentlyEquipt == null)
        {
            //Check distance, if large distance & we have a range weapon, equipt it
            if (distance > 8 && eqMan.HasRangeWeapon())
                eqMan.EquiptRangeWeapon();
            //If we are either close, or we have no range weapon, check for range weapon
            else if (eqMan.HasMeleeWeapon())
                eqMan.EquiptMeleeWeapon();
        }else if(currentlyEquipt is RangeWeapon)
        {//If the currently equipt weapon is a range weapon
            //If we are close to the enemy, try to equipt a melee weapon
            if (distance < 5 && eqMan.HasMeleeWeapon())
                eqMan.EquiptMeleeWeapon();
        }
        else if(currentlyEquipt is Weapon)
        {//If we currently have a melee weapon
            if (distance > 10 && eqMan.HasRangeWeapon())
                eqMan.EquiptRangeWeapon();
        }

    }

    public override string ToString()
    {
        return base.ToString();
    }

    public override void OnDealDamage(Entity source)
    {

    }
}