using UnityEngine;
using UnityEditor;

/// <summary>
/// The standard Combat AI for the average NPC.
/// Will attack only when attacked, or if seeing a friend of theirs attacked.
/// </summary>
public class NonAggresiveNPCCombatAI : EntityCombatAI
{
    public NonAggresiveNPCCombatAI()
    {
    }
    
    public NPC NPC { get { return Entity as NPC; } }

    public override void OnDealDamage(Entity source)
    {
        //We check if we can see the entity.
        if (!CanSeeEntity(source))
        {
            RunFromCombat(source.TilePos);
           // Entity.EntityAI.TaskAI.SetTask(new EntityTaskLookForDamageSource(Entity, source, 15), false);
            return;
        }

        Debug.Log("Entity " + Entity + " delt damage by Entity " + source);
        float aggr = NPC.EntityRelationshipManager.Personality.Aggression;
        if(aggr > 0.8f)
        {
            Entity.GetLoadedEntity().SpeechBubble.PushMessage("Ouch! Aggressive -> attacking " + source);
            Debug.Log("Entity " + Entity + " has high aggression - Start Combat");
            //If our aggression is high, we fight
            EntityManager.Instance.NewCombatEvent(source, Entity);
        }
        if (GameManager.RNG.PercentageChance(60 * aggr))
        {
            Entity.GetLoadedEntity().SpeechBubble.PushMessage("Ouch! Med Aggr + RNG -> attacking " + source);

            Debug.Log("Entity " + Entity + " has medium aggression & RNG - Start Combat");
            EntityManager.Instance.NewCombatEvent(source, Entity);
        }
        if (aggr < 0.3f)
        {
            Debug.Log("Entity " + Entity + " is a lil' pus pus, run from combat");
            Entity.GetLoadedEntity().SpeechBubble.PushMessage("Ouch! running from combat source " + source);

            RunFromCombat(source.TilePos);
            return;
            //if our aggression is low, we run
            Vec2i goTo = GameManager.RNG.RandomVec2i(-20, 20);
            while (goTo.QuickDistance(new Vec2i(0, 0)) < 10 * 10) goTo = GameManager.RNG.RandomVec2i(-20, 20);
            Entity.EntityAI?.TaskAI?.SetTask(new EntityTaskGoto(Entity, goTo + Entity.TilePos, running: true, priority: 50));
        }        
    }

    public override void WorldCombatEvent(WorldCombat wce)
    {
        //if we are already part of the combat event, we have no reaction.
        if (wce.IsParticipant(Entity) || CurrentCombatEvent == wce)
            return;

        //if we are far, ignore
        if (wce.Position.QuickDistance(Entity.TilePos) > World.ChunkSize * World.ChunkSize * 4)
            return;

        if(CurrentCombatEvent == null || CurrentCombatEvent.IsComplete)
        {
            CurrentCombatEvent = wce;
            //If we are in the same faction as either of the sides, join accordingly
            if(Entity.EntityFaction != null && Entity.EntityFaction.Equals(wce.Faction1))
            {
                Debug.Log("[WorldCombatEvent] Entity " + Entity + " is close to new combat event, part of Faction1 - joining combat");
                wce.Team1.Add(Entity);
                CurrentCombatEvent = wce;
                CurrentTarget = wce.GetNearestTeam2Entity(Entity);
                Entity.GetLoadedEntity().SpeechBubble.PushMessage("Seen combat event, in same faction. Joining team 1");
                return;
            }else if(Entity.EntityFaction != null && Entity.EntityFaction.Equals(wce.Faction2))
            {
                Debug.Log("[WorldCombatEvent] Entity " + Entity + " is close to new combat event, part of Faction2 - joining combat");
                Entity.GetLoadedEntity().SpeechBubble.PushMessage("Seen combat event, in same faction. Joining team 2");
                wce.Team2.Add(Entity);
                CurrentCombatEvent = wce;
                CurrentTarget = wce.GetNearestTeam1Entity(Entity);
                return;
            }
            //if we are not in a faction/not related to their factions, we check friendships/family relations
            float team1RelVal = 0;
            bool team1High = false;

            float team2RelVal = 0;
            bool team2High = false;

            foreach (Entity e in wce.Team1)
            {
                float eRelVal = NPC.EntityRelationshipManager.GetEntityRelationship(e);
                //If we have a close friend/family member, take note
                if (eRelVal > 0.7f / NPC.EntityRelationshipManager.Personality.Loyalty)
                    team1High = true;
                team1RelVal += eRelVal;
            }
            
            foreach (Entity e in wce.Team2)
            {
                float eRelVal = NPC.EntityRelationshipManager.GetEntityRelationship(e);
                //If we have a close friend/family member, take note
                if (eRelVal > 0.7f/NPC.EntityRelationshipManager.Personality.Loyalty)
                    team2High = true;
                team2RelVal += eRelVal;
            }

            //If we have a close friend or family member, (Currently) do nothing.
            //TODO - add something here? Who knows?
            if (team1High && team2High)
            {
                Debug.Log("[WorldCombatEvent] Entity " + Entity + " is is high relationship with both factions - do nothing");
                Entity.GetLoadedEntity().SpeechBubble.PushMessage("High relationship with all in combat event, no combat");
                RunFromCombat(wce.Position);
                return;
            }
            //If we have a family/friend on team 1, we join the combat with team 1.
            if (team1High)
            {
                //We join their team, then choose a target to fight
                wce.Team1.Add(Entity);
                CurrentCombatEvent = wce;
                CurrentTarget = wce.GetNearestTeam2Entity(Entity);
                Debug.Log("[WorldCombatEvent] Entity " + Entity + " is close to new combat event, friend with team 1 - joining combat");
                Entity.GetLoadedEntity().SpeechBubble.PushMessage("Friend on team 1, joining");


            }
            else if (team2High)
            {
                //We join their team, then choose a target to fight
                wce.Team2.Add(Entity);
                CurrentCombatEvent = wce;
                CurrentTarget = wce.GetNearestTeam1Entity(Entity);
                Debug.Log("[WorldCombatEvent] Entity " + Entity + " is close to new combat event, friend with team 2 - joining combat");
                Entity.GetLoadedEntity().SpeechBubble.PushMessage("Friend on team 2, joining");


            }
            else
            {
                //Check if aggression is high against RNG
                if (NPC.EntityRelationshipManager.Personality.Aggression > GameManager.RNG.Random(0.6f, 0.8f))
                {
                    //Agression is between 0 and 1, we divide the team2 relationship value by this.
                    //And check against team1relval. This means higher agression requires less difference
                    //We also divide team2relval by the entities loyatly. This means that an entity with low loyalty requires a higher difference
                    if (team1RelVal > team2RelVal / (NPC.EntityRelationshipManager.Personality.Loyalty * NPC.EntityRelationshipManager.Personality.Aggression))
                    {
                        Debug.Log("[WorldCombatEvent] Entity " + Entity + " has high aggression - join team 1");

                        wce.Team1.Add(Entity);
                        CurrentCombatEvent = wce;
                        CurrentTarget = wce.GetNearestTeam2Entity(Entity);
                        Entity.GetLoadedEntity().SpeechBubble.PushMessage("High aggression (" + NPC.EntityRelationshipManager.Personality.Aggression + ")" +
                            "higher relationship with team 1, joining");

                        return;
                    }
                    else if (team2RelVal < team1RelVal / (NPC.EntityRelationshipManager.Personality.Loyalty * NPC.EntityRelationshipManager.Personality.Aggression))
                    {
                        wce.Team2.Add(Entity);
                        CurrentCombatEvent = wce;
                        CurrentTarget = wce.GetNearestTeam1Entity(Entity);
                        Debug.Log("[WorldCombatEvent] Entity " + Entity + " has high aggression - join team 2");
                        Entity.GetLoadedEntity().SpeechBubble.PushMessage("High aggression (" + NPC.EntityRelationshipManager.Personality.Aggression + ")" +
                            "higher relationship with team 2, joining");
                        return;
                    }


                }
                else
                {
                    Debug.Log("[WorldCombatEvent] Entity " + Entity + " is running from combat event");
                    Entity.GetLoadedEntity().SpeechBubble.PushMessage("Running from combat event");
                    RunFromCombat(wce.Position);
                    /*
                    Vec2i runPos = Entity.TilePos + GameManager.RNG.RandomVec2i(10, 20) * GameManager.RNG.RandomSign();
                    Entity.EntityAI?.TaskAI.SetTask(new EntityTaskGoto(Entity, runPos, priority: 10, running: true));*/
                }//if our agression is low, we check for 
                //If neither team has a friend/family, we 

            }



        }       

    }


    public override string ToString()
    {
        return base.ToString();
    }
    protected override void ChooseEquiptWeapon()
    {
    }
    /// <summary>
    /// Is called when this entity can see the other entity.
    /// Only time combat should occur is if there is a sever hatred 
    /// 
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    protected override bool ShouldCombat(Entity entity)
    {

        float rel = NPC.EntityRelationshipManager.GetEntityRelationship(entity);
        float aggr = NPC.EntityRelationshipManager.Personality.Aggression;
        if (rel * (1.7-aggr) < 0.1f)
        {
            Entity.GetLoadedEntity().SpeechBubble.PushMessage("Low rel " + rel + " & high aggr " + aggr + " - attacking " + entity);
            Debug.Log("[CombatAI] Entity " + Entity + " has seen " + entity + " and will enter combat. Relationship value: " + rel + ", Aggression: " + aggr);
            return true;
        }
            

        return false;
    }

    protected override bool ShouldRun(Entity entity)
    {
        return false;
    }
}