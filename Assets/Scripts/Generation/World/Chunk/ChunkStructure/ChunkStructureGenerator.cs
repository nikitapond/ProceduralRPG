using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Threading;
public class ChunkStructureGenerator
{
    public Object LOCK = new Object();
    public GameGenerator GameGenerator { get; private set; }

    public Dictionary<Vec2i, ChunkStructure> ChunkStructureShells { get; private set; }
    private GenerationRandom GenerationRandom;
    public ChunkStructureGenerator(GameGenerator gameGen)
    {
        GameGenerator = gameGen;
        GenerationRandom = new GenerationRandom(gameGen.Seed * 13 + 29535);
    }

    ///Shell creation - where we generate the empty shells for all structures
    #region shell_creation
    /// <summary>
    /// Calls the functions that generate the shells of all possible chunk structures
    /// The shells only define the positions and sizes, as well as the types of each structure.
    /// To generate them, 
    /// </summary>
    public void GenerateStructureShells()
    {
        //Create dictionary to store shells
        ChunkStructureShells = new Dictionary<Vec2i, ChunkStructure>();
        //Generate all bandit camps, iterate and add to lists
        foreach(KeyValuePair<Vec2i, ChunkStructure> kvp in GenerateBanditCampShells())
        {
            GameGenerator.World.AddChunkStructure(kvp.Value);
            ChunkStructureShells.Add(kvp.Key, kvp.Value);
        }
        foreach (KeyValuePair<Vec2i, ChunkStructure> kvp in GenerateAbandonedCastleShells())
        {
            GameGenerator.World.AddChunkStructure(kvp.Value);
            ChunkStructureShells.Add(kvp.Key, kvp.Value);
        }
        //TODO - add to others
    }



    private Dictionary<Vec2i, ChunkStructure> GenerateBanditCampShells(int count = 25)
    {
        Dictionary<Vec2i, ChunkStructure> banditShells = new Dictionary<Vec2i, ChunkStructure>();
        //iterate all counts
        for (int i=0; i<count; i++)
        {
            //We make5 attempts to find a valid place for each bandit camp
            for(int a=0; a<5; a++)
            {
                //Generate random position and size
                Vec2i position = GenerationRandom.RandomFromList(GameGenerator.TerrainGenerator.LandChunks);
                Vec2i size = GenerationRandom.RandomVec2i(2, 6);
                //Check if position is valid,
                if (IsPositionValid(position))
                {
                    //if valid, we add the structure to ChunkBases and to the dictionary of shells
                    ChunkStructure banditCampShell = new BanditCamp(position, size);
                    for(int x=0; x<size.x; x++)
                    {
                        for (int z = 0; z < size.z; z++)
                        {
                            GameGenerator.TerrainGenerator.ChunkBases[position.x + x, position.z + z].AddChunkStructure(banditCampShell);
                        }
                    }
                    banditShells.Add(position, banditCampShell);
                }
            }
        }
        return banditShells;
    }

    private Dictionary<Vec2i, ChunkStructure> GenerateAbandonedCastleShells(int count = 10)
    {
        Dictionary<Vec2i, ChunkStructure> banditShells = new Dictionary<Vec2i, ChunkStructure>();
        //iterate all counts
        for (int i = 0; i < count; i++)
        {
            //We make5 attempts to find a valid place for each bandit camp
            for (int a = 0; a < 20; a++)
            {
                //Generate random position and size
                Vec2i position = GenerationRandom.RandomFromList(GameGenerator.TerrainGenerator.LandChunks);
                Vec2i size = GenerationRandom.RandomVec2i(2, 6);
                //Check if position is valid,
                if (IsPositionValid(position))
                {

                }
            }
        }
        return banditShells;
    }

    private Dictionary<Vec2i, ChunkStructure> GenerateMineShells(int count = 20)
    {
        return null;
    }

 
    #endregion

    ///Where we generate the chunk data for all the structure.
    #region thread_gen

    private Dictionary<Vec2i, ChunkData> GeneratedChunks;
    private Object GeneratedChunksAddLock;

    public Dictionary<Vec2i, ChunkData> GenerateAllStructures()
    {

        GeneratedChunks = new Dictionary<Vec2i, ChunkData>();
        GeneratedChunksAddLock = new Object();

        //Create array to hold data we send to the thread
        ChunkStructure[] toGen = new ChunkStructure[10];
        int genCount = 0;

        //Create list to store all threads
        List<Thread> threads = new List<Thread>(20);

        //iterate all structure shells
        foreach(KeyValuePair<Vec2i, ChunkStructure> kvp in ChunkStructureShells)
        {
            //Add to array
            toGen[genCount] = kvp.Value;
            genCount++;
            if(genCount == 10)
            {
                //Start generation
                threads.Add(ThreadStructureGeneration(toGen));
                //Reset
                genCount = 0;
                toGen = new ChunkStructure[10];
            }
        }

        if(toGen[0] != null)
            threads.Add(ThreadStructureGeneration(toGen));


        bool threadsComplete = false;
        while (!threadsComplete)
        {
            threadsComplete = true;
            foreach (Thread t in threads)
            {
                if (t.IsAlive)
                    threadsComplete = false;
            }
        }
        return GeneratedChunks;
    }
    /// <summary>
    /// Creates and starts the thread to generate the specified structures
    /// </summary>
    /// <param name="toGen"></param>
    /// <returns></returns>
    private Thread ThreadStructureGeneration(ChunkStructure[] toGen)
    {
        Thread thread = new Thread(() => InternalThreadStructureGeneration(toGen));
        thread.Start();
        return thread;
    }

    private void InternalThreadStructureGeneration(ChunkStructure[] toGen)
    {
       
        //Ensure we have some structures to generate
        if (toGen[0] == null)
            return;

        //Create list to hold generated chunks. Create random (thread safe)
        List<ChunkData> generatedChunks = new List<ChunkData>(40);
        GenerationRandom genRan = new GenerationRandom(toGen[0].ChunkPos.x * 13 + toGen[0].ChunkPos.z * 3064);


        ChunkStructureBuilder builder = null;
        //iterate all structures to generate, ignore if null
        foreach (ChunkStructure str in toGen)
        {
            if (str == null)
                continue;
            //If a bandit camp, create a bandit camp builder then generate structure.
            if(str is BanditCamp)
            {
                builder = new BanditCampBuilder(str, GameGenerator);
            }

            builder.Generate(genRan);


            //If the chunk structure has sub worlds we iterate them all, and add them to the world
            //we then set the subworld of the entrance to its correct form
            if (builder.HasSubworlds)
            {
                lock (LOCK)
                {
                    foreach (KeyValuePair<Subworld, ISubworldEntranceObject> kvp in builder.Subworlds)
                    {
                        GameGenerator.World.AddSubworld(kvp.Key);
                        kvp.Value.SetSubworld(kvp.Key);

                    }
                }
                
            }

            generatedChunks.AddRange(builder.ToChunkData());
            
            

        }
        //Lock for thread safe adding
        lock (GeneratedChunksAddLock)
        {
            //iterate generated threads, add to dictionary
            foreach (ChunkData cd in generatedChunks)
            {
                GeneratedChunks.Add(new Vec2i(cd.X, cd.Z), cd);
            }
        }

        generatedChunks = null;
        genRan = null;

    }

    #endregion

    ///Contains function to check if positions are valid
    #region misc
    /// <summary>
    /// Takes a position and size, checks against terrain values to see if 
    /// the position is free to place the desired structure on
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    private bool IsPositionValid(Vec2i position, int clear = 5)
    {

        for(int x=-clear; x<= clear; x++)
        {
            for (int z = -clear; z <= clear; z++)
            {
                int cx = position.x + x;
                int cz = position.z + z;
                //Debug.Log(cx + "_" + cz);
                if (cx < 0 || cz < 0 || cx >= World.WorldSize - 1 || cz >= World.WorldSize - 1)
                    return false;
                ChunkBase cb = GameGenerator.TerrainGenerator.ChunkBases[cx, cz];
                if(cb.HasSettlement || cb.RiverNode != null || cb.Lake != null || cb.ChunkStructure != null)
                {
                    //Debug.Log(cb.HasSettlement + "_" + cb.RiverNode + "_" + cb.Lake + "_" + cb.ChunkStructure);
                    return false;
                }
            }
        }
        return true;
    }
    #endregion
}