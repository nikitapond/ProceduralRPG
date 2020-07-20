using UnityEngine;
using UnityEditor;
[System.Serializable]
public class BasicNPCTaskAI : EntityTaskAI
{
    private NPC NPC { get { return Entity as NPC; } }
    public BasicNPCTaskAI()
    {
    }

  

    public override EntityTask ChooseIdleTask()
    {
        if (NPC.NPCData.HasJob)
        {
            if (ShouldGoToWork())
            {
                //If not at work, go to work
                if (!IsAtWork())
                {
                    Debug.Log("Entity " + Entity + " task: GoToWork");
                    return new EntityTaskGoto(Entity, NPC.NPCData.NPCJob.WorkLocation.WorkBuilding, taskDesc: "Going to work at " + NPC.NPCData.NPCJob.WorkLocation);
                }
                else
                {
                    //If we are at work, choose idle task
                    Debug.Log("Entity " + Entity + " task: Job");

                    return new NPCTaskDoJob(Entity, NPC.NPCData.NPCJob.WorkLocation, NPC.NPCData.NPCJob, 10, 60);
                }
            }
            else
            {
                //if time in some area
                return new EntityTaskGoto(Entity, NPC.NPCData.House, taskDesc: "Going home");
            }
        }
        else
        {
            //if its day, we wonder
            if (!WorldManager.Instance.Time.IsNight)
            {
                //If no job, we randomly walk around
                Settlement homeSet = NPC.NPCKingdomData.GetSettlement();
                Building target = GenerationRandom.RNG.RandomFromList(homeSet.Buildings);
                return new EntityTaskGoto(Entity, target, taskDesc: "Wondering");
            }
            else
            {
                return new EntityTaskGoto(Entity, NPC.NPCData.House, taskDesc: "Going home");
            }
            
        }
        return null;
    }




    /// <summary>
    /// Returns true if the entity should go to work
    /// TODO - make this time dependent.
    /// </summary>
    /// <returns></returns>
    private bool ShouldGoToWork()
    {
        //Debug.Log(NPC + "_" + Entity);
        //Debug.Log(NPC.NPCData);
        return NPC.NPCData.HasJob && !WorldManager.Instance.Time.IsNight;
    }
    /// <summary>
    /// Returns true if the NPC's current position is within their work building
    /// </summary>
    /// <returns></returns>
    private bool IsAtWork()
    {
        return NPC.NPCData.NPCJob.WorkLocation.WorkBuilding.GetWorldBounds().ContainsPoint(Entity.TilePos);
    }

    public override string ToString()
    {
        return base.ToString();
    }
}