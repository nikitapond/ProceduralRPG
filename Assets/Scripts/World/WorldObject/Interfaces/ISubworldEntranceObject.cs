using UnityEngine;
using UnityEditor;


/// <summary>
/// ISubworldEntraceObjects are objects that, allow the entity
/// that interacts with them to teleport to a new sub world.
/// </summary>
public interface ISubworldEntranceObject
{


    /// <summary>
    /// The key required to enter this sub world.
    /// If null, then no key is required.
    /// </summary>
    /// <returns></returns>
    Key GetSubworldKey();

    /// <summary>
    /// The subworld that this Object allows entrance to.
    /// This must be saved as a local variable
    /// </summary>
    /// <param name="world"></param>
    void SetSubworld(Subworld world);
    /// <summary>
    /// Returns the subworld that 
    /// </summary>
    /// <returns></returns>
    int GetSubworldID();
}
public static class ISubworldEntranceHelper
{
    public static Subworld GetSubworld(this ISubworldEntranceObject swEnt)
    {
        return GameManager.WorldManager.World.GetSubworld(swEnt.GetSubworldID());
    }
}