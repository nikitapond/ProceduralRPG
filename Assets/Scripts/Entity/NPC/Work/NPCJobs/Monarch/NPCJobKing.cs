using UnityEngine;
using UnityEditor;

public class NPCJobKing : NPCJob
{
    public NPCJobKing(IWorkBuilding workLocation) : base("King", workLocation, KingdomHierarchy.Monarch)
    {
    }

    public override Color GetShirtColor => new Color(1, 1, 0);

    public override void JobTick(NPC npc)
    {
    }

    public override void JobUpdate(NPC npc)
    {
    }
}