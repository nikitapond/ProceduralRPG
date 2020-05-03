using UnityEngine;
using UnityEditor;

/// <summary>
/// Used for objects that can be used at a work place.
/// Object simply defines the relative position to the object
/// that the entity must be at to work
/// </summary>
public interface IWorkEquiptmentObject
{

    Entity CurrentUser { get; set; }
    Vector3 DeltaPosition { get; }
}
public static class IWorkEquiptmentHelper
{
    public static Vector3 WorkPosition(this IWorkEquiptmentObject obj)
    {
        WorldObjectData objDat = obj as WorldObjectData;
        float x = obj.DeltaPosition.x * Mathf.Cos(objDat.Rotation * Mathf.Deg2Rad) 
                + obj.DeltaPosition.z * Mathf.Sin(objDat.Rotation * Mathf.Deg2Rad);

        float z = -obj.DeltaPosition.x * Mathf.Sin(objDat.Rotation * Mathf.Deg2Rad) 
                + obj.DeltaPosition.z * Mathf.Cos(objDat.Rotation * Mathf.Deg2Rad);

        return objDat.Position + new Vector3(x, obj.DeltaPosition.y, z);
    }
}