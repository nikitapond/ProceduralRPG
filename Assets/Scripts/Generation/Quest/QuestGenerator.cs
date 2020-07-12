using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class QuestGenerator
{

    public World World { get; private set; }


    private Dictionary<Vec2i, List<ChunkStructure>> OrderedStructures;
    private Dictionary<Vec2i, List<Settlement>> OrderedSettlements;
    private GenerationRandom GenRan;
    public QuestGenerator(World world)
    {
        World = world;
        GenRan = new GenerationRandom(0);
    }


    public List<Quest> GenerateAllQuests()
    {

        OrderedStructures = GetAllOrderedChunkStructures();
        OrderedSettlements = GetSettlementsOrderedByRegion();


        List<Quest> allQuests = new List<Quest>();
        int questCount = 0;
        foreach(KeyValuePair<Vec2i, List<ChunkStructure>> kvp in OrderedStructures)
        {
            foreach(ChunkStructure chunkStruct in kvp.Value)
            {
               

                if (questCount > 10)
                    return allQuests;
            }
        }

        return allQuests;


    }
    private int questCount = 0;

    /// <summary>
    /// Generates a quest with the goal of clearing a specific chunk structure
    /// </summary>
    /// <param name="dungeonPos"></param>
    /// <param name="chunkStructure"></param>
    /// <returns></returns>
    private Quest GenerateDungeonQuest(Vec2i dungeonPos, ChunkStructure chunkStructure)
    {
        return null;
        /*
        questCount++;
        //Get the dungeon this quest is based on
        Dungeon dun = (Dungeon)chunkStructure.DungeonEntrance.GetSubworld();
        //If the dungeon doesn't have a key (generation should be set so it doesn't), we create one
        Key key = dun.GetKey();
        if (key == null)
        {
            key = new SimpleDungeonKey(GenRan.RandomInt(1, int.MaxValue));
            dun.SetKey(key);
        }

        //We build the quest in reverse, we first define the goal
        List<QuestTask> revTasks = new List<QuestTask>();

        //For a dungeon quest, this is always to kill the dungeon boss
        revTasks.Add(new QuestTask("Kill " + dun.Boss.Name, QuestTask.QuestTaskType.KILL_ENTITY, new object[] { dun.Boss }));

        //The second to last task is to get to the dungeon, 
        revTasks.Add(new QuestTask("Get to dungeon", QuestTask.QuestTaskType.GO_TO_LOCATION, new object[] { chunkStructure.WorldMapLocation, dun.WorldEntrance }));

        //Before we get to the dungeon, we need to get the key.
        //The key can either be held by an entity, or it can be found in a 
        //random chunk structure with no dungeon.


        return GenerateDungeonKeyQuest_RandomStructure(dungeonPos, dun, revTasks);
        */
        return null;
    }

    /// <summary>
    /// Places the dungeon key in a random structure.
    /// 
    /// </summary>
    /// <param name="dungeonPos"></param>
    /// <param name="chunkStructure"></param>
    /// <param name="endTasks"></param>
    /// <returns></returns>
    private Quest GenerateDungeonKeyQuest_RandomStructure(Vec2i dungeonPos, Dungeon dungeon, List<QuestTask> endTasks)
    {
        return null;
        /*
        //Find a random chunk structure
        ChunkStructure ranStruct = GetRandomFreeStructure(dungeonPos, 3);
        if (ranStruct == null)
            throw new System.Exception("We need to fix this");
        Debug.Log(dungeon + "_" + ranStruct + "_" + ranStruct.FinalLootChest + "_");
        //We add the dungeon key to the loot chest of this structure
        ranStruct.FinalLootChest.GetInventory().AddItem(dungeon.GetKey());
        //Add the item finding task


        endTasks.Add(new QuestTask("Go to " + ranStruct.Name + " and collect the key", QuestTask.QuestTaskType.PICK_UP_ITEM,
            new object[] { dungeon.GetKey(), ranStruct.WorldMapLocation, (ranStruct.FinalLootChest as WorldObjectData).Position }));

        //Quest initiator will be a random entity
        //Therefore, we choose a random settlement
        Settlement set = GetRandomSettlement(dungeonPos, 5);
        //Then take a random npc from it.
        NPC npc = GameManager.EntityManager.GetEntityFromID(GenRan.RandomFromList(set.SettlementNPCIDs)) as NPC;
        QuestInitiator questInit = new QuestInitiator(QuestInitiator.InitiationType.TALK_TO_NPC, new object[] { npc , false});
        


        //We now reverse the tasks to get in correct order
        endTasks.Reverse();
        Quest quest = new Quest("Clear dungeon " + questCount, questInit, endTasks.ToArray(), QuestType.clear_dungeon);

        

        if (npc.Dialog == null)
        {
            NPCDialog dialog = new NPCDialog(npc, "Hello adventurer! How can I help you today?");

            NPCDialogNode exitNode = new NPCDialogNode("Don't worry, I'll be on my way", "");
            exitNode.IsExitNode = true;

            dialog.AddNode(exitNode);
            npc.SetDialog(dialog);

        }
        NPCDialogNode startQuestNode = new NPCDialogNode("Have you heard of any quests for an adventurer such as myself?","I've heard of a dungeon that may be full of sweet shit." +
        " It's probably locked though, last I heard the key was at " + ranStruct.Name);

        NPCDialogNode exitNode2 = new NPCDialogNode("Thanks! I'll be on my way", "");
        exitNode2.IsExitNode = true;
        startQuestNode.AddNode(exitNode2);
        npc.Dialog.AddNode(startQuestNode);

        startQuestNode.SetOnSelectFunction(() => {
            GameManager.QuestManager.StartQuest(quest);
        });
        startQuestNode.SetShouldDisplayFunction(() =>
        {
            if (GameManager.QuestManager.Unstarted.Contains(quest))
                return true;
            return false;

        });


        NPCDialogNode questRewardNode = new NPCDialogNode("I killed " + dungeon.Boss.Name, "I didn't think it was possible. Here, take this as a reward");
        questRewardNode.AddNode(exitNode2);
        questRewardNode.SetShouldDisplayFunction(() => {
            return GameManager.QuestManager.Completed.Contains(quest);
        });
        questRewardNode.SetOnSelectFunction(() =>
        {
            Inventory playerInv = GameManager.PlayerManager.Player.Inventory;
            playerInv.AddItem(new SteelLongSword());
        });
        npc.Dialog.AddNode(questRewardNode);

        return quest;
        */
    }

    private void AddNPCInitDialoge(NPC npc, QuestInitiator init, Quest quest)
    {
        if (init.InitType != QuestInitiator.InitiationType.TALK_TO_NPC)
            throw new System.Exception("NPC dialoge can only be added for quest initiations of type 'TALK_TO_NPC");

        if (npc.Dialog == null)
        {
            //npc.SetDialog(new NPCDialog(npc));
        }


        switch (quest.QuestType)
        {
            case QuestType.clear_dungeon:
                
                break;
        }
        

    }
    private void AddNPCTaskDialoge(NPC npc, QuestTask task)
    {
        
    }


    /// Get functions for settlement and chunk structures
    #region ordered_gets 
    /// <summary>
    /// Searches all settlements to find a random one within 'allowedDistance' regions
    /// of the defined position.
    /// </summary>
    /// <param name="centrePoint"></param>
    /// <param name="allowedDistance"></param>
    /// <returns></returns>
    private Settlement GetRandomSettlement(Vec2i centrePoint, int allowedDistance)
    {
        //We first collect all regions close to the required region
        List<Vec2i> allowedRegions = new List<Vec2i>();
        for (int x = -allowedDistance; x <= allowedDistance; x++)
        {
            for (int z = -allowedDistance; z <= allowedDistance; z++)
            {
                allowedRegions.Add(centrePoint + new Vec2i(x, z));
            }
        }

        //Keep running loop until a valid settlement is found
        bool isValid = false;
        while (!isValid)
        {
            //Choose random region
            Vec2i reg = GenRan.RandomFromList(allowedRegions);
            //If the selected region contains at least one settlment, randomly select one
            if (OrderedSettlements.ContainsKey(reg) && OrderedSettlements[reg].Count > 0)
            {
                return GenRan.RandomFromList(OrderedSettlements[reg]);
            }
            else
            {
                //If no settlements, remove it from the list of allowed regions
                //to prevent selecting it twice
                allowedRegions.Remove(reg);
            }
        }
        return null;
    }

    
    /// <summary>
    /// Searches all chunk structures to find a valid chunkstructure within
    /// 'allowedDistance' regions of the centre point.
    /// </summary>
    /// <param name="centrePoint"></param>
    /// <param name="allowedDistance"></param>
    /// <returns></returns>
    private ChunkStructure GetRandomFreeStructure(Vec2i centrePoint, int allowedDistance)
    {
        //We first collect all regions close to the required region
        List<Vec2i> allowedRegions = new List<Vec2i>();
        for(int x=-allowedDistance; x<= allowedDistance; x++)
        {
            for (int z = -allowedDistance; z <= allowedDistance; z++)
            {
                allowedRegions.Add(centrePoint + new Vec2i(x, z));
            }
        }

        bool isValid = false;
        int count = 0;
        while (!isValid)
        {
            if (allowedRegions.Count == 0)
                return null;
            //We choose a random region
            Vec2i chosenReg = GenRan.RandomFromList(allowedRegions);
            
            if (OrderedStructures.ContainsKey(chosenReg) && OrderedStructures[chosenReg].Count > 0)
            {
                ChunkStructure ranStruct = GenRan.RandomFromList(OrderedStructures[chosenReg]);
                //We require a structure with no dungeon
                if (!ranStruct.HasSubworlds)
                {                  
                    //If this chunk structure is valid, we remove it from the list of chunk structures and return it;
                    OrderedStructures[chosenReg].Remove(ranStruct);
                    isValid = true;
                    return ranStruct;
                }
            }
            else
            {
                allowedRegions.Remove(chosenReg);
            }

            count++;
            if (count > 100)
                isValid = true;
        }
        return null;
    }
    #endregion 



    /// Creates organised data structures for Settlements and ChunkStructures
    #region data_organisation
    /// <summary>
    /// We create a dictionary that sorts all the chunk structures into a dictionary
    /// This dictionary key is the region the structure exists in.
    /// </summary>
    /// <returns></returns>
    private Dictionary<Vec2i, List<ChunkStructure>> GetAllOrderedChunkStructures()
    {
        Dictionary<Vec2i, List<ChunkStructure>> allStructures = new Dictionary<Vec2i, List<ChunkStructure>>();
        foreach (KeyValuePair<int, WorldLocation> locs in World.WorldLocations)
        {
            if (locs.Value is ChunkStructure)
            {
                Vec2i regionPos = World.GetRegionCoordFromChunkCoord(locs.Value.ChunkPos);
                if (!allStructures.ContainsKey(regionPos))
                    allStructures.Add(regionPos, new List<ChunkStructure>());
                allStructures[regionPos].Add(locs.Value as ChunkStructure);
            }
        }

        return allStructures;
    }
    /// <summary>
    /// We create a dictionary that sorts all the settlements into a dictionary
    /// This dictionary key is the region the settlement exists in.
    /// </summary>
    /// <returns></returns>
    private Dictionary<Vec2i, List<Settlement>> GetSettlementsOrderedByRegion()
    {
        Dictionary<Vec2i, List<Settlement>> allSets = new Dictionary<Vec2i, List<Settlement>>();


        foreach(KeyValuePair<int, WorldLocation> locs in World.WorldLocations)
        {
            if(locs.Value is Settlement)
            {
                Vec2i regionPos = World.GetRegionCoordFromChunkCoord(locs.Value.ChunkPos);
                if (!allSets.ContainsKey(regionPos))
                    allSets.Add(regionPos, new List<Settlement>());
                allSets[regionPos].Add(locs.Value as Settlement);
            }
        }


        return allSets;
    }
    #endregion
}