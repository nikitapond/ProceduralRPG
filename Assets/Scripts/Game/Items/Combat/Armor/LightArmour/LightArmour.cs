﻿using UnityEngine;
using UnityEditor;

public abstract class LightArmour : Armor
{
    public LightArmour(ItemMetaData meta = null) : base(meta)
    {
    }
    public override bool IsEquiptable => true;
    public override string SpriteSheetTag => "weapon"; //TODO - Change (test only)
}