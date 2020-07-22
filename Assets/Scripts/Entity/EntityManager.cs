using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class EntityManager : MonoBehaviour
{

    public static EntityManager Instance;

    /// <summary>
    /// IDLE_CHUNK_DISTANCE - An entity that is further than this many chunks away from the player
    /// will be set to idle.
    /// When idle, no update calculations are run. The entity is set to idle mode, which prevents physics calculations also
    /// 
    /// </summary>
    public static readonly int IDLE_CHUNK_DISTANCE = 8;
    public static readonly int IDLE_DISTANCE_SQR = World.ChunkSize * World.ChunkSize * IDLE_CHUNK_DISTANCE * IDLE_CHUNK_DISTANCE;
    public static readonly int ENTITY_CLOSE_TO_PLAYER_RADIUS = World.ChunkSize * 5;
    public static readonly int MAX_LOADED_ENTITIES = 128;

    private int FixedEntityCount;
    //private Dictionary<Vec2i, List<int>> FixedEntitiesByChunk; //Fixed entities are ones that are saved when un-loaded
    /// <summary>
    /// Dictionary containing all entities, ordered by their ID
    /// </summary>
    private Dictionary<int, Entity> AllEntities;
    /// <summary>
    /// All entity IDs, sorted by: <br/>
    /// Key[0] -> ID of current subworld<br/>
    /// Vavlue[0] -> Key[1] -> Chunk position within the world<br/>
    /// Value[1] -> List of entity IDs in this chunk, in this world
    /// 
    /// </summary>
    private Dictionary<int, Dictionary<Vec2i, List<int>>> EntitiesByLocation;
    private List<LoadedEntity> LoadedEntities;
    /// <summary>
    /// List of entities who's actions should be updated on the slow tick, but who aren't currently loaded
    /// </summary>
    private HashSet<int> UnloadedUpdatableEntities;

    /// <summary>
    /// A list of entities created when the player leaves a subworld, containing all entities in that subworld. 
    /// This allows them to update for following etc
    /// </summary>
    private List<Entity> PastSubworldEntities;
    private int PastSubworldID;

    private List<Vec2i> LoadedChunks;
    private EntitySpawner EntitySpawner;

    private Dictionary<Vec2i, List<Entity>> LoadedEntityChunks;
    private Dictionary<Vec2i, List<Entity>> NearEntityChunks;

    private List<LoadedEntity> ToUnloadEntities;

    private List<WorldCombat> CurrentWorldCombatEvents;

    private Subworld Subworld;
    private float timer;
    private float timer2;

    private int CurrentUpdateIndex;
    private static int TicksPerFrame = 2;

    

    void Awake()
    {
        Instance = this;
        //FixedEntitiesByChunk = new Dictionary<Vec2i, List<int>>();
        AllEntities = new Dictionary<int, Entity>();
        LoadedEntities = new List<LoadedEntity>();
        LoadedChunks = new List<Vec2i>();
        EntitySpawner = new EntitySpawner(this);
        NearEntityChunks = new Dictionary<Vec2i, List<Entity>>();
        LoadedEntityChunks = new Dictionary<Vec2i, List<Entity>>();
        CurrentWorldCombatEvents = new List<WorldCombat>();
        ToUnloadEntities = new List<LoadedEntity>();
        EntitiesByLocation = new Dictionary<int, Dictionary<Vec2i, List<int>>>();
        PastSubworldEntities = new List<Entity>();
        UnloadedUpdatableEntities = new HashSet<int>();
    }



    #region load_save
    public void Save(GameLoadSave gls)
    {
        gls.GameEntities = AllEntities;
       // gls.GameEntityChunks = FixedEntitiesByChunk;
    }

    public void Load(GameLoadSave gls)
    {
        AllEntities = gls.GameEntities;
        //FixedEntitiesByChunk = gls.GameEntityChunks;
    }
    #endregion

    /// <summary>
    /// Main update loop for all entity
    /// </summary>
    private void Update()
    {
        
        //Check if we are in a subworld, if we are, update its entities
        int idleEnt = 0;


        if (Subworld == null)
        {
            //Check to see if we need to spawn any entities
            EntitySpawner.Update();
        }



        foreach (LoadedEntity e in LoadedEntities)
        {

            int quickDist = Vec2i.QuickDistance(e.Entity.TilePos, PlayerManager.Instance.Player.TilePos);
            e.LEPathFinder?.SetDistanceToPlayer(quickDist);
            if(quickDist > IDLE_DISTANCE_SQR || WorldManager.Instance.Time.TimeChange)
            {
                e.SetIdle(true);
                idleEnt++;
            }
            else
            {
                e.Entity.Update();
                e.SetIdle(false);
            }
            
        }

        foreach(LoadedEntity le in ToUnloadEntities)
        {
            Debug.Log("Unloading entity " + le);
            LoadedEntities.Remove(le);
        }
        ToUnloadEntities.Clear();

        DebugGUI.Instance.SetData("loaded_entity_count",(LoadedEntities.Count-idleEnt) + "/" + LoadedEntities.Count);


        //Increminent timer and run AI loop if required.
        //TODO - Split entities into a series of arrays which each take 
        //a turn to do AI update - reduce lag?
        timer += Time.deltaTime;
    
        timer2 += Time.deltaTime;

        //If there are less entities than the amount per frame, we update all of them
        if(LoadedEntities.Count < TicksPerFrame)
        {
            foreach (LoadedEntity e in LoadedEntities)
            {
                if(!e.IsIdle)
                    e.Entity.Tick();
            }
        }
        else
        {
            //otherwise, we incriment through them
            for (int i = 0; i < TicksPerFrame; i++)
            {
                CurrentUpdateIndex++;
                CurrentUpdateIndex %= LoadedEntities.Count;

                if(!LoadedEntities[(CurrentUpdateIndex) % LoadedEntities.Count].IsIdle)
                    LoadedEntities[(CurrentUpdateIndex) % LoadedEntities.Count].Entity.Tick();
            }

        }

        /*
        //If we have just left a subworld
        if(PastSubworldEntities.Count > 0)
        {
            //Iterate all entities
            foreach(Entity e in PastSubworldEntities)
            {
                //We check if any of the entities are in combat
                if (e.EntityAI.CombatAI.InCombat && e.EntityAI.CombatAI.CurrentTarget != null)
                {
                    //If they are, and we are in pursuet, we check if the entities are in seperate subworlds
                    if(e.EntityAI.CombatAI.CurrentTarget.CurrentSubworldID != e.CurrentSubworldID)
                    {
                        //Current target in main world
                        if (e.EntityAI.CombatAI.CurrentTarget.CurrentSubworldID == -1) {

                            Subworld sw = e.GetSubworld();
                            e.MoveEntity(sw.ExternalEntrancePos, -1);
                            e.GetLoadedEntity().LEPathFinder.SetEntityTarget(e.EntityAI.CombatAI.CurrentTarget);
                            Debug.Log("Leaving subworld to persue");
                        }
                        else
                        {
                            Subworld sw = e.EntityAI.CombatAI.CurrentTarget.GetSubworld();
                            e.MoveEntity(sw.InternalEntrancePos, sw.SubworldID);
                            e.GetLoadedEntity().LEPathFinder.SetEntityTarget(e.EntityAI.CombatAI.CurrentTarget);
                            Debug.Log("Enter subworld to persue");

                        }



                    }
                }
            }
            PastSubworldEntities.Clear();
        }
        */
        
        if (timer2 > 1)
        {
            timer2 = 0;
            UpdateNearEntityChunks();
            UnloadedEntityTick();
        }
        //TODO - Add slow entity tick management


    }

    /// <summary>
    /// Called as a slow tick to update entities that aren't currently loaded.
    /// <br/> Used to update entities that are close to the player, but which are in different worlds. <br/>
    /// When the player is in the world, this consists of all entities in all subworlds who's entrances are in currently loaded chunks
    /// <br/>When the player is in a subworld, it is all the entities that were loaded previous to entering the subworld.
    /// <br/>Update allows entities to leave subworlds while they are not loaded (allowing pursuet).
    /// <br/>Also checks for the entities desired location (i.e, if they should be at work) and allows them to travel.
    /// </summary>
    public void UnloadedEntityTick()
    {
        Debug.Log("Unloaded update count: " + UnloadedUpdatableEntities.Count);
        int currentSubworldID = Subworld == null ? -1 : Subworld.SubworldID;
        //Iterate all entities
        foreach(int id in UnloadedUpdatableEntities)
        {
            //Get entity and its combat target
            Entity e = GetEntityFromID(id);

            Entity currentTarget = e.EntityAI.CombatAI.CurrentTarget;
            if (currentTarget != null)
            {
                Debug.Log(e + " is unloaded but has a target: " + currentTarget);
                Debug.Log(e + " attempting to follow " + currentTarget);
            }
                
            Subworld entWorld = e.GetSubworld();
            //Check if the current target is one the entity should be following
            if (currentTarget != null && currentTarget.CurrentSubworldID != e.CurrentSubworldID && currentTarget.IsLoaded)
            {
                
                if(entWorld != null)
                {
                    Debug.Log(e + " is leaving subworld, moving to subworld ID: " + "-1" + " pos:" + entWorld.ExternalEntrancePos);
                    //If we are currently in a subworld, then we assume we are attempting to leave the subworld (no nested sw)
                    MoveEntity(e, -1, entWorld.ExternalEntrancePos);
                }
                else
                {
                    entWorld = currentTarget.GetSubworld();
                    if(entWorld != null)
                    {
                        //If we are currently in a subworld, then we assume we are attempting to leave the subworld (no nested sw)
                        MoveEntity(e, entWorld.SubworldID, entWorld.InternalEntrancePos);
                    }
                }
            }else if(e is NPC)
            {
                
                //Get the NPC and its AI
                NPC npc = e as NPC;
                BasicNPCTaskAI npcAI = npc.EntityAI.TaskAI as BasicNPCTaskAI;

                if (!npcAI.HasTask || npcAI.CurrentTask.ShouldTaskEnd())
                {
                    //Check for a new task
                    EntityTask curTask = npcAI.ChooseIdleTask();
                    npcAI.SetTask(curTask);
                    if (curTask != null)
                    {
                        Debug.Log("HEREHEHREHERE");
                        //If the current task has a specified location
                        if (curTask.HasTaskLocation)
                        {
                            int taskWorld = curTask.Location.SubworldWorldID;
                            //Check if this entities task is in the currently loaded world.
                            //if this is the case, we should teleport the entity to this place
                            if (taskWorld == currentSubworldID)
                            {
                                Debug.Log("This should not happen...");
                                MoveEntity(e, taskWorld, curTask.Location.Position);
                                continue;
                            }
                            //If we are currently in the same world as the desired task, then we do not need to do anything
                            if (taskWorld == e.CurrentSubworldID)
                                continue;
                            //If the current task world is not loaded, and this entity is in an unloaded subworld, and will
                            //wish to travel to the target world
                            //The simplest example is an entity at home/work, who wishes to walk to a position in the main world.
                            if (e.CurrentSubworldID != -1 && taskWorld == -1)
                            {

                                Vec2i extPos = entWorld.ExternalEntrancePos;
                                if(EntityWithinDistanceOfPlayer(e, World.ChunkSize * 3)){
                                    //We move the entity from the current subworld to the main world
                                    MoveEntity(e, taskWorld, extPos);
                                    Debug.Log("1");
                                }

                               
                            }
                            else if(e.CurrentSubworldID != -1 && taskWorld != -1 && taskWorld != e.CurrentSubworldID)
                            {
                                //if the entity is currently in a subworld, and the task is in a different subworld
                                //We move first to the main world
                                Vec2i extPos = entWorld.ExternalEntrancePos;
                                if (EntityWithinDistanceOfPlayer(e, World.ChunkSize * 3))
                                {
                                    //We move the entity from the current subworld to the main world
                                    MoveEntity(e, taskWorld, extPos);
                                    Debug.Log("2");
                                }
                               
                                //TODO - check if currently loaded world is main world, if so, move to entrance and travel (depending on distance to player)
                            }else if(e.CurrentSubworldID == -1 && taskWorld != -1)
                            {
                                Debug.Log("3");
                            }

                        }
                    }

                }
            }
        }
        foreach(LoadedEntity le in LoadedEntities)
        {
            if (UnloadedUpdatableEntities.Contains(le.Entity.ID))
                UnloadedUpdatableEntities.Remove(le.Entity.ID);
        }
    }

    public bool EntityInWorldCombatEvent(Entity entity, out WorldCombat wce)
    {
        foreach(WorldCombat WC in CurrentWorldCombatEvents)
        {
            if (!WC.IsComplete)
            {
                if(WC.Team1.Contains(entity) || WC.Team2.Contains(entity))
                {
                    wce = WC;
                    return true;
                }
            }
        }
        wce = null;
        return false;
    }

    private void WorldCombatEventTick()
    {
        Debug.BeginDeepProfile("combat_event_tick");
        List<WorldCombat> toRemoveCombat = new List<WorldCombat>();
        foreach (WorldCombat wce in CurrentWorldCombatEvents)
        {
            if (wce.IsComplete)
                toRemoveCombat.Add(wce);
            else
                GameManager.EventManager.InvokeNewEvent(wce);
        }

        foreach (WorldCombat wc in toRemoveCombat)
            CurrentWorldCombatEvents.Remove(wc);

        foreach (WorldCombat wce in CurrentWorldCombatEvents)
        {
            GameManager.DebugGUI.SetData(wce.ToString(), wce.Team1.Count + "_" + wce.Team2.Count);
        }
        Debug.EndDeepProfile("combat_event_tick");
    }

    private void SubworldUpdate()
    {
        
    }


    public WorldCombat NewCombatEvent(Entity a, Entity b)
    {
        WorldCombat combatEvent = new WorldCombat(a, b);
        CurrentWorldCombatEvents.Add(combatEvent);
        EventManager.Instance.InvokeNewEvent(combatEvent);
        Debug.Log("Entity " + a.ToString() + " is attacking " + b.ToString());
        return combatEvent;
    }

    /// <summary>
    /// Checks all positions of entities, and adds to a dictionary based on positions.
    /// An entity will be added to the position of the dictionary if its chunk is within
    /// 1 of the relevent position
    /// </summary>
    private void UpdateNearEntityChunks()
    {
        //Iterate all near chunks and clear
        foreach(KeyValuePair<Vec2i, List<Entity>> kvp in NearEntityChunks)
        {
            kvp.Value.Clear();
        }

        foreach(KeyValuePair<Vec2i, List<Entity>> kvp in LoadedEntityChunks)
        {

            for(int x=-2; x<=2; x++)
            {
                for (int z = -2; z <= 2; z++)
                {
                    Vec2i key = kvp.Key + new Vec2i(x, z);
                    if (!NearEntityChunks.ContainsKey(key))
                        NearEntityChunks.Add(key, new List<Entity>());

                    if(kvp.Value.Count != 0)
                    {
                        //Debug.Log(kvp.Value.Count + " entities in chunk " + kvp.Key + " close to chunk" + key);
                    }

                    NearEntityChunks[key].AddRange(kvp.Value);
                }
            }

        }

        int total=0;
        int enabled=0;

        DebugGUI.Instance.SetData("chunk_col", +enabled + "/" + total);
    }

    public List<Entity> GetEntitiesNearChunk(Vec2i cPos)
    {
        List<Entity> toOut;
        NearEntityChunks.TryGetValue(cPos, out toOut);

        return toOut;
    }

    /// <summary>
    /// Informs the entity manager that an entity has moved chunks.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="last"></param>
    /// <param name="next"></param>
    public void UpdateEntityChunk(Entity entity, Vec2i last, Vec2i next)
    {
        Debug.BeginDeepProfile("update_entity_chunk");
        //If the last position is not null, we must remove the entity from said chunk position
        if (last != null && LoadedEntityChunks.ContainsKey(last))
        {
            LoadedEntityChunks[last].Remove(entity);
            //If we are removing the last entity, deleted the chunk address
            if (LoadedEntityChunks[last].Count == 0)
            {
                LoadedEntityChunks.Remove(last);
            }
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    //GameManager.WorldManager.CRManager.GetLoadedChunk(new Vec2i(x + last.x, z + last.z)).GetComponent<MeshCollider>().enabled = false;
                }
            }

            if (!(entity is Player))
            {
                //If the last position is not null, 
                bool succesful = false;
                if (EntitiesByLocation.TryGetValue(entity.CurrentSubworldID, out Dictionary<Vec2i, List<int>> subworldEnts))
                {
                    if (subworldEnts.TryGetValue(last, out List<int> entIds))
                    {
                        //if this entity is here, we remove them
                        if (entIds.Contains(entity.ID))
                        {
                            entIds.Remove(entity.ID);
                            succesful = true;
                        }
                    }
                    if (!succesful)
                        throw new System.Exception("No entity could be found at the old chunk position");


                }
            }
           

        }
        //If the next position is not null, we try to add to correct spot
        if(next != null)
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                   // GameManager.WorldManager.CRManager.GetLoadedChunk(new Vec2i(x + next.x, z + next.z)).GetComponent<MeshCollider>().enabled = true;
                }
            }
            //We don't update chunk position of player, as it isn't stored here
            if(!(entity is Player))
            {
                //If the chunk has no entity list associated, create it
                if (!LoadedEntityChunks.ContainsKey(next))
                    LoadedEntityChunks.Add(next, new List<Entity>(16));
                //Add to correct list
                LoadedEntityChunks[next].Add(entity);

                if (!EntitiesByLocation.ContainsKey(entity.CurrentSubworldID))
                    EntitiesByLocation.Add(entity.CurrentSubworldID, new Dictionary<Vec2i, List<int>>());
                if (!EntitiesByLocation[entity.CurrentSubworldID].ContainsKey(next))
                    EntitiesByLocation[entity.CurrentSubworldID].Add(next, new List<int>());
                EntitiesByLocation[entity.CurrentSubworldID][next].Add(entity.ID);
            }
           
        }
        Debug.EndDeepProfile("update_entity_chunk");


    }


    public void EnterSubworld(Subworld subworld)
    {
        if(subworld != null) //If we are entering a new subworld
        {
            Debug.Log("HEREHEHREHHER");
            PastSubworldID = -1;

            Debug.Log("LE count:" + LoadedEntities.Count);

            PastSubworldEntities.Clear();
            UnloadedUpdatableEntities.Clear();
            foreach (LoadedEntity le in LoadedEntities)
            {
                UnloadedUpdatableEntities.Add(le.Entity.ID);
                PastSubworldEntities.Add(le.Entity);
            }
            Debug.Log(PastSubworldEntities.Count);
            UnloadAllChunks();
            UnloadAllEntities();

            for(int x=0; x<subworld.SubworldChunks.GetLength(0); x++)
            {
                for (int z = 0; z < subworld.SubworldChunks.GetLength(1); z++)
                {
                    LoadedChunks.Add(new Vec2i(x, z));
                }
            }

        }
        if (subworld.HasEntities)
        {
            foreach (Entity e in subworld.Entities)
            {
                if(e.IsAlive)
                    LoadEntity(e);
            }
        }
        

        Debug.Log(subworld);
        if(subworld is Dungeon)
        {
            Debug.Log("test");
            Dungeon dun = subworld as Dungeon;
            foreach (Entity e in dun.DungeonEntities)
                LoadEntity(e);
            LoadEntity(dun.Boss);
        }
        Subworld = subworld;


    }

    public void LeaveSubworld()
    {
        if (Subworld != null)
        {
            PastSubworldID = Subworld.SubworldID;

            Subworld = null;
            PastSubworldEntities.Clear();
            foreach (LoadedEntity le in LoadedEntities)
            {
                PastSubworldEntities.Add(le.Entity);
            }

            LoadedChunks.Clear();

            UnloadAllEntities();
        }
    }


    public int LoadedEntityCount()
    {
        return LoadedEntities.Count;
    }

    public Entity GetEntityFromID(int id)
    {
        return AllEntities[id];
    }

    public bool IsEntityLoaded(Entity entity)
    {
        foreach(LoadedEntity le in LoadedEntities)
        {
            if (le.Entity == entity)
                return true;
        }
        return false;
    }
    public bool IsEntityLoaded(int entityID)
    {
        foreach(LoadedEntity le in LoadedEntities)
        {
            if (le.Entity.ID == entityID)
                return true;
        }
        return false;
    }


    #region load_unload_entity



    /// <summary>
    /// used to move an entity from one subworld to another
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="newSubworldID"></param>
    /// <param name="newPosition"></param>
    public void MoveEntity(Entity entity, int newSubworldID, Vec2i newPosition)
    {

        Vec2i oldChunk = World.GetChunkPosition(entity.Position);
        bool succesful = false;
        if(EntitiesByLocation.TryGetValue(entity.CurrentSubworldID, out Dictionary<Vec2i, List<int>> subworldEnts))
        {
            if(subworldEnts.TryGetValue(oldChunk, out List<int> entIds))
            {
                //if this entity is here, we remove them
                if (entIds.Contains(entity.ID))
                {
                    entIds.Remove(entity.ID);
                    succesful = true;
                }
            }
        }
      
        if (!succesful)
        {
            Debug.LogError("Could not find old location of entity: " + entity);
            throw new System.Exception("loool");
        }

        int id = entity.ID;
        //get world ID and chunk position
        Vec2i cPos = World.GetChunkPosition(newPosition);    

        if (!EntitiesByLocation.ContainsKey(newSubworldID))
        {
            EntitiesByLocation.Add(newSubworldID, new Dictionary<Vec2i, List<int>>());
        }

        if (!EntitiesByLocation[newSubworldID].ContainsKey(cPos))
        {
            EntitiesByLocation[newSubworldID].Add(cPos, new List<int>());
        }

        EntitiesByLocation[newSubworldID][cPos].Add(id);
        
        
        Subworld sw = World.Instance.GetSubworld(newSubworldID);
        
        
        if (sw != null)
            sw.AddEntity(entity);
        entity.SetSubworld(newSubworldID);
        entity.SetPosition(newPosition.AsVector3());
        entity.SetLastChunk(cPos);
        int loadedSubworldID = Subworld == null ? -1 : Subworld.SubworldID;
        //If the current entity is loaded...
        if (IsEntityLoaded(entity.ID))
        {

            Debug.Log("1");
            //Check if the new position is loaded, if not then we unload the entity
            if (!LoadedChunks.Contains(cPos) || (entity.CurrentSubworldID != loadedSubworldID))
            {
                Debug.Log("2");
                //If the new position isn't loaded, then we must unload the entity
                //If correct chunk position is loaded, but wrong world, we also unload
                UnloadEntity(entity.GetLoadedEntity(), false);
                //If the entity is now in a subworld after being unloaded,
                //we must check if they are close enough to the player to have a slow update.
                if(entity.CurrentSubworldID != -1)
                {
                    if(sw != null)
                    {
                        //We get the entrance, and check if the relvent chunk is loaded
                        Vec2i entranceChunk = World.GetChunkPosition(sw.ExternalEntrancePos);
                        //If it is, then we make sure we continue to run slow ticks on the unloaded entity
                        if (LoadedChunks.Contains(entranceChunk))
                        {
                            UnloadedUpdatableEntities.Add(entity.ID);
                        }
                    }
                }

            }
        }
        else
        {
            
            if (LoadedChunks.Contains(cPos) && (entity.CurrentSubworldID == loadedSubworldID))
            {
                LoadEntity(entity);
            }
        }

    }

    /// <summary>
    /// An entity is added to the dictionary containing all entities <see cref="AllEntities"/>
    /// with the Key being the entities ID. <br/>
    /// This ID is found by finding the current count of the dictionary, such that entity IDs start at 0 and 
    /// incriment upwards. <br/>
    /// Once this ID is known, we set the ID of the entity via <see cref="Entity.SetEntityID(int)"/>, and add it to
    /// the dictionary.
    /// We then check the entities world (world or subworld) and its position
    /// We add this to the relevent part of 
    /// </summary>
    /// <param name="entity"></param>
    public void AddEntity(Entity entity)
    {

        int id = AllEntities.Count;
        while (AllEntities.ContainsKey(id))
            id++;
        AllEntities.Add(id, entity);
        entity.SetEntityID(id);


        //entity.SetFixed(true);//If not fixed, make it fixed.
        Vec2i cPos = World.GetChunkPosition(entity.Position);

        //Ensure data structure for position exists
        if (!EntitiesByLocation.ContainsKey(entity.CurrentSubworldID))
            EntitiesByLocation.Add(entity.CurrentSubworldID, new Dictionary<Vec2i, List<int>>());
        if (!EntitiesByLocation[entity.CurrentSubworldID].ContainsKey(cPos))
            EntitiesByLocation[entity.CurrentSubworldID].Add(cPos, new List<int>());
        //Add entity
        EntitiesByLocation[entity.CurrentSubworldID][cPos].Add(entity.ID);

        if (LoadedChunks.Contains(cPos))
        {
            LoadEntity(entity);
        }

    }
    public void LoadNonFixedEntity(Entity entity)
    {
        entity.SetFixed(false);
        Vec2i chunk = World.GetChunkPosition(entity.Position);
        if (LoadedChunks.Contains(chunk))
        {
            LoadEntity(entity);
        }
    }
    public void UnloadAllEntities()
    {
        LoadedChunks.Clear(); //Remove all loaded chunks
        foreach (LoadedEntity e in LoadedEntities)
            UnloadEntity(e, false);
        LoadedEntities.Clear();
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="entity"></param>
    public void LoadEntity(Entity entity)
    {
        if (!entity.IsAlive)
            return;
        Debug.Log("Entity Pref: " + entity.GetEntityGameObject());

        LoadedEntity loadedEntity = Instantiate(entity.GetEntityGameObject()).GetComponent<LoadedEntity>();
        loadedEntity.gameObject.name = entity.Name;
        loadedEntity.gameObject.transform.parent = transform;

        loadedEntity.SetEntity(entity);
        entity.OnEntityLoad(loadedEntity);
        //Ensure entities last chunk is set.
        entity.SetLastChunk(World.GetChunkPosition(entity.Position));

        LoadedEntities.Add(loadedEntity);
        //UpdateEntityChunk(entity, null, World.GetChunkPosition(entity.Position));

        if (entity is NPC)
        {
            Debug.Log("Loading " + entity.Name + " from chunk " + World.GetChunkPosition(entity.Position));
        }

    }

    public void UnloadEntity(LoadedEntity e, bool autoRemove = true, bool killEntity=false)
    {

        if (e.Entity is Player)
            return;
        if (autoRemove)
            LoadedEntities.Remove(e);
        else
            ToUnloadEntities.Add(e);


        EventManager.Instance.RemoveListener(e.Entity.CombatManager);
        EventManager.Instance.RemoveListener(e.Entity.GetLoadedEntity());
        e.Entity.EntityAI.OnEntityUnload();

        if (!killEntity)
        {
            
        }


        Debug.Log("Destroying " + e.Entity);
        Destroy(e.gameObject);
        EventManager.Instance.RemoveListener(e);
    }

    #endregion


    #region chunk_related
    public List<Vec2i> LoadedChunkCoords()
    {
        return LoadedChunks;
    }

    public void LoadChunk(Vec2i v)
    {
        //Debug.Log("Loading chunk with entities");
        LoadedChunks.Add(v);

        int currentSubworldID = Subworld == null ? -1 : Subworld.SubworldID;
        //Main entity loading
        if(EntitiesByLocation.TryGetValue(currentSubworldID, out Dictionary<Vec2i, List<int>> chunkEnts))
        {
            if(chunkEnts.TryGetValue(v, out List<int> ents))
            {
                foreach(int id in ents)
                {
                    

                    Entity e = GetEntityFromID(id);
                    if(e.IsAlive)
                        LoadEntity(e);
                }
            }
        }
        if(currentSubworldID == -1)
        {
            List<Subworld> sws = World.Instance.GetSubworldsInChunk(v);
            if(sws != null)
            {
                //Iterate all subworlds in this chunk
                foreach (Subworld sw in sws)
                {
                    foreach (Entity e in sw.Entities)
                    {
                        UnloadedUpdatableEntities.Add(e.ID);
                    }
                }
            }
            

        }
    }
    public void UnloadAllChunks()
    {
        
        List<LoadedEntity> toUnload = new List<LoadedEntity>();

        foreach (LoadedEntity e in LoadedEntities)
        {
                toUnload.Add(e);
        }
        foreach (LoadedEntity e in toUnload)
        {
            UnloadEntity(e);
        }
        LoadedChunks.Clear();
        
    }
    public void UnloadChunk(Vec2i chunk)
    {
        LoadedChunks.Remove(chunk);
        List<LoadedEntity> toUnload = new List<LoadedEntity>();
        foreach (LoadedEntity e in LoadedEntities)
        {
            if (World.GetChunkPosition(e.transform.position) == chunk)
                toUnload.Add(e);

        }
        foreach (LoadedEntity e in toUnload)
        {
            UnloadEntity(e);
        }
    }

    public void UnloadChunks(List<Vec2i> chunks)
    {
        List<LoadedEntity> toUnload = new List<LoadedEntity>();

        foreach (Vec2i v in chunks)
            LoadedChunks.Remove(v);

        foreach(LoadedEntity e in LoadedEntities)
        {
            Vec2i entCPos = World.GetChunkPosition(e.transform.position);
            if (chunks.Contains(entCPos))
            {
                toUnload.Add(e);
            }
        }
        foreach(LoadedEntity e in toUnload)
        {
            UnloadEntity(e);
        }
    }

    #endregion


    public static bool EntityWithinDistanceOfPlayer(Entity e, float dist)
    {
        return e.TilePos.QuickDistance(PlayerManager.Instance.Player.TilePos) < dist * dist;
    }
    public static bool PlayerCanSeeEntity(Entity entity)
    {
        float LookAngle = PlayerManager.Instance.Player.LookAngle;
        Vector3 Position = PlayerManager.Instance.Player.Position;
        float fov = PlayerManager.Instance.Player.fov;


        Vector3 entityLookDirection = new Vector3(Mathf.Sin(LookAngle * Mathf.Deg2Rad), 0, Mathf.Cos(LookAngle * Mathf.Deg2Rad));
        Vector3 difPos = new Vector3(entity.Position.x - Position.x, 0, entity.Position.z - Position.z).normalized;
        //float angle = Vector3.Angle(entityLookDirection, difPos);
        float dot = Vector3.Dot(entityLookDirection, difPos);

        float angle = Mathf.Abs(Mathf.Acos(dot) * Mathf.Rad2Deg);
        //Debug.Log(entityLookDirection + ", " + difPos + ", " + angle);
        if (angle > fov)
            return false;
        //Debug.Log("object in way");
        return PlayerLineOfSight(entity);
    }




    /// <summary>
    /// Returns true if there is no opaque world object blocking the direct line
    /// of sight between this entity and the entity i question.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public static bool PlayerLineOfSight(Entity other)
    {

        Vector3 dir = other.Position - PlayerManager.Instance.Player.Position;
        RaycastHit[] hit = Physics.RaycastAll(PlayerManager.Instance.Player.Position + Vector3.up * 1.5f, dir, dir.magnitude);

        foreach (RaycastHit h in hit)
        {
            GameObject blocking = h.transform.gameObject;
            if (blocking.GetComponent<LoadedEntity>() != null)
            {
                continue; ;
            }
            else if (blocking.CompareTag("MainCamera"))
                continue;
            else if (blocking.GetComponent<LoadedChunk2>() != null)
                continue;
            else
            {
                return false;
            }

        }
        return true;
    }
}
