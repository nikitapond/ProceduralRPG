using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// Manager that belongs to each instance of entity.
/// Holds data on current health, mana, and stamina.
/// Manages combat effects
/// Manages end attack damage based on attack type and armor.
/// </summary>
[System.Serializable]

public class EntityCombatManager : IGamePauseEvent
{

    public Entity Entity { get; private set; }

    private LoadedEntityAnimationManager GetAnimationManager()
    {
        return  Entity.GetLoadedEntity().AnimationManager;
    }


    /// <summary>
    /// All values of Max, Current, and regen rate of health, mana, and stamina
    /// </summary>
    public float MaxHealth { get; private set; }
    public float CurrentHealth { get; private set; }
    public float HealthRegenerationRate { get; private set; }


    public float MaxStamina { get; private set; }
    public float CurrentStamina { get; private set; }
    public float StaminaRegenerationRate { get; private set; }

    public float UnarmedAttackDamage { get; private set; }
    public float UnarmedAttackRange { get; private set; }
    public float UnarmedAttackStaminaUse { get; private set; }
    public float UnarmedAttackCooldown { get; private set; }

    public float UnarmedAttackTime { get; private set; }

    private Weapon EquiptWeapon;

    private Armor[] EquiptArmor;


    [System.NonSerialized]
    private Stopwatch stopwatch;

    [System.NonSerialized]
    private LoadedMeleeWeapon LoadedMeleeWeapon;

    public EntitySpellManager EntitySpellManager { get; private set; }

    public EntityCombatManager(Entity entity)
    {
        Entity = entity;


        MaxHealth = CurrentHealth = 100;
        HealthRegenerationRate = 5;

        MaxStamina = CurrentStamina = 100;
        StaminaRegenerationRate = 20;
        UnarmedAttackRange = 3;
        UnarmedAttackDamage = 5;
        UnarmedAttackCooldown=2;
        UnarmedAttackTime = 1;

        stopwatch = new Stopwatch();
        stopwatch.Start();

        EventManager.Instance.AddListener(this);

        EntitySpellManager = new EntitySpellManager(entity);
        

    }

    /// <summary>
    /// Called when an entity is spawned, ensures all stats are at max values
    /// </summary>
    public void Reset()
    {
        CurrentHealth = MaxHealth;
    }

    /// <summary>
    /// Returns the range of the currently equipt weapon.
    /// If unarmed, return <see cref="EntityCombatManager.UnarmedAttackRange"/>
    /// </summary>
    /// <returns></returns>
    public float GetCurrentWeaponRange()
    {
        return EquiptWeapon == null ? UnarmedAttackRange : EquiptWeapon.WeaponRange;
    }



    public float GetCurrentWeaponCooldown()
    {
        return EquiptWeapon == null ? UnarmedAttackCooldown : EquiptWeapon.WeaponCooldown;
    }

    /// <summary>
    /// Calculates the current remaining cool down by subtracting the equipt weapon cool down from 
    /// the elapsed time since the last attack
    /// </summary>
    /// <returns></returns>
    public float GetCurrentWeaponCooldownRemaining()
    {
        return Mathf.Clamp((GetCurrentWeaponCooldown() * 1000-stopwatch.ElapsedMilliseconds)/1000f, 0, float.MaxValue);
    }

    public float GetCurrentWeaponAttackTime()
    {
        return EquiptWeapon == null ? UnarmedAttackTime : EquiptWeapon.WeaponAttackTime;

    }

    /// <summary>
    /// Update tick for the entity. 
    /// Time is in seconds
    /// </summary>
    /// <param name="time"></param>
    public void Tick(float dt)
    {
        if (GameManager.Paused)
            return;
        //Apply regen rate for health, mana, and stamina.
        CurrentHealth = Mathf.Clamp(CurrentHealth + HealthRegenerationRate * dt, 0, MaxHealth);
        //Debug.Log(CurrentHealth + "_" + HealthRegenerationRate * dt);
        CurrentStamina = Mathf.Clamp(CurrentStamina + StaminaRegenerationRate * dt, 0, MaxStamina);
        //Debug.Log(CurrentStamina + "_" + StaminaRegenerationRate * dt);

        EntitySpellManager.Tick(dt);
    }

    public void SetEquiptWeapon(Weapon weapon)
    {
        EquiptWeapon = weapon;
    }

    public void SetLoadedMeleeWeapon(LoadedMeleeWeapon lmw)
    {
        LoadedMeleeWeapon = lmw;
    }

    public Weapon GetEquiptWeapon()
    {
        return EquiptWeapon;
    }




    public void SetEquiptArmor(Armor[] armor)
    {
        EquiptArmor = armor;
    }



    public bool IsAttacking()
    {
        return stopwatch.ElapsedMilliseconds < GetCurrentWeaponAttackTime() * 1000;
    }


    /// <summary>
    /// Checks if this entity is able to attack the target.
    /// First checks if the currently equipt weapon is still in cooldown <see cref="WeaponController.CanUseWeapon"/>
    /// Checks distance between two entities, and then checks if the range
    /// of the equipt weapon (or unarmed range) is close enough to attack
    /// Checks the time since the last attack
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool CanAttack(Entity target)
    {
        //Check weapon cooldown and stamina amount
        if (!CanAttack())
            return false;

        float distance = Vector3.Distance(target.Position, Entity.Position);

        //Check if unarmed
        if (EquiptWeapon == null)
            return distance < UnarmedAttackRange;
        return distance < EquiptWeapon.WeaponRange;
    }
    /// <summary>
    /// Checks if weapon cool down is done, and if we have enough stamina
    /// </summary>
    /// <returns></returns>
    public bool CanAttack()
    {
        if (stopwatch.ElapsedMilliseconds < GetCurrentWeaponCooldown() * 1000)
        {
            return false;

        }
        if (EquiptWeapon == null)
            return CurrentStamina > UnarmedAttackStaminaUse;
        return CurrentStamina > EquiptWeapon.AttackStaminaUse;
    }
    /// <summary>
    /// Only to be used when CanAttack has been checked.
    /// Creates a new damage region based on the currently equipt weapon and the enity look direction.
    /// 
    /// </summary>
    public void UseEquiptWeapon()
    {
      

        
        stopwatch.Restart();
        //Find damage absed on either unarmed 
        float damage = UnarmedAttackDamage;
        DamageType damageType = DamageType.BLUNT;
        float staminaUse = UnarmedAttackStaminaUse;
        //If weapon is not null, apply correct damage
        if (EquiptWeapon != null)
        {
            damage = EquiptWeapon.Damage;
            damageType = EquiptWeapon.DamageType;
            staminaUse = EquiptWeapon.AttackStaminaUse;

            if(damageType == DamageType.BLUNT)
            {
                damage *= Entity.SkillTree.BluntWeapons.GetBonus();
            }else if(damageType == DamageType.SHARP)
            {
                damage *= Entity.SkillTree.SharpWeapons.GetBonus();
            }

        }

        CurrentStamina -= staminaUse;
        if (EquiptWeapon is RangeWeapon)
        {
            RangeWeapon rw = EquiptWeapon as RangeWeapon;
            SpellManager.Instance.AddNewProjectile(Entity.Position, EntityLookAngleToEulerLook2D(Entity.LookAngle), rw.GenerateProjectile(), Entity, Entity.LookAngle);
            //GameManager.WorldManager.AddNewProjectile(Entity.Position, EntityLookAngleToEulerLook2D(Entity.LookAngle), rw.GenerateProjectile(), Entity, Entity.LookAngle);
        }
        else
        {
            //Instruct animation manager to play attack animation
            Entity.GetLoadedEntity().AnimationManager.HumanoidCast().AttackLeft();

            //Calculate the final dealth damage, based on entity skill tree modifiers and the weapons base damage.


            LoadedMeleeWeapon?.SwingWeapon(damage);

        }
    }

    

    public void DealDamage(float damage, DamageType type, Entity source=null, object[] args=null)
    {
        Debug.Log("Test here");
        CurrentHealth -= CalculateDamageValue(damage, type);
        if (CurrentHealth <= 0)
        {
            Entity.Kill();
            
            return;
        }
        if (args != null)
        {
            //Iterate each one and apply.
            foreach (object arg in args)
            {
                if (arg is DamageRegionArgKockback)
                    ApplyKnockback(arg as DamageRegionArgKockback);
                if (arg is DamageRegionArgBurning)
                {
                    //TODO add burning effect.
                }
            }
        }
    }

    /// <summary>
    /// Calculates the final damage dealt to entity after taking into account
    /// defencive score from armour, as well as active effects and spells (TODO)
    /// </summary>
    /// <param name="baseDamage"></param>
    /// <param name="damageType"></param>
    /// <returns></returns>
    public float CalculateDamageValue(float baseDamage, DamageType damageType)
    {
        if(Entity is HumanoidEntity)
        {


            HumanoidEntity hum = Entity as HumanoidEntity;
            float armourVal = hum.EquiptmentManager.GetArmourValue();
            if (armourVal <= 1)
                return baseDamage;
            return baseDamage/Mathf.Sqrt(armourVal);

        }
        return baseDamage;
    }
    /// <summary>
    /// Applies a knockback to the entity 
    /// </summary>
    /// <param name="knock"></param>
    protected void ApplyKnockback(DamageRegionArgKockback knock)
    {
        Vector2 normDir = knock.Direction.normalized;
        Vector3 forceDir = new Vector3(normDir.x, 0.5f, normDir.y).normalized;
        Entity.GetLoadedEntity().RigidBody.AddForce(forceDir * knock.Force);
    }



    public static Vector3 EntityLookAngleToEulerLook(float LookAngle)
    {
        return new Vector3(Mathf.Cos(LookAngle * Mathf.Deg2Rad),0, -Mathf.Sin(LookAngle * Mathf.Deg2Rad));
        //return new Vector3(Mathf.Sin(look * Mathf.Deg2Rad), 0, Mathf.Cos(look * Mathf.Deg2Rad));
    }
    public static Vector2 EntityLookAngleToEulerLook2D(float LookAngle)
    {
        return new Vector2(Mathf.Cos(LookAngle * Mathf.Deg2Rad), -Mathf.Sin(LookAngle * Mathf.Deg2Rad));

    }

    


    public void GamePauseEvent(bool pause)
    {
        if (pause)
            stopwatch.Stop();
        else
            stopwatch.Start();
        
    }

    /// <summary>
    /// TODO:
    /// Calculates the total combat strength of this entity
    /// Takes into account combat skills, weapons, and armour 
    /// </summary>
    /// <returns></returns>
    public int CalculateEntityCombatStrength()
    {
        return 1;
    }
}