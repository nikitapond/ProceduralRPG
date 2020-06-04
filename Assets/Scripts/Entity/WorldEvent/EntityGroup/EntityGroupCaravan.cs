using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class EntityGroupCaravan : EntityGroup
{
    public EntityGroupCaravan(Vec2i startChunk, Vec2i endChunk, List<Entity> entities = null) : base(startChunk, endChunk, entities)
    {
    }

    public override GroupType Type => EntityGroup.GroupType.Traders;

    public override bool DecideNextTile(out Vec2i nextTile)
    {
        CurrentPathIndex++;
        //If we are at the end of the path, we return false. This
        if (CurrentPathIndex == Path.Count)
        {
            nextTile = null;
            return false;
        }

        nextTile = Path[CurrentPathIndex];
        return true;


    }

    public override void OnGroupInteract(EntityGroup other)
    {
        throw new System.NotImplementedException();
    }

    public override void OnReachDestination(Vec2i position)
    {
        GridPoint gp = WorldEventManager.Instance.GridPlacement.GetNearestPoint(position);
        if (!gp.HasSettlement)
        {
            Debug.Error("This shouldn't happen");
            return;
        }



    }
}