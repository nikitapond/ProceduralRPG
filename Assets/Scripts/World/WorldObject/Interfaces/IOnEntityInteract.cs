using UnityEngine;
using UnityEditor;

public interface IOnEntityInteract
{
    /// <summary>
    /// Called when an entity interacts with this object
    /// </summary>
    /// <param name="entity"></param>
    void OnEntityInteract(Entity entity);
}