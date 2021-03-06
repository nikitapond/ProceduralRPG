﻿using UnityEngine;
using UnityEditor;
[System.Serializable]
public abstract class Skill
{
    private Skills SkillName;

    protected float SkillXP;
    public Skill(Skills skill)
    {
        SkillName = skill;
    }


    public abstract string Name { get; }


    public void AddXP(float xp)
    {
        SkillXP += xp;
    }

    public float GetXP()
    {
        return SkillXP;
    }
    public int GetLevel()
    {
        return SkillTree.GetLevelFromXP(SkillXP);
    }

    public float GetBonus()
    {
        return Mathf.Sqrt(GetLevel());
    }

}