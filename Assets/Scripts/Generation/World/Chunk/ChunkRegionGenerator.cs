using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
[System.Serializable]
public class ChunkRegionGenerator
{

    public static List<ChunkRegion> KeptRegions;

    private Object LOCK_OBJ;
    private GameGenerator GameGenerator;
    private ChunkGenerator ChunkGenerator;
    private Dictionary<Vec2i, ChunkData2> PreGeneratedChunks;
    private List<Vec2i> RegionsToGen;
    private List<Vec2i> CurrentlyThreadGenerating;

    private int[,] EMPTY_PLAINS;
    private int[,] OCEAN;

   
    public ChunkRegionGenerator(GameGenerator gameGen, Dictionary<Vec2i, ChunkData2> preGenChunks)
    {
        KeptRegions = new List<ChunkRegion>();
        LOCK_OBJ = new Object();
        GameGenerator = gameGen;
        CurrentlyThreadGenerating = new List<Vec2i>();
        PreGeneratedChunks = preGenChunks;

        ChunkGenerator = new ChunkGenerator(gameGen);
        RegionsToGen = new List<Vec2i>();



        EMPTY_PLAINS = new int[World.ChunkSize, World.ChunkSize];
        OCEAN = new int[World.ChunkSize, World.ChunkSize];
        for (int x = 0; x < World.ChunkSize; x++)
        {
            for (int z = 0; z < World.ChunkSize; z++)
            {
                EMPTY_PLAINS[x, z] = Tile.GRASS.ID;
                OCEAN[x, z] = Tile.WATER.ID;
            }
        }
    }

    public ChunkRegion ForceGenerateRegion(Vec2i rPos)
    {
        lock (LOCK_OBJ)
        {
            //Check if this region still needs to be generated
            if (RegionsToGen.Contains(rPos))
            {
                //If it does, we force generate it now
                RegionsToGen.Remove(rPos);
                return GenerateRegion(rPos.x, rPos.z);

            }
        }
        //Check if the region is currently generating
        if(CurrentlyThreadGenerating.Contains(rPos)){
            //If currently generating, we must return null as we wait for it to be generated
            return null;
        }
        return null;



    }


    public bool IsGeneratingRegion(Vec2i rPos)
    {
        lock (LOCK_OBJ)
        {
            if (CurrentlyThreadGenerating.Contains(rPos))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Generates the initial 9 regions, then adds the rest to 
    /// </summary>
    /// <param name="midpoint"></param>
    public void GenStartRegion(Vec2i midpoint)
    {
        RegionsToGen = new List<Vec2i>();
        for (int x=0; x < World.RegionCount; x++)
        {
            for (int z = 0; z < World.RegionCount; z++)
            {
                RegionsToGen.Add(new Vec2i(x, z));
            }
        }




        Vec2i[] toGen = new Vec2i[3];
        List<Thread> initGenThreads = new List<Thread>();
        for(int x=-1, i=0; x<=1; x++, i++)
        {
            for(int z=-1; z<=1; z++, i++)
            {

                Vec2i toGen_ = midpoint + new Vec2i(x, z);
                if (toGen_.x < 0 || toGen_.x >= World.RegionCount || toGen_.z < 0 || toGen_.z >= World.RegionCount)
                {
                    continue;
                    //toGen[i] = null;
                }
     
                initGenThreads.Add(ThreadGenerateRegions(new Vec2i[] { midpoint + new Vec2i(x, z) }, true));

                RegionsToGen.Remove(toGen_);
                /*
                if (i == 2)
                {
                    i = 0;
                //    initGenThreads.Add(ThreadGenerateRegions(toGen));
                    toGen = new Vec2i[3];
                }*/
            }
        }
        //initGenThreads.Add(ThreadGenerateRegions(toGen));
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        bool isDone = false;
        while (!isDone)
        {
            isDone = true;
            foreach(Thread c in initGenThreads)
            {
                if (c.IsAlive)
                    isDone = false;
            }
        }

        Debug.Log("[CRGenerator] Initial region generation complete, took time " + (sw.ElapsedMilliseconds));
        RegionsToGen.Sort(delegate (Vec2i a, Vec2i b)
        {
            return Vec2i.QuickDistance(midpoint, a).CompareTo(Vec2i.QuickDistance(midpoint, b));
        });
        string order = "Midpoint at " + midpoint + "\n";
        foreach(Vec2i v in RegionsToGen)
        {
            order += v + ", ";
        }
        Debug.Log(order, Debug.WORLD_GENERATION);
        ThreadGenerateRemainingRegions();

        foreach(ChunkRegion r in KeptRegions)
        {
            GameManager.WorldManager.CRManager.LoadedRegions[r.X, r.Z] = r;
        }

    }



    private void ThreadGenerateRemainingRegions()
    {

        Thread thread = new Thread(() => ThreadGenerateRemainingRegionsInternal());
        thread.Start();


    }

    private void ThreadGenerateRemainingRegionsInternal()
    {

        bool isComplete = false;

        while (!isComplete)
        {
            Vec2i toGen=null;
            //Use lock for thread safty (though this should only be accessed by this thread, so should be safe
            lock (LOCK_OBJ)
            {
                if (RegionsToGen.Count == 0)
                    isComplete = true;
                else
                {
                    toGen = RegionsToGen[0];
                    RegionsToGen.RemoveAt(0);
                    Debug.Log("Generating region " + toGen + ". " + RegionsToGen.Count + " remaining", Debug.WORLD_GENERATION);
                    if(toGen != null)
                        CurrentlyThreadGenerating.Add(toGen);
                }                    
            }
            if(toGen != null)
            {
                ChunkRegion cr = GenerateRegion(toGen.x, toGen.z);
                GameManager.LoadSave.SaveChunkRegion(cr);

                lock (LOCK_OBJ)
                {
                    CurrentlyThreadGenerating.Remove(toGen);
                }
            }
        }


    }






    
    /// <summary>
    /// Creates and start a new thread which generates the regions
    /// defined in regions via <see cref="ThreadGenerate(Vec2i[])"/>
    /// </summary>
    /// <param name="regions"></param>
    /// <returns></returns>
    private Thread ThreadGenerateRegions(Vec2i[] regions, bool keep = false)
    {
        lock (LOCK_OBJ)
        {
            CurrentlyThreadGenerating.AddRange(regions);
        }
        //Initiate
        Thread thread = new Thread(() => ThreadGenerate(regions, keep));
        thread.Start();


       
        return thread;
    }
    private int threadN;
    /// <summary>
    /// Takes an array of region position and generates them.
    /// Once generation is complete, access <see cref="GameManager.LoadSave"/>
    /// to save the region via <see cref="LoadSave.SaveChunkRegion(ChunkRegion)"/>
    /// </summary>
    /// <param name="regions"></param>
    private void ThreadGenerate(Vec2i[] regions, bool keep=false)
    {
        threadN++;
        //UnityEngine.Profiling.Profiler.BeginThreadProfiling("region_gen", "thread_" + threadN);
     
        //Create empty list to store created ChunkRegions
        List<ChunkRegion> genedReg = new List<ChunkRegion>(5);
        foreach (Vec2i v in regions)
        {
            
            //If the position is not null, generate the region and add to list
            if (v != null)
                genedReg.Add(GenerateRegion(v.x, v.z));
            
        }

        //Iterate all regions and save
        foreach (ChunkRegion r in genedReg)
        {
            GameManager.LoadSave.SaveChunkRegion(r);
        }


        //Set to null and GC ?? (TODO - check if needed)
        
        lock (LOCK_OBJ)
        {
            foreach (Vec2i v in regions)
                CurrentlyThreadGenerating.Remove(v);
            if (keep)
            {
                KeptRegions.AddRange(genedReg);
            }
        }
        genedReg = null;
        System.GC.Collect();
   

        //UnityEngine.Profiling.Profiler.EndThreadProfiling();

    }

    /// <summary>
    /// Iterates all chunk coordinates in the region.
    /// Checks if <see cref="PreGeneratedChunks"/> contains one of these chunks.
    /// If it does, we set the chunk based on the settlement chunk.
    /// If not, we generate the chunk via <see cref="ChunkGenerator.GenerateChunk(int, int)"/>
    /// </summary>
    /// <param name="rx"></param>
    /// <param name="rz"></param>
    /// <returns></returns>
    public ChunkRegion GenerateRegion(int rx, int rz)
    {

        //Define array for all chunks
        ChunkData2[,] regionChunks = new ChunkData2[World.RegionSize, World.RegionSize];
        //Iterate all chunks in region
        for (int cx = 0; cx < World.RegionSize; cx++)
        {
            for (int cz = 0; cz < World.RegionSize; cz++)
            {
                //Define chunk coordinate
                Vec2i chunkCoord = new Vec2i(rx * World.RegionSize + cx, rz * World.RegionSize + cz);

                //regionChunks[cx, cz] = ChunkGenerator.GenerateChunk(rx * World.RegionSize + cx, rz * World.RegionSize + cz);
                
                //Check if this chunk is a settlement chunk, if it is, set it as such
                if (PreGeneratedChunks.ContainsKey(chunkCoord))
                {
                    regionChunks[cx, cz] = PreGeneratedChunks[chunkCoord];
                }
                else
                {
            
                    //otherwise, generate the chunk via 
                    regionChunks[cx, cz] = ChunkGenerator.GenerateChunk(rx * World.RegionSize + cx, rz * World.RegionSize + cz);
               
                }                            
            }
        }
        return new ChunkRegion(rx, rz, regionChunks);
    }

 

}