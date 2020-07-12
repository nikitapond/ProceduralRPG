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
    public static readonly int IDLE_CHUNK_DISTANCE = 4;
    public static readonly int IDLE_DISTANCE_SQR = World.ChunkSize * World.ChunkSize * IDLE_CHUNK_DISTANCE * IDLE_CHUNK_DISTANCE;
    public static readonly int ENTITY_CLOSE_TO_PLAYER_RADIUS = World.ChunkSize * 3;
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
            if(quickDist > IDLE_DISTANCE_SQR)
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
        Debug.BeginDeepProfile("entity_tick");
        if (timer > 0.2f)
        {
            WorldCombatEventTick();          

            GameManager.PlayerManager.Tick(timer);
            foreach (LoadedEntity e in LoadedEntities)
            {
                e.Entity.Tick(timer);
            }
            timer = 0;

        }*/

        if (timer2 > 1)
        {
            timer2 = 0;
            UpdateNearEntityChunks();
        
        }
        //TODO - Add slow entity tick management


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
        /*
        foreach(Vec2i v in LoadedChunks)
        {
            MeshCollider mc = GameManager.WorldManager.CRManager.GetLoadedChunk(v).GetComponent<MeshCollider>();
            total++;
            if (mc.enabled)
                enabled++;
            if(LoadedEntityChunks.ContainsKey(v) && LoadedEntityChunks[v].Count != 0)
            {
                //enabled++;
                //mc.enabled = true;
            }
            else
            {
               // mc.enabled = false;
            }
        }*/
        /*
        foreach(KeyValuePair<Vec2i, List<Entity>> kvp in LoadedEntityChunks)
        {
            MeshCollider mc = GameManager.WorldManager.CRManager.GetLoadedChunk(kvp.Key).GetComponent<MeshCollider>();
            total++;
            if (kvp.Value == null || kvp.Value.Count == 0)
            {

                mc.enabled = false;
            }
            else
            {
                mc.enabled = true;
                enabled++;
            }
        }*/
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
        if(last != null && LoadedEntityChunks.ContainsKey(last))
        {
            LoadedEntityChunks[last].Remove(entity);
            //If we are removing the last entity, deleted the chunk address
            if(LoadedEntityChunks[last].Count == 0)
            {
                LoadedEntityChunks.Remove(last);
            }
            for(int x=-1; x<=1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    //GameManager.WorldManager.CRManager.GetLoadedChunk(new Vec2i(x + last.x, z + last.z)).GetComponent<MeshCollider>().enabled = false;
                }
            }

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
        Debug.EndDeepProfile("update_entity_chunk");
        //Debug.Log("Entity " + entity + " moved from chunk " + last + " to " + next);
        /*
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


            if (!subworldEnts.ContainsKey(next))
                subworldEnts.Add(next, new List<int>());
            subworldEnts[next].Add(entity.ID);
        }
        */

    }


    public void EnterSubworld(Subworld subworld)
    {
        if(subworld != null) //If we are entering a new subworld
        {
            UnloadAllEntities();
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
        if(Subworld != null)
        {
            Subworld = null;
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

        entity.SetSubworld(newSubworldID);
        entity.SetPosition(newPosition.AsVector3());
        entity.SetLastChunk(cPos);
        int loadedSubworldID = Subworld == null ? -1 : Subworld.SubworldID;
        //If the current entity is loaded...
        if (IsEntityLoaded(entity.ID))
        {


            //Check if the new position is loaded
            if (!LoadedChunks.Contains(cPos) || (entity.CurrentSubworldID != loadedSubworldID))
            {
                //If the new position isn't loaded, then we must unload the entity
                //If correct chunk position is loaded, but wrong world, we also unload
                UnloadEntity(entity.GetLoadedEntity(), false);
            }
        }
        else
        {
            if (LoadedChunks.Contains(cPos) && (entity.CurrentSubworldID != loadedSubworldID))
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

}
