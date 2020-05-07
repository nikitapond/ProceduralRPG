using UnityEngine;
using UnityEditor;

/// <summary>
/// The simplet task AI. Used for creatures/animals.
/// They will simply wonder and idle until they notice other entities
/// </summary>
[System.Serializable]
public class CreatureTaskAI : EntityTaskAI
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override EntityTask ChooseIdleTask()
    {
        return null;
    }

    public override string ToString()
    {
        return base.ToString();
    }

}