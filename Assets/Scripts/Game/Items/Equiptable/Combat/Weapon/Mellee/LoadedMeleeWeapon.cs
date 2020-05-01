using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadedMeleeWeapon : MonoBehaviour
{

    private Collider Collider;
    private bool IsSwinging;

    private Entity Entity;
    private Weapon Weapon;

    private float WeaponBaseDamage;
    private float WeaponAttackTime;
    private DamageType DamageType;

    private float SwingDamage;
    /// <summary>
    /// Called when the melee weapon is loaded in.
    /// Sets up the colliders required
    /// </summary>
    private void Awake()
    {
        Collider = GetComponent<Collider>();
        if (Collider == null)
            Collider = GetComponentInChildren<Collider>();
        if(Collider == null)
        {
            Collider = gameObject.AddComponent<MeshCollider>();
            (Collider as MeshCollider).sharedMesh= GetComponentInChildren<MeshFilter>().mesh;

        }else if(Collider is MeshCollider)
        {
            MeshCollider mc = Collider as MeshCollider;
            if(mc.sharedMesh == null)
                mc.sharedMesh = GetComponentInChildren<MeshFilter>().mesh;
        }
    }

    /// <summary>
    /// Sets the LoadedMeleeWeapon details based on the weapon itself 
    /// and the entity using it.
    /// If the supplied weapon is null, the LoadedMeleeWeapon represents the 
    /// unarmed attack. in this case, the collider should be at the hand/fist of the
    /// entity.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="weapon"></param>
    public void SetWeaponDetails(Entity parent, Weapon weapon)
    {
        //Set simple values
        Entity = parent;
        Weapon = weapon;
        //if the weapon is null, set up from unarmed valuees
        if(weapon == null)
        {
            WeaponBaseDamage = Entity.CombatManager.UnarmedAttackDamage;
            WeaponAttackTime = Entity.CombatManager.UnarmedAttackTime*0.9f;
            DamageType = DamageType.BLUNT;
        }
        else
        {
            WeaponBaseDamage = Weapon.Damage;
            WeaponAttackTime = Weapon.WeaponAttackTime*0.9f;
            DamageType = Weapon.DamageType;
        }
    }



    /// <summary>
    /// Triggers the internal logic for swinging a weapon
    /// This is required as no damage is done if the weapon collides
    /// with an entity while it is not swinging.
    /// This function sets IsSwinging to true, and starts a Coroutine (<see cref="InternalSwing"/>) that waits for
    /// the weapon to stop swinging, before setting IsSwinging to false.
    /// 
    /// </summary>
    public void SwingWeapon(float damage=-1)
    {
        Debug.Log("swing!");
        IsSwinging = true;
        if (damage == -1)
        {
            SwingDamage = WeaponBaseDamage;
        }            
        else
            SwingDamage = damage;
        StartCoroutine(InternalSwing());
    }
    /// <summary>
    /// Waits for the weapon swing to finish before setting IsSwinging to false
    /// </summary>
    /// <returns></returns>
    private IEnumerator InternalSwing()
    {
        yield return new WaitForSeconds(WeaponAttackTime);
        IsSwinging = false;
    }

    /// <summary>
    /// Called when another collider intersects with the weapon collider.
    /// If the weapon isn't swinging, no damage is dealt
    /// Checks if the collision is with an entity, if so, checks if the entity
    /// is the parent entity. In this case, no damage is dealt.
    /// Elsewise, we deal the damage amount to the entity via <see cref="EntityCombatManager.DealDamage(float, DamageType, Entity, object[])"/>
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        //We only check for damage collision if we are currently swining this weapon
        if (!IsSwinging)
            return;
        //Check if the object we collide with is an entity
        LoadedEntity le = other.gameObject.GetComponent<LoadedEntity>();
        if (le == null)
            return;


        if (le.Entity == Entity)
            return;
        
        le.Entity.CombatManager.DealDamage(SwingDamage, DamageType, Entity);
        le.Entity.EntityAI?.CombatAI?.OnDealDamage(Entity);
        GameManager.EventManager.InvokeNewEvent(new WorldCombat(Entity, le.Entity));
    }


    
    private void OnCollisionEnter(Collision collision)
    {
        //We only check for damage collision if we are currently swining this weapon
        if (!IsSwinging)
            return;
        //Check if the object we collide with is an entity
        LoadedEntity le = collision.gameObject.GetComponent<LoadedEntity>();
        if (le == null)
            return;


        if (le.Entity == Entity)
            return;

        le.Entity.CombatManager.DealDamage(SwingDamage, DamageType, Entity);
    }

}
