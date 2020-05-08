using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
/// <summary>
/// Holds onto data and function srelated to an entities personality,
/// as well as their responses to world events
/// </summary>
public class EntityRelationshipManager
{

    public EntityPersonality Personality { get; private set; }

    private NPC NPC;

    /// <summary>
    /// Contains the relationship this entity feels with another entity.
    /// The key (int) represents the ID of the entity.
    /// The value (float) represents the relationship. 
    /// Value of 1 = strong positive relationship
    /// Value of 0 = strong hate
    /// </summary>
    private Dictionary<int, float> EntityRelationship;
    private Dictionary<int, EntityRelationshipTag> EntityRelationshipTags;
    public EntityRelationshipManager(NPC npc)
    {
        NPC = npc;
        EntityRelationship = new Dictionary<int, float>();
        EntityRelationshipTags = new Dictionary<int, EntityRelationshipTag>();
    }

    public EntityRelationshipManager(NPC npc, EntityPersonality personality)
    {
        NPC = npc;
        Personality = personality;
        EntityRelationship = new Dictionary<int, float>();
        EntityRelationshipTags = new Dictionary<int, EntityRelationshipTag>();

    }

    public void SetPersonality(EntityPersonality pers)
    {
        Personality = pers;
    }



    public void SetRelationshipTag(Entity entity, EntityRelationshipTag tag)
    {
        if (!entity.IsFixed)
        {
            Debug.LogError("[EntityRelationship] Relationship tags can only be set for fixed entities");
            return;
        }
        //Check if the entity is already known.
        //Set the tag
        if (EntityRelationshipTags.ContainsKey(entity.ID))
        {
            EntityRelationshipTags[entity.ID] = tag;
        }
        else
        {
            EntityRelationshipTags.Add(entity.ID, tag);
        }
        //Set the relationship value based on this tag
        SetEntityRelationship(entity, GetRelationshipFromTag(tag));
    }

    /// <summary>
    /// Returns the relationship this entity has with the specified entity param
    /// If the entity is fixed, we attempt to check if a relationship is stored. If not, 
    /// we generate the base relationship. We then store this data.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public float GetEntityRelationship(Entity entity)
    {

        //If the entity is fixed, we can check if we contain its ID in our relationship dict
        if (entity.IsFixed)
        {
            float relation = -1;
            //If we contain this value
            if (EntityRelationship.TryGetValue(entity.ID, out relation))
            {
                return relation;
            }
            //if we do not contain this value, we need to calculate it
            relation = CalculateEntityRelationship(entity);
            EntityRelationship.Add(entity.ID, relation);
            return relation;
        }
        //Non fixed entities have their relationship calculated on the fly
        return CalculateEntityRelationship(entity);
    }

    public void SetEntityRelationship(Entity entity, float val)
    {
        if (!entity.IsFixed)
        {
            Debug.LogError("[EntityRelationship] Can only set relationships for fixed entities");
            return;
        }
        if (EntityRelationship.ContainsKey(entity.ID))
        {
            EntityRelationship[entity.ID] = val;
        }
        else
        {
            EntityRelationship.Add(entity.ID, val);
        }
    }


    public void UpdateEntityRelationship(Entity entity, float dRel)
    {
        if (!entity.IsFixed)
        {
            Debug.LogError("[EntityRelationship] Can only set relationships for fixed entities");
            return;
        }
        //If we know this entity, directly modify
        if (EntityRelationship.ContainsKey(entity.ID))
        {
            EntityRelationship[entity.ID] += dRel;
        }
        else
        {//If not, we calculate the base relationship and then modify accordingly
            EntityRelationship.Add(entity.ID, CalculateEntityRelationship(entity) + dRel);
        }
    }


    /// <summary>
    /// Calculates the initial/default relationship with the specified entity.
    /// 
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    private float CalculateEntityRelationship(Entity entity)
    {
        //Non fixed entities - likely bandits
        if (!entity.IsFixed)
        {
            if (entity is HumanoidEntity)
            {
                if (entity is Bandit)
                {
                    return 0;
                }
            }

            return 0.5f;
        }
        else
        {
            //if the entity is fixed, the initial relationship will 
            //be based on the kingdoms the entities belong to, and the two entity personality

            if (entity is NPC)
            {

                NPC otherNPC = entity as NPC;
                EntityPersonality otherPer = otherNPC.EntityRelationshipManager.Personality;
                float kingdomRelation = 1;
                //Subjuget is a value which is high if otherNPC is above this NPC in the kingdom heirarchy
                //It is 
                float subjugetMult = 1;

                if (NPC.NPCKingdomData.KingdomID == otherNPC.NPCKingdomData.KingdomID)
                {
                    kingdomRelation = 1.5f;

                    //We modify the subjuget multiplier if the otherNPC outranks this one
                    if ((int)NPC.NPCKingdomData.Rank > (int)otherNPC.NPCKingdomData.Rank)
                    {
                        //If this NPCs loyalty is low, then they don't care as much about being outranked
                        if (Personality.Loyalty < 0.4f)
                        {
                            subjugetMult = 0.5f;
                        }
                        else
                        {
                            //High loyalty gives relationship bonus
                            subjugetMult *= (Personality.Loyalty + 0.2f);
                        }
                        if (NPC.NPCKingdomData.SettlementID == otherNPC.NPCKingdomData.SettlementID)
                        {
                            //If the NPCs are in the same settlement, the subjugation multiplier is increased
                            //Due to higher pressure
                            subjugetMult *= 1.4f;
                        }
                    }
                }
                else
                {
                    //TODO - add kingdom relations (how friendly two kingdoms are)
                    //kingdomRelation = Kingdom.GetRelation(otherNPC.NPCKingdomData.KingdomID);
                }

                //If this entity is loyal, then they will respect a +ve kingdom relationship more
                //If they are not loyal, they won't care
                float weightedKingdom = kingdomRelation * Personality.Loyalty;

                //High difference if aggression results in bad relationship
                float agresDifMult = 1 - Mathf.Abs(otherPer.Aggression - Personality.Aggression);

                //Two entities will not like each other if they are opposite wealth
                //Rich hate the poor, and the poor hate the rich
                float wealthDifMult = 1 - Mathf.Abs(otherPer.Wealth - Personality.Wealth);
                //Two entities of high difference in greed will result in dislike
                float greedDifMult = 1 - Mathf.Abs(otherPer.Greed - Personality.Greed);

                //We take product of all, then multiply by this entities personality.
                float totalRelation = weightedKingdom * subjugetMult * agresDifMult * wealthDifMult * greedDifMult * Personality.Kindness;
                //Clamp relationship [0,1]
                totalRelation = Mathf.Clamp(totalRelation, .2f, 1);
                return totalRelation;
            }

            if(entity is Player)
            {
                return 1;
            }



            //Default mid relationship
            return 0.5f;

        }



    }


    public static float GetRelationshipFromTag(EntityRelationshipTag tag)
    {
        switch (tag)
        {
            case EntityRelationshipTag.Enemy:
                return 0;
            case EntityRelationshipTag.Family:
                return 1;
            case EntityRelationshipTag.Friendly:
                return 0.7f;
            case EntityRelationshipTag.Neutral:
                return 0.5f;
        }
        return 0.5f;
    }
}

/// <summary>
/// Simple struct that contains 
/// </summary>
public struct EntityPersonality
{
    public float Aggression; //Relevent to combat - how likey an entity is to enter combat, their jobs etc
    public float Kindness; //Relevent to ???
    public float Loyalty; //Relevent to nationality/ faction belonging
    public float Greed; //Reltative to crime, greedy agression -> criminal
    public float Wealth;

    public EntityPersonality(float agression, float kindness, float loyaly, float greed, float wealth)
    {
        Aggression = agression;
        Kindness = kindness;
        Loyalty = loyaly;
        Greed = greed;
        Wealth = wealth;
    }
    public override string ToString()
    {
        return "Aggression: " + Aggression + ", Kindness: " + Kindness + ", Loyalty: " + Loyalty +
            ", Greed: " + Greed + ", Wealth: " + Wealth;
    }
}

public enum EntityRelationshipTag
{
    Enemy, Neutral, Friendly, Family
}