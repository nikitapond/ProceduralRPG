using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
/// <summary>
/// Entity spawner decideds what entities to spawn
/// </summary>
public class EntitySpawner
{
    public static readonly int MinimumChunkSpawnDistance = 3; //No spawning in areas within 3 chunks of player

    private EntityManager Manager;

    private float Timer;
    public EntitySpawner(EntityManager manager)
    {
        Manager = manager;
    }

    /// <summary>
    /// Checks time passed since last EntitySpawnTick. If it is more than 5 seconds, 
    /// run <see cref="EntitySpawnTick"/>
    /// </summary>
    public void Update()
    {
        Timer += Time.deltaTime;
        if(Timer > 5)
        {
            Timer = 0;
            //EntitySpawnTick();
        }
    }


    bool hasSpawned = false;
    private void EntitySpawnTick()
    {
        //If the player is in a dungeon then we don't want to spawn any new creatures
        if (GameManager.WorldManager.InSubworld)
            return;
        Player player = GameManager.PlayerManager.Player; //Get the player

        //Check if we're able spawn an entity
        if (Manager.LoadedEntityCount() > EntityManager.MAX_LOADED_ENTITIES)
            return;

        Vec2i playerChunk = World.GetChunkPosition(player.Position);

        List<Vec2i> possibleSpawn = GetSpawnableChunks(player);

        if (!hasSpawned)
        {
           // Entity bear = new Bear();
            //bear.SetPosition(Vec2i.FromVector3(player.Position) + new Vec2i(4, 4));
            //Manager.LoadNonFixedEntity(bear);
            //hasSpawned = true;
        }
    }


    /// <summary>
    /// Returns all chunks that we are able to spawn creatures onto.
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    private List<Vec2i> GetSpawnableChunks(Player player)
    {
        Vec2i playerChunk = World.GetChunkPosition(player.Position);

        List<Vec2i> possibleSpawn = new List<Vec2i>();
        foreach (Vec2i v in Manager.LoadedChunkCoords())
        {
            //If chunk is too close, then continue
            if (Mathf.Abs(v.x - playerChunk.x) < MinimumChunkSpawnDistance || Mathf.Abs(v.z - playerChunk.z) < MinimumChunkSpawnDistance)
            {
                continue;
            }

            ChunkData c = GameManager.WorldManager.CRManager.GetLoadedChunk(v).Chunk;
            if (c == null || !c.IsLand)
                continue;

            //If this chunk is in a settlement then we can't spawn on it.
            if (c.GetSettlement() != null)
                continue;

            possibleSpawn.Add(v);
        }
        return possibleSpawn;
    }


}