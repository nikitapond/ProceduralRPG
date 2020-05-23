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
    private Dictionary<Vec2i, List<int>> FixedEntities; //Fixed entities are ones that are saved when un-loaded
    private Dictionary<int, Entity> FixedEntities_;
    private List<LoadedEntity> LoadedEntities;
    private List<Vec2i> LoadedChunks;
    private EntitySpawner EntitySpawner;

    private Dictionary<Vec2i, List<Entity>> LoadedEntityChunks;
    private Dictionary<Vec2i, List<Entity>> NearEntityChunks;

    private List<WorldCombat> CurrentWorldCombatEvents;

    private Subworld Subworld;
    private float timer;
    private float timer2;

    private int CurrentUpdateIndex;
    private static int TicksPerFrame = 2;

    void Awake()
    {
        Instance = this;
        FixedEntities = new Dictionary<Vec2i, List<int>>();
        FixedEntities_ = new Dictionary<int, Entity>();
        LoadedEntities = new List<LoadedEntity>();
        LoadedChunks = new List<Vec2i>();
        EntitySpawner = new EntitySpawner(this);
        NearEntityChunks = new Dictionary<Vec2i, List<Entity>>();
        LoadedEntityChunks = new Dictionary<Vec2i, List<Entity>>();
        CurrentWorldCombatEvents = new List<WorldCombat>();
    }



    #region load_save
    public void Save(GameLoadSave gls)
    {
        gls.GameEntities = FixedEntities_;
        gls.GameEntityChunks = FixedEntities;
    }

    public void Load(GameLoadSave gls)
    {
        FixedEntities_ = gls.GameEntities;
        FixedEntities = gls.GameEntityChunks;
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
        }
        Debug.EndDeepProfile("update_entity_chunk");
        //Debug.Log("Entity " + entity + " moved from chunk " + last + " to " + next);
    }


    public void EnterSubworld(Subworld subworld)
    {
        if(subworld != null) //If we are entering a new subworld
        {
            UnloadAllEntities();
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
        return FixedEntities_[id];
    }

    #region load_unload_entity
    public void AddFixedEntity(Entity entity)
    {
        entity.SetFixed(true);//If not fixed, make it fixed.
        Vec2i r = World.GetChunkPosition(entity.Position);
        if (!FixedEntities.ContainsKey(r))
        {
            Debug.Log("New entity chunk created at " + r, Debug.ENTITY_GENERATION);
            FixedEntities.Add(r, new List<int>());

        }

        int id = FixedEntities_.Count;
        entity.SetEntityID(id);
        FixedEntities_.Add(id, entity);
        FixedEntities[r].Add(entity.ID);

        //Debug.Log("added at " + chunk);
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

    public void LoadEntity(Entity entity)
    {
        GameObject entityObject = Instantiate(entity.GetEntityGameObject());
        entityObject.name = entity.Name;
        entityObject.transform.parent = transform;

        LoadedEntity loadedEntity = entityObject.GetComponent<LoadedEntity>();
        loadedEntity.SetEntity(entity);
        entity.OnEntityLoad(loadedEntity);
        LoadedEntities.Add(loadedEntity);

        if (entity is NPC)
        {
            Debug.Log("Loading " + entity.Name + " from chunk " + World.GetChunkPosition(entity.Position), Debug.ENTITY_TEST);
        }

    }

    public void UnloadEntity(LoadedEntity e, bool autoRemove = true)
    {
        if (autoRemove)
            LoadedEntities.Remove(e);



        EventManager.Instance.RemoveListener(e.Entity.CombatManager);
        EventManager.Instance.RemoveListener(e.Entity.GetLoadedEntity());
        e.Entity.EntityAI.OnEntityUnload();

        if (e.Entity.IsFixed)
        {
            AddFixedEntity(e.Entity);
        }
            

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
        //Debug.Log("Loading entities from chunk " + v);
        if (FixedEntities.ContainsKey(v))
        {
            List<int> toLoad = FixedEntities[v];
            Debug.Log("Loading " + toLoad.Count + " entities from " + v, Debug.ENTITY);
            //Debug.Log(toLoad.Count);
            foreach (int e in toLoad)
                LoadEntity(GetEntityFromID(e));

            FixedEntities[v].Clear();
        }
        
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
