using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
/// <summary>
/// Used to generate terrain
/// </summary>
public class TerrainGenerator
{
    public GameGenerator GameGenerator;
    public World World { get; private set; }

    public ChunkBase[,] ChunkBases { get; private set; }
    public List<Vec2i> LandChunks { get; private set; }
    private GenerationRandom GenRan;
    private float WorldRad;
    /// <summary>
    /// Initiates the Terrain Generator, the given World is the empty world
    /// that all terrain will be generated to
    /// </summary>
    /// <param name="world"></param>
    public TerrainGenerator(GameGenerator gamGen, World world)
    {
        GameGenerator = gamGen;
        GenRan = new GenerationRandom(GameGenerator.Seed);
        World = world;
    }

    public void GenerateChunkBases()
    {
        ChunkBases = new ChunkBase[World.WorldSize, World.WorldSize];
        LandChunks = new List<Vec2i>();

        Vec2i mid = new Vec2i(World.WorldSize / 2, World.WorldSize / 2);
        float r_sqr = (World.WorldSize / 3) * (World.WorldSize / 3);
        WorldRad = (World.WorldSize / 2.1f)* (World.WorldSize / 2.1f);

        Texture2D t = new Texture2D(World.WorldSize, World.WorldSize);
        Texture2D hum = new Texture2D(World.WorldSize, World.WorldSize);
        Texture2D temp = new Texture2D(World.WorldSize, World.WorldSize);



        float[,] humdity = new float[World.WorldSize, World.WorldSize];
        Vec2i offset = GenRan.RandomVec2i(World.WorldSize / 8, World.WorldSize / 4);

        Vec2i humMid = mid + offset;
        float humRadSqr = GenRan.Random(World.WorldSize / 4, World.WorldSize / 2);
        humRadSqr *= humRadSqr;


        Vec2i tempMid = mid -offset;
        float tempRadSqr = GenRan.Random(World.WorldSize / 4, World.WorldSize / 2);
        tempRadSqr *= tempRadSqr;
        float[,] temperature = new float[World.WorldSize, World.WorldSize];

        for (int x = 0; x < World.WorldSize; x++)
        {
            for (int z = 0; z < World.WorldSize; z++)
            {
                float c = WorldHeightChunk(x, z);


                    

                humdity[x, z] = 0.4f + 0.6f * Mathf.PerlinNoise(4000 + x * 0.02f, 4000 + z * 0.02f);
                humdity[x, z] /= (((x - humMid.x) * (x - humMid.x) + (z - humMid.z) * (z - humMid.z)) / humRadSqr);
                humdity[x, z] = Mathf.Clamp(humdity[x, z], 0, 1);

                //temperature[x, z] = Mathf.PerlinNoise(700 + x * 0.02f, 700 + z * 0.02f);
                temperature[x, z] = 0.4f + 0.6f*Mathf.PerlinNoise(700 + x * 0.02f, 700 + z * 0.02f);
                temperature[x, z] /= (((x - tempMid.x) * (x - tempMid.x) + (z - tempMid.z) * (z - tempMid.z)) / tempRadSqr);
                temperature[x, z] = Mathf.Clamp(temperature[x, z], 0, 1);
                hum.SetPixel(x, z, new Color(humdity[x, z], humdity[x, z], humdity[x, z]));
                temp.SetPixel(x, z, new Color(temperature[x, z], temperature[x, z], temperature[x, z]));

                //c /= (((x - mid.x) * (x - mid.x) + (z - mid.z) * (z - mid.z)) / r_sqr);

                t.SetPixel(x,z, new Color(c/World.ChunkHeight, c / World.ChunkHeight, c / World.ChunkHeight));
                Vec2i v = new Vec2i(x, z);

                //if ((x - mid.x) * (x - mid.x) + (z - mid.z) * (z - mid.z) < r_sqr)
                if (c > 40 && !(x==0||z==0||x==World.WorldSize-1 || z==World.WorldSize-1))
                { //If point within this radius of middle

                    ChunkBases[x, z] = new ChunkBase(v, Mathf.FloorToInt(c), true);
                    LandChunks.Add(v);

                    if(c > 100)
                    {
                        ChunkBases[x, z].SetBiome(ChunkBiome.mountain);
                    }else if (temperature[x, z] > 0.7f && humdity[x, z] < 0.4f)
                    {
                        ChunkBases[x, z].SetBiome(ChunkBiome.dessert);
                    }
                    else if (humdity[x, z] > 0.4f && temperature[x, z]>0.5f)
                    {
                        ChunkBases[x, z].SetBiome(ChunkBiome.forrest);
                    }
                    else
                    {
                        ChunkBases[x, z].SetBiome(ChunkBiome.grassland);
                    }

                    /*
                    if(temperature[x, z] > 0.7f && humdity[x,z] < 0.4f)
                    {
                        ChunkBases[x, z].SetBiome(ChunkBiome.dessert);
                    }else if(temperature[x, z] < 0.7f && humdity[x, z] > 0.6f)
                    if (temperature[x, z] < 0.25f)
                    {
                        ChunkBases[x, z].SetBiome(ChunkBiome.forrest);
                    }
                    else
                    {
                            ChunkBases[x, z].SetBiome(ChunkBiome.grassland);
                    }*/

                }
                else
                {
                    ChunkBases[x, z] = new ChunkBase(v, 1, false);
                }
            }
        }
        t.Apply();
        hum.Apply();
        temp.Apply();
        /*
        GameManager.Game.toDrawTexts[0] = t;
        GameManager.Game.toDrawTexts[1] = hum;
        GameManager.Game.toDrawTexts[2] = temp;*/
    }

    private float GenerateHeight(float x, float z)
    {
        float sum = 0;
        float ridgeScale = 0.005f;
        float heightScale = 0.01f;
        for (int i = 0; i < 4; i++)
        {
            sum += Mathf.Pow(0.4f, i + 1) * Mathf.PerlinNoise(x * heightScale * (i + 1), z * heightScale * (i + 1));
        }
        //sum = Mathf.Pow(sum, 0.7f);


        float distMult = (World.WorldSize * World.WorldSize / 9) / ((x - World.WorldSize / 2) * (x - World.WorldSize / 2) + (z - World.WorldSize / 2) * (z - World.WorldSize / 2));
        return sum + 0.2f * (distMult * Mathf.Pow((1 - Mathf.Abs(Mathf.PerlinNoise(x * ridgeScale * (0 + 1) + 7, z * ridgeScale * (0 + 1) + 13) - 0.5f) * 2), 2.5f));

    }
    public float WorldHeightChunk(float x, float z)
    {
        //return GenerateHeight(x, z) * 128;
        //return GenerateHeight(x, z);
        float c = 0;
        for(int i=1; i<8; i++)
        {
            float scale = 32* Mathf.Pow(0.5f, i);
            float octave = 0.05f * i;
            c += scale * Mathf.PerlinNoise(x * octave, z * octave);
        }

        //float c = 5 + 1*Mathf.PerlinNoise(x * 0.3f, z * 0.3f)*20;

        //float c = 5 + (World.ChunkHeight - 5) * (1 - Mathf.Pow(Mathf.PerlinNoise(x * 0.01f, z * 0.01f), 2)); 
        //float radialScale = ((x - World.WorldSize / 2) * (x - World.WorldSize / 2) + (z - World.WorldSize / 2) * (z - World.WorldSize / 2))/ WorldRad;
        //c *= (1-Mathf.Clamp(radialScale, 0, 1));
        
        if((x - World.WorldSize / 2) * (x - World.WorldSize / 2) + (z - World.WorldSize / 2) * (z - World.WorldSize / 2) > WorldRad)
        {
            c = 1;
        }

        return c * 6;
    }


    public float WorldHeight(int x, int z)
    {
        int cx = Mathf.FloorToInt((float)x / World.ChunkSize);
        int cz = Mathf.FloorToInt((float)z / World.ChunkSize);
        float px = ((float)(x % World.ChunkSize)) / World.ChunkSize;
        float pz = ((float)(z % World.ChunkSize)) / World.ChunkSize;
        return WorldHeightChunk(cx + px, cz + pz);
    }
    public void GenerateTerrainDetails()
    {
        GameGenerator.RiverGenerator.GenerateAllRivers();
        GameGenerator.LakeGenerator.GenerateAllLakes();
    }


}