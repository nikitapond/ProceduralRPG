using UnityEngine;
using UnityEditor;

[System.Serializable]
public class SpellFireball : SingleSpell
{
    public override float ManaCost => 20;

    public override string Description => "A simple fire ball spell";

    public override string Name => "Fire Ball";
    public override float CoolDown => 3;

    public override float XPGain => 5;

    public override SpellCombatType SpellCombatType => SpellCombatType.OFFENSIVE;

    public override Spells ID => Spells.FireBall;

    public override void CastSpell(SpellCastData data)
    {
        
        Vector2 look = new Vector2(Mathf.Cos(data.Source.LookAngle * Mathf.Deg2Rad), -Mathf.Sin(data.Source.LookAngle * Mathf.Deg2Rad));

        Vector3 direction = Quaternion.Euler(data.Target - data.SpellSource) * Vector3.forward;

        SpellManager.Instance.AddNewProjectile(data.Source.Position + Vector3.up * 0.8f, direction, new FireBall(), data.Source);
        //GameManager.WorldManager.AddNewProjectile(data.Source.Position + Vector3.up * 0.8f, look, new FireBall(), data.Source);
    }
}