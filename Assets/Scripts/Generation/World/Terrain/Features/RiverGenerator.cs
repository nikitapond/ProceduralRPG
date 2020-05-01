using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class RiverGenerator
{
    private GameGenerator GameGenerator;

    private static Vec2i[] ALL_DIR = new Vec2i[] { new Vec2i(-1, 0), new Vec2i(1, 0), new Vec2i(0, -1), new Vec2i(0, 1) };

    private static Vec2i[] PerpDir(Vec2i dir)
    {
        if(dir.x != 0)
        {
            return new Vec2i[] { ALL_DIR[2], ALL_DIR[3] };
        }
        return new Vec2i[] { ALL_DIR[0], ALL_DIR[1] };
    }

    public RiverGenerator(GameGenerator gameGen)
    {
        GameGenerator = gameGen;
    }

    /// <summary>
    /// Generates all the rivers in the world.
    /// Generates <para name="count"></para> rivers in total
    /// </summary>
    /// <param name="count"></param>
    public void GenerateAllRivers(int count = 10)
    {
        //Create a RNG to use for generating rivers
        GenerationRandom genRan = new GenerationRandom(GameGenerator.Seed);

        Vec2i middle = new Vec2i(World.WorldSize / 2, World.WorldSize / 2);
        River[] rivers = new River[count];
        for(int i=0; i<count; i++)
        {
            //Define the start point as one within a set distance of the middle of the map
            Vec2i startOff = genRan.RandomVec2i(-World.WorldSize / 5, World.WorldSize / 5);
            //Create null direction, define main direction heading away from land mass centre
            Vec2i dir = null;

            //Check directions travelling in negative x direction.
            if(startOff.x <= 0)
            {
                //If the z value is of greater magnitude, then the river travels in the z direction 
                if(Mathf.Abs(startOff.z) > Mathf.Abs(startOff.x))
                {
                    dir = new Vec2i(0, (int)Mathf.Sign(startOff.z));
                }
                else
                {
                    dir = new Vec2i(-1, 0);
                }
            }
            else
            {
                //If the z value is of greater magnitude, then the river travels in the z direction 
                if (Mathf.Abs(startOff.z) > Mathf.Abs(startOff.x))
                {
                    dir = new Vec2i(0, (int)Mathf.Sign(startOff.z));
                }
                else
                {
                    dir = new Vec2i(1, 0);
                }
            }
            //Generate and store river
            rivers[i] = GenerateRiver(middle + startOff, dir, genRan);
        }


        foreach(River riv in rivers)
        {
            foreach (KeyValuePair<Vec2i, RiverNode> kvp in riv.GenerateRiverNodes())
            {
                if (GameGenerator.TerrainGenerator.ChunkBases[kvp.Key.x, kvp.Key.z].RiverNode == null)
                    GameGenerator.TerrainGenerator.ChunkBases[kvp.Key.x, kvp.Key.z].AddRiver(kvp.Value);
                else
                    GameGenerator.LakeGenerator.AddLake(new Lake(kvp.Key, 10));
            }
        }
    }

    private River GenerateRiver(Vec2i start, Vec2i mainDirection, GenerationRandom genRan)
    {
        bool shouldStop = false;
        River r = new River();
        r.SetFirstChunk(start, 5);
        List<Vec2i> directions = new List<Vec2i>();

        directions.Add(mainDirection);
        directions.Add(mainDirection);
        directions.Add(mainDirection);
        directions.Add(mainDirection);
        directions.AddRange(PerpDir(mainDirection));
        Vec2i last = start;

        while (!shouldStop)
        {
            Vec2i next = last + genRan.RandomFromList<Vec2i>(directions);
            if(r.AddChunk(next, 5))
            {
                last = next;
                if(next.x < 0 || next.z < 0 || next.x>=World.WorldSize || next.z >= World.WorldSize)
                {
                    shouldStop = true;

                }
                else if (!GameGenerator.TerrainGenerator.ChunkBases[next.x, next.z].IsLand)
                    shouldStop = true;
            }
        }

        return r;
    }



    public void GenerateRiver()
    {

    }


    private int SimpleSinSum(int x)
    {
        return x;
        float sum = 0;
        for(int i=0; i<5; i++)
        {

            sum += Mathf.Sin(x * i) / ((float)i);


        }

        return (int)sum;
    }

    public River GEN_RIVER_TEST()
    {


        Vec2i start = new Vec2i(World.WorldSize/2, World.WorldSize/2);
        River r = new River();
        r.SetFirstChunk(start, 10);
        int lastY = start.z;
        //Iterate 100 x points
        Vec2i last = start;
        for(int x=0; x<10; x++)
        {
            //r.AddChunk(new Vec2i(start.x + x + 1, start.z), 10);
            
            int i = Random.Range(0, 2);
            Vec2i dr = new Vec2i(0, 1);
            Vec2i next = last + dr;
            r.AddChunk(next, 10);
            last = next;
            continue;

            int cx = start.x + (x+1);
            //Find the dy at this point
            int dy = SimpleSinSum(x) - lastY;
            //If dy > 1, then we must seperate out
            if(Mathf.Abs(dy) > 1)
            {
                int sign = (int)Mathf.Sign(dy);
                for(int y=0; y<Mathf.Abs(dy); y++)
                {
                    if(!r.AddChunk(new Vec2i(cx, lastY + y*sign), 10))
                    {
                        throw new System.Exception("Chunk not next to last one");
                    }
                    
                }
            }
            else
            {
                //If it is not, then we can add it directly.
                if(!r.AddChunk(new Vec2i(cx, lastY + dy), 10))
                {
                    throw new System.Exception("Chunk not next to last one 2");
                }
            }
            lastY = lastY + dy;


        }
        Debug.Log("start point at " + start);

        return r;
    }

}