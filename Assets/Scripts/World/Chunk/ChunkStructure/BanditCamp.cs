using UnityEngine;
using UnityEditor;

public class BanditCamp : ChunkStructure
{

    //The number of ticks since the last patrol returned
    private int TicksWithoutPatrol;

    //Bandit camps can only have 1 entity group at a time
    private EntityGroup CurrentEntityGroup;

    public BanditCamp(Vec2i cPos, Vec2i cSize) : base(cPos, cSize)
    {
    }

    public override void Tick()
    {
        //If we don't currently have a patrol, we increase the count
        if(CurrentEntityGroup == null)
        {
            TicksWithoutPatrol++;

            if (ShouldSpawnPatrol())
                SpawnPatrol();

        }
    }

    private void SpawnPatrol()
    {
        Debug.Log("[WorldEvent] Bandit Patrol spawned from " + Position);
        TicksWithoutPatrol = 0;
        CurrentEntityGroup = WorldEventManager.Instance.SpawnBanditPatrol(this, null);
    }



    private bool ShouldSpawnPatrol()
    {

        if (TicksWithoutPatrol < 5)
            return false;
        return true;
        float p = Mathf.Exp(-2.0f / TicksWithoutPatrol);

        if (GenerationRandom.RNG.Random() < p)
            return true;
        return false;

    }

    public override void GroupReturn(EntityGroup group)
    {
        //Add entities back
        CurrentEntityGroup = null;
    }
}