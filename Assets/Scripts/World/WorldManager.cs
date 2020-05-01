using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldManager : MonoBehaviour
{

    public static bool ISDAY = true;
    public static bool TEST = false;
    public static int LoadChunkRadius = 5;

    public static Vec2i LOAD_CHUNK;


    public ChunkRegionManager CRManager { get; private set; }
    public World World { get; private set; }
    public Subworld CurrentSubworld { get; private set; }
    public List<LoadedChunk> SubworldChunks { get; private set; }
    public Vec2i LoadedChunksCentre { get; private set; }

    //public int LoadedChunksRadius = 10;


    public ChunkRegion[,] LoadedRegions;

    public Dictionary<Vec2i, ChunkRegion> LoadedChunkRegions { get; private set; }
    public Dictionary<Vec2i, LoadedChunk> LoadedChunks { get; private set; }

    public List<LoadedProjectile> LoadedProjectiles { get; private set; }

    public Player Player { get; private set; }

    public void SetWorld(World world)
    {
        World = world;

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
        if (sub.WorldEntrance == null)
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
            //Load all subworld chunks and add them to the the relevent list
            CRManager.LoadSubworldChunks(sub);
            GameManager.PathFinder.LoadSubworld(sub);
            //Inform entity manager we are entering the sub world, then teleport player to correct position.
            GameManager.EntityManager.EnterSubworld(sub);
            Player.SetPosition(sub.SubworldEntrance);
        }


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
            foreach (LoadedChunk c in SubworldChunks)
            {
                Destroy(c.gameObject);
            }
            CRManager.LeaveSubworld();
            Player.SetPosition(CurrentSubworld.WorldEntrance);
            CurrentSubworld = null;
            LoadedChunksCentre = null;
            GameManager.EntityManager.LeaveSubworld();
        }
    }

    void Awake()
    {
        LoadedChunks = new Dictionary<Vec2i, LoadedChunk>();
        LoadedRegions = new ChunkRegion[World.RegionCount, World.RegionCount];

        SubworldChunks = new List<LoadedChunk>();
        LoadedProjectiles = new List<LoadedProjectile>();
        CRManager = GetComponent<ChunkRegionManager>();
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
            Player = GameManager.PlayerManager.Player;
            return;
        }

        SlowWorldTick();
        Debug.EndDeepProfile("world_update");

    }

    void SlowWorldTick()
    {
        List<LoadedProjectile> toRem = new List<LoadedProjectile>();
        foreach(LoadedProjectile p in LoadedProjectiles)
        {
            if (!MiscMaths.WithinDistance(p.transform.position, Player.Position, 40))
                toRem.Add(p);
        }
        foreach(LoadedProjectile proj in toRem)
        {
            LoadedProjectiles.Remove(proj);
            Destroy(proj.gameObject);
        }
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
    /// Spawns a new projectile in the current world/subworld at the given
    /// position and direction
    /// </summary>
    /// <param name="position"></param>
    /// <param name="direction"></param>
    /// <param name="projectile"></param>
    public void AddNewProjectile(Vector3 position, Vector2 direction, Projectile projectile, Entity source = null, float yAxisRotate=0)
    {
        Debug.Log("Projectile added");
        GameObject pro = Instantiate(projectile.GenerateProjectileObject());
        pro.transform.Rotate(new Vector3(0, yAxisRotate, 0));
        pro.transform.parent = transform;
        LoadedProjectile l = pro.AddComponent<LoadedProjectile>();
        l.CreateProjectile(position, direction, projectile, source);
        if(source != null)
        {
        }

        LoadedProjectiles.Add(l);
    }


    public void DestroyProjectile(LoadedProjectile proj)
    {
        LoadedProjectiles.Remove(proj);
        Destroy(proj.gameObject);
    }


    public LoadedBeam CreateNewBeam(Entity entity, Beam beam, Vector3 target)
    {
        GameObject beamObj = Instantiate(beam.GenerateBeamObject());
        LoadedBeam loadedBeam = beamObj.GetComponent<LoadedBeam>();

        loadedBeam.transform.parent = entity.GetLoadedEntity().transform;
        loadedBeam.transform.localPosition = Vector3.up * 1.5f;
        loadedBeam.CreateBeam(entity, target, beam);
        return loadedBeam;
    }


    public void DestroyBeam(LoadedBeam beam)
    {
        if(beam != null && beam.gameObject != null)
            Destroy(beam.gameObject);
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
        Vec2i chunkPos = World.GetChunkPosition(worldObject.WorldPosition);
        ChunkData2 c = CRManager.GetChunk(chunkPos);
        return;
        /*
        Vec2i localPos = new Vec2i(worldObject.WorldPosition.x % World.ChunkSize, worldObject.WorldPosition.z % World.ChunkSize);
        c.SetObject(localPos.x,localPos.z, worldObject);

        //Check if the object has been added to a loaded chunk
        LoadedChunk loaded = CRManager.GetLoadedChunk(chunkPos);
        if (loaded != null)
        {
            WorldObject newObject = worldObject.CreateWorldObject(loaded.transform);

            loaded.LoadedWorldObjects[localPos.x, localPos.z] = newObject;            
        }*/
    }

}
