using UnityEngine;
using UnityEditor;
[System.Serializable]
public class BasicNPCTaskAI : EntityTaskAI
{
    private NPC NPC { get { return Entity as NPC; } }
    public BasicNPCTaskAI()
    {
    }

    /*
    public override void Tick()
    {
        Vec2i entPos = Vec2i.FromVector3(Entity.Position);
        //Check for job
        if (NPC.NPCData.HasJob)
        {
            NPCJob job = NPC.NPCData.NPCJob;
            Building b = job.WorkLocation.WorkBuilding;
            //Debug.Log("[NPCAI] Entity " + NPC.Name + " going to work at " + b.Entrance);         
            if (b != null)
            {
                //If we are inside work
                if (b.GetWorldBounds().ContainsPoint(entPos))
                {
                    //Debug.Log("[NPCAI] Entity " + NPC.Name + " at work");
                    return;

                }
                //If we are far from the player, we can teleport to work
                if (Vec2i.FromVector3(GameManager.PlayerManager.Player.Position).QuickDistance(Vec2i.FromVector3(Entity.Position)) > (16 * 3) * (16 * 3))
                {
                    NPC.SetPosition(GameManager.RNG.RandomFromList(b.GetSpawnableTiles()));
                    //Debug.Log("[NPCAI] Far from player, teleporting");

                }
                else if (Entity.EntityAI.GeneratePath(b.Entrance))
                {
                    Entity.EntityAI.FollowPath();
                    Debug.Log("[NPCAI] Following path");
                }
                else
                {
                    Debug.Log("[NPCAI] No path found");
                }
            }
        }
        
        return;
        
    }
    */

    public override EntityTask ChooseIdleTask()
    {
        if (ShouldGoToWork())
        {

            //If not at work, go to work
            if (!IsAtWork())
            {
                Debug.Log("Entity " + Entity + " task: GoToWork");
                return new EntityTaskGoto(Entity, NPC.NPCData.NPCJob.WorkLocation.WorkBuilding);
            }
            else {
                //If we are at work, choose idle task
                Debug.Log("Entity " + Entity + " task: Job");

                return new NPCTaskDoJob(Entity, NPC.NPCData.NPCJob.WorkLocation, 10, 60);
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
        return NPC.NPCData.HasJob;
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