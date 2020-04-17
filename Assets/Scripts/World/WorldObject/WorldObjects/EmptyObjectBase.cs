﻿using UnityEngine;
using UnityEditor;
[System.Serializable]
public class EmptyObjectBase : WorldObjectData, IMultiTileObjectChild
{

    private IMultiTileObject Parent;
    public override WorldObjects ObjID => WorldObjects.EMPTY_OBJECT_BASE;

    public override string Name => "Empty";
    public EmptyObjectBase(Vec2i worldPosition, WorldObjectMetaData meta=null, IMultiTileObject parent=null) : base(worldPosition, meta, null)
    {
        Parent = parent;
    }

    public IMultiTileObject Getparent()
    {
        return Parent;
    }
}