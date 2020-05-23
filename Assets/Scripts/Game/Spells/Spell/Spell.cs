using UnityEngine;
using UnityEditor;

[System.Serializable]
public abstract class Spell
{
    public abstract float ManaCost { get; }
    public abstract Spells ID { get; }
    public abstract float XPGain { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }

    public abstract SpellCombatType SpellCombatType { get; }
}

public enum Spells
{
    FireBall, FireBreath, StoneWall,
}

public enum SpellCombatType
{
    OFFENSIVE, DEFENSIVE, PASSIVE
}
[System.Serializable]
public abstract class SingleSpell : Spell
{
    public abstract float CoolDown { get; }
    public abstract void CastSpell(SpellCastData data);

}
[System.Serializable]
public abstract class HoldSpell : Spell
{

    public bool IsCast{ get; protected set; }


    public abstract void SpellStart(SpellCastData data);
    public abstract void SpellUpdate(SpellCastData data);
    public abstract void SpellEnd(SpellCastData data);
}
public struct SpellCastData
{
    public Entity Source;
    public Vector3 SpellSource;
    public Vector3 Target;
}
