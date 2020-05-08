using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class KingdomNPCGenerator
{
    public readonly Kingdom Kingdom;
    public readonly EntityManager EntityManager;
    public readonly GameGenerator GameGen;

    private GenerationRandom GenerationRan;
    public KingdomNPCGenerator(GameGenerator gameGen, Kingdom kingdom, EntityManager entityManager)
    {
        GameGen = gameGen;


        if (gameGen == null)
            GenerationRan = new GenerationRandom(0);
        else
            //Create a RNG based on this seed and kingdomID
            GenerationRan = new GenerationRandom(GameGen.Seed*13 + kingdom.KingdomID*27);
        Kingdom = kingdom;
        EntityManager = entityManager;
    }

    /// <summary>
    /// <para>
    /// Generates all NPCs for this kingdom.
    /// The overall process takes several steps.
    /// We iterate all the settlements, generating all NPCs on a settlement by settlement basis. <see cref="GenerateSettlementNPC(Settlement)"/>
    /// </para>
    /// <para>
    /// The next step is the generate the empty shells for each NPC (<see cref="GenerateNPCShells(Settlement)"/>), where
    /// we generate the initial NPC. This is where we create the NPC object, set its spawn position and home.
    /// </para>
    /// <para>
    /// Next, the basic Kingdom data is set. The NPCs rank is decided (based on their home and their wealth)
    /// </para>
    /// <para>Then we generate the personalities for all the NPCs. This is largely random, except for loyalty which is has a higher multiplier 
    /// depending on the NPCs kingdom rank (i.e, kings have high loyalty to their kingdom, pesents have random)</para>
    /// <para>We can then generate the NPCs jobs. High loyalty -> solider, etc</para>
    /// </summary>
    public void GenerateKingdomNPC()
    {
        foreach(int i in Kingdom.SettlementIDs)
        {

            GenerateSettlementNPC(GameManager.WorldManager.World.GetSettlement(i));
        }

    }

    /// <summary>
    /// Generates all NPCs in a settlement
    /// First, generates each empty NPC <see cref="GenerateNPCShells(Settlement)"/>
    /// Next, generates the NPCKingdomData <see cref="GenerateSettlementNPCKingdomData(List{NPC}, Settlement)"/>
    /// Then sets the entities job
    /// </summary>
    /// <param name="set"></param>
    public void GenerateSettlementNPC(Settlement set)
    {
        
        List<NPC> npcs = GenerateNPCShells(set);
        Debug.Log("Total NPCs " + npcs.Count);
        GenerateSettlementNPCKingdomData(npcs, set);
        GenerateNPCPersonalities(npcs);
        GenerateNPCJobs(npcs, set);
        GenerateNPCDialoges(npcs);
        return;
    }
    /// <summary>
    /// Generates all empty NPCs, defining only their name, and thier homes
    /// </summary>
    /// <param name="set"></param>
    private List<NPC> GenerateNPCShells(Settlement set)
    {
        if (set.Buildings == null)
            return null;
        List<NPC> settlementNPCs = new List<NPC>(250);

        List<NPC> inThisHouse = new List<NPC>(10);

        //iterate all building, and check if this is a house
        foreach(Building b in set.Buildings)
        {
            
           
            if (b is House)
            {
                inThisHouse.Clear();
                //Define the house and get the possible tiles we can spawn the this houses' NPCs on
                House h = b as House;
                List<Vec2i> spawnableTiles = h.GetSpawnableTiles();
                Debug.Log("spawn count: " + spawnableTiles.Count);
                for(int i=0; i<h.capacity; i++)
                {
                    //Attempt to find a valid spawn point
                    Vec2i entitySpawn = MiscUtils.RandomFromArray(spawnableTiles, Random.value);
                    if(entitySpawn == null)
                    {
                        Debug.Log("No valid spawn point found for house " + h.ToString(), Debug.ENTITY_GENERATION);
                        continue;
                    }
                    //Create the empty NPC and add it to the settlement
                    NPC npc = new NPC(isFixed: true);
                    npc.SetPosition(entitySpawn);
                    npc.NPCData.SetHome(h);
                    //First two entities should be male and female (husband and wife)
                    //All others are then randomly assigned
                    if (i == 0)
                    {
                        npc.NPCData.SetGender(NPCGender.male);
                    }
                    else if (i == 1)
                        npc.NPCData.SetGender(NPCGender.female);
                    else
                        npc.NPCData.SetGender((NPCGender)GenerationRan.RandomInt(0, 2));
                    
                    EntityManager.AddFixedEntity(npc);
                    npc.SetName("NPC " + npc.ID);
                    set.AddNPC(npc);
                    settlementNPCs.Add(npc);
                    inThisHouse.Add(npc);
                }
                //If more than 1 person lives in this house, they are family
                if(inThisHouse.Count > 1)
                {
                    //Iterate all pairs of family members in this house
                    for(int i=0; i<inThisHouse.Count; i++)
                    {
                        for(int j=0; j<inThisHouse.Count; j++)
                        {
                            if (i == j)
                                continue;
                            inThisHouse[i].EntityRelationshipManager.SetRelationshipTag(inThisHouse[j], EntityRelationshipTag.Family);
                            inThisHouse[j].EntityRelationshipManager.SetRelationshipTag(inThisHouse[i], EntityRelationshipTag.Family);
                        }
                    }
                }
            }
        }
        return settlementNPCs;
    }
    /// <summary>
    /// Generates the kingdom data for each NPC -
    /// Tells the NPC which settlement and kingdom it belongs too, as well as its position in the settlement
    /// </summary>
    /// <param name="npcs"></param>
    /// <param name="settlement"></param>
    private void GenerateSettlementNPCKingdomData(List<NPC> npcs, Settlement settlement)
    {           
        foreach(NPC npc in npcs)
        {
            KingdomHierarchy rank = KingdomHierarchy.Peasant;
            House h = npc.NPCData.House as House;
            if(h is Castle) //Check if the house is a Castle
            {
                //If this is a capital, then this NPC is a monarch
                if (settlement.SettlementType == SettlementType.CAPITAL)
                {
                    rank = KingdomHierarchy.Monarch;
                }
                else
                {//If not, they're a noble
                    rank = KingdomHierarchy.Noble;
                }
                //Either way, they'll be a leader of this settlement
                settlement.AddLeader(npc);
            }
            else if(h is Hold)
            {
                rank = KingdomHierarchy.LordLady;
                settlement.AddLeader(npc);
            }
            else if(h is VillageHall)
            {
                rank = KingdomHierarchy.Mayor;
                settlement.AddLeader(npc);
            }
            else
            {
                if(settlement.SettlementType == SettlementType.CAPITAL)
                {
                    rank = KingdomHierarchy.Citizen; //All NPCs in capital are citizens
                }
                else
                {
                    //Citizen or peasant based on house value
                    float cutoff = 150; //The cutoff value. If house is worth less than this, peasant
                    if(h.Value < cutoff)
                    {
                        rank = KingdomHierarchy.Peasant;
                    }
                    else
                    {
                        rank = KingdomHierarchy.Citizen;
                    }
                }
            }
            //Create and appl.
            NPCKingdomData kData = new NPCKingdomData(rank, settlement.KingdomID, settlement.SettlementID);
            npc.SetKingdomData(kData);
        }
    }
    
    private void GenerateNPCPersonalities(List<NPC> npcs)
    {
        //iterate all npcs
        foreach(NPC npc in npcs)
        {
            //Personality is random
            KingdomHierarchy rank = npc.NPCKingdomData.Rank;
            float loyalty = 1;
            float wealth = 0;
            if (rank == KingdomHierarchy.Monarch)
            {
                wealth = 1;
                loyalty = 1;
            }
            else if (rank == KingdomHierarchy.Noble)
            {
                loyalty = Mathf.Clamp(GenerationRan.GaussianFloat(0.8f, 0.2f), 0, 1);
                wealth = Mathf.Clamp(GenerationRan.GaussianFloat(0.8f, 0.2f), 0, 1);
            }
            else if (rank == KingdomHierarchy.LordLady)
            {
                loyalty = Mathf.Clamp(GenerationRan.GaussianFloat(0.7f, 0.3f), 0, 1);
                wealth = Mathf.Clamp(GenerationRan.GaussianFloat(0.7f, 0.3f), 0, 1);
            }
            else if (rank == KingdomHierarchy.Knight || rank == KingdomHierarchy.Mayor)
            {
                loyalty = Mathf.Clamp(GenerationRan.GaussianFloat(0.7f, 0.4f), 0, 1);
                wealth = Mathf.Clamp(GenerationRan.GaussianFloat(0.7f, 0.3f), 0, 1);

            }
            else if (rank == KingdomHierarchy.Citizen)
            {
                loyalty = Mathf.Clamp(GenerationRan.GaussianFloat(0.6f, 0.6f), 0, 1);
                wealth = Mathf.Clamp(GenerationRan.GaussianFloat(0.5f, 0.3f), 0, 1);

            }
            else
            {
                loyalty = Mathf.Clamp(GenerationRan.GaussianFloat(0.5f, 0.5f), 0, 1);
                wealth = Mathf.Clamp(GenerationRan.GaussianFloat(0.3f, 0.3f), 0, 1);
            }

            float kindness = Mathf.Clamp(GenerationRan.GaussianFloat(0.7f, 0.3f), 0, 1);
            float agression = Mathf.Clamp(GenerationRan.GaussianFloat(0.3f, 0.2f), 0, 1);
            float greed = Mathf.Clamp(GenerationRan.GaussianFloat(0.6f, 0.4f), 0, 1);

            EntityPersonality pers = new EntityPersonality(agression, kindness, loyalty, greed, wealth);
            npc.SetPersonality(pers);
        }
    }
    private void GenerateNPCJobs(List<NPC> npcs, Settlement set)
    {
        List<NPCJob> workJobs = new List<NPCJob>();
        Dictionary<KingdomHierarchy, List<NPCJob>> rankedJobs = new Dictionary<KingdomHierarchy, List<NPCJob>>();
        //Iterate all buildings and select work buildings
        foreach (Building b in set.Buildings)
        {
            if (b is IWorkBuilding)
            {

                //Get the jobs for each building
                NPCJob[] js = (b as IWorkBuilding).GetWorkData.BuildingJobs;
                if (js == null)
                    continue;
                foreach(NPCJob j in js)
                {
                    if (!rankedJobs.ContainsKey(j.RequiredRank))
                        rankedJobs.Add(j.RequiredRank, new List<NPCJob>());
                    rankedJobs[j.RequiredRank].Add(j);
                }
      
            }
        }
        Debug.Log("Settlement " + set.ToString() + " has " + workJobs.Count + " jobs", Debug.ENTITY_GENERATION);


        foreach (NPC npc in npcs)
        {
            KingdomHierarchy rank = npc.NPCKingdomData.Rank;

            if (!rankedJobs.ContainsKey(rank))
                continue;
            if (rankedJobs[rank].Count == 0)
                continue;

            NPCJob job = rankedJobs[rank][0];
            rankedJobs[rank].Remove(job);
            npc.NPCData.SetJob(job);
        }
    }

    /// <summary>
    /// Generates a dialoge tree for all NPCs
    /// </summary>
    /// <param name="npcs"></param>
    private void GenerateNPCDialoges(List<NPC> npcs)
    {
        //NPCDialog dialog = new NPCDialog()

        foreach(NPC npc in npcs)
        {
            JobRankItems(npc);
        }
    }


    private void JobRankItems(NPC npc)
    {
        if (npc.NPCData.HasJob)
        {
            if(npc.NPCData.NPCJob is NPCJobBlackSmith)
            {
                npc.EquiptmentManager.AddDefaultItem(new Shirt(new ItemMetaData().SetColor(Color.red)));
            }else if(npc.NPCData.NPCJob is NPCJobSoldier)
            {
                npc.EquiptmentManager.AddDefaultItem(new Shirt(new ItemMetaData().SetColor(Color.green)));
            }else if(npc.NPCData.NPCJob is NPCJobKing)
            {
                npc.EquiptmentManager.AddDefaultItem(new Shirt(new ItemMetaData().SetColor(Color.yellow)));
            }
        }
        Color rankColor = Color.black;
        switch (npc.NPCKingdomData.Rank)
        {
            case KingdomHierarchy.Monarch:
                rankColor = Color.yellow;
                break;
            case KingdomHierarchy.Noble:
                rankColor = Color.blue;
                break;
            case KingdomHierarchy.LordLady:
                rankColor = Color.red;
                break;
            case KingdomHierarchy.Mayor:
                rankColor = Color.white;
                break;
            case KingdomHierarchy.Citizen:
                rankColor = Color.cyan;
                break;
            case KingdomHierarchy.Peasant:
                rankColor = Color.green;
                break;
        }
        npc.EquiptmentManager.AddDefaultItem(new Trousers(new ItemMetaData().SetColor(rankColor)));
    }
}