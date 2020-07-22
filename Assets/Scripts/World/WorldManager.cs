using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance;



    public static bool ISDAY = true;
    public static bool TEST = false;
    public static int LoadChunkRadius = 5;

    public static Vec2i LOAD_CHUNK;

    public GameObject MainLight;

    public AstarPath PathFinder { get; private set; }

    public ChunkRegionManager CRManager { get; private set; }
    public World World { get; private set; }
    public WorldDateTime Time;
    public Subworld CurrentSubworld { get; private set; }
    //public List<LoadedChunk> SubworldChunks { get; private set; }
    public Vec2i LoadedChunksCentre { get; private set; }

    //public int LoadedChunksRadius = 10;


    public ChunkRegion[,] LoadedRegions;

    public Dictionary<Vec2i, ChunkRegion> LoadedChunkRegions { get; private set; }


    public Player Player { get; private set; }



    public void SetWorld(World world)
    {
        World = world;
        Time = new WorldDateTime();
    }


    public void EntityEnterSubworld(Entity entity, Subworld subworld)
    {

        if(subworld != null)
        {
            entity.MoveEntity(subworld.InternalEntrancePos.AsVector3(), subworld.SubworldID);
        }

        
    }
    public void EntityExitSubworld(Entity entity, Subworld subworld)
    {
        subworld.RemoveEntity(entity);
        entity.MoveEntity(GenerationRandom.RNG.RandomPositionOnRadius(subworld.ExternalEntrancePos.AsVector3(), 1.5f), -1);
        entity.SetSubworld(null);
        //TODO - check that removing below code hasn't broken anything
        //This code should be run correctly in
        ///<see cref="EntityManager.MoveEntity(Entity, int, Vec2i)"/>
        ///
        /*
        if (entity.GetLoadedEntity() != null)
        {
            entity.SetFixed(true);
            EntityManager.Instance.UnloadEntity(entity.GetLoadedEntity(), false);
            
        }*/
        

    }


    /// <summary>
    /// Enters the player into the subworld of the given ID.
    /// If the given subworld is the one the player is already in, leave the subworld <see cref="WorldManager.LeaveSubworld"/>
    /// Unloads world chunks 
    /// </summary>
    /// <param name="id"></param>
    public void EnterSubworld(int id)
    {
        //Collect subworld based on its ID
        Subworld sub = World.GetSubworld(id);
        //Check for null
        if (sub == null)
        {
            Debug.Error("Subworld of ID '" + id + "' could not be found");
            return;
        }
        if (sub == CurrentSubworld)
        {
            LeaveSubworld();
            return;
        }
        //If the subworld doesn't have an entrance, set its entrance as the current player position.
        if (sub.InternalEntrancePos == null)
        {
            Debug.Error("Subworld " + sub.ToString() + " has no WorldEntrance, Player position " + Player.Position + " has been set");
            sub.SetWorldEntrance(Vec2i.FromVector3(Player.Position));
        }

        if (CurrentSubworld != null)
        {
            Debug.Error("Cannot enter a subworld while already in one.");
            return;
        }
        if (sub != null)
        {
            Debug.Log("Player entering subworld " + sub.ToString(), Debug.NORMAL);
            //First unload all world chunks and set the current subworld
            CurrentSubworld = sub;
            //EntityManager.Instance?.UnloadAllChunks();
            //Load all subworld chunks and add them to the the relevent list
            CRManager.LoadSubworldChunks(sub);

            //GameManager.PathFinder.LoadSubworld(sub);
            //Inform entity manager we are entering the sub world, then teleport player to correct position.

            Player.SetPosition(sub.InternalEntrancePos.AsVector3());
            WaitAndPathScanCoroutine();

            EntityManager.Instance?.EnterSubworld(sub);
            
            //PlayerManager.Instance.ProceduralGridMover.UpdateGraph();

        }
    }



    private void WaitAndPathScanCoroutine(float time = 0.5f)
    {
        StartCoroutine(WaitAndScan(time));
    }
    private IEnumerator WaitAndScan(float t = 0.5f)
    {
        yield return new WaitForSeconds(t);
        AstarPath.active.Scan();

    }

    public GameObject ForceInstanse(GameObject prefab, Transform parent=null)
    {
        GameObject instance = Instantiate(prefab);

        if (parent == null)
            parent = transform;
        instance.transform.parent = parent;

        return instance;
    }



    public bool InSubworld { get { return CurrentSubworld != null; } }

    /// <summary>
    /// Called when a player leaves a Sub World.
    /// Unloads all the chunks in the sub world, and sets the players position back
    /// to the world entrance.
    /// </summary>
    public void LeaveSubworld()
    {
        Debug.Log("leaving subworld");
        if (CurrentSubworld != null)
        {
            EntityManager.Instance?.LeaveSubworld();
            CRManager.LeaveSubworld();
            Player.SetPosition(CurrentSubworld.ExternalEntrancePos.AsVector3());
            WaitAndPathScanCoroutine();

            //CurrentSubworld = null;
            LoadedChunksCentre = null;
            
            CurrentSubworld = null;
            //Rescan pathfinding

        }
    }

    void Awake()
    {
        LoadedRegions = new ChunkRegion[World.RegionCount, World.RegionCount];
        Instance = this;
        CRManager = GetComponent<ChunkRegionManager>();
        
        PathFinder = (AstarPath)GameObject.FindObjectOfType<AstarPath>();
        Debug.Log("Path finder... " + PathFinder);
    }

    /// <summary>
    /// Updates the world.
    /// If the player is not inside a subworld, we get the player chunk and check if 
    /// new chunks need to be loaded. If they do, load them
    /// </summary>
    void Update()
    {
        Debug.BeginDeepProfile("world_update");
        if (Player == null)
        {
            Player = PlayerManager.Instance.Player;
            return;
        }
        Time.Update(MainLight);
        Debug.EndDeepProfile("world_update");

    }



    /// <summary>
    /// Destroys the object at the given point
    /// TODO - make sure multi-tile objects are destroyed correctly.
    /// TODO - make sure objects with listeners (inventory that destroy on empty) have lsiteners removed correctly
    /// </summary>
    /// <param name="worldPos"></param>
    public void DestroyWorldObject(Vec2i worldPos)
    {
        return;
        /*
        Vec2i chunkPos = World.GetChunkPosition(worldPos);
        ChunkData c = CRManager.GetChunk(chunkPos);

        c.SetObject(worldPos.x, worldPos.z, null);
        //c.Objects[worldPos.x % World.ChunkSize, worldPos.z % World.ChunkSize] = null;
        LoadedChunk loaded = CRManager.GetLoadedChunk(chunkPos);
        Debug.Log("Destroy: " + worldPos);
        if (loaded != null)
        {
            if(loaded.LoadedWorldObjects[worldPos.x % World.ChunkSize, worldPos.z % World.ChunkSize] != null)
            {
                Destroy(loaded.LoadedWorldObjects[worldPos.x % World.ChunkSize, worldPos.z % World.ChunkSize].gameObject);
                loaded.LoadedWorldObjects[worldPos.x % World.ChunkSize, worldPos.z % World.ChunkSize] = null;
            }
            
        }*/
    }









    /// <summary>
    /// Adds the given world object to the given world position.
    /// If the object is placed in a loaded chunk, then we load the object into the game
    /// </summary>
    /// <param name="worldPos"></param>
    /// <param name="worldObject"></param>
    public void AddNewObject(WorldObjectData worldObject)
    {
        //Get relevent chunk of object placement.
        //Vec2i chunkPos = World.GetChunkPosition(worldObject.WorldPosition);
        //ChunkData2 c = CRManager.GetChunk(chunkPos);
        return;

    }

}
