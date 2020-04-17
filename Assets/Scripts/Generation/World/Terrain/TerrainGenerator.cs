﻿using UnityEngine;
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

    /// <summary>
    /// Initiates the Terrain Generator, the given World is the empty world
    /// that all terrain will be generated to
    /// </summary>
    /// <param name="world"></param>
    public TerrainGenerator(GameGenerator gamGen, World world)
    {
        GameGenerator = gamGen;
        World = world;
    }

    public void GenerateChunkBases()
    {
        ChunkBases = new ChunkBase[World.WorldSize, World.WorldSize];
        LandChunks = new List<Vec2i>();
        GenerationRandom genRan = new GenerationRandom(0);

        Vec2i mid = new Vec2i(World.WorldSize / 2, World.WorldSize / 2);
        float r_sqr = (World.WorldSize / 3) * (World.WorldSize / 3);

        Texture2D t = new Texture2D(World.WorldSize, World.WorldSize);
        Texture2D hum = new Texture2D(World.WorldSize, World.WorldSize);
        Texture2D temp = new Texture2D(World.WorldSize, World.WorldSize);



        float[,] humdity = new float[World.WorldSize, World.WorldSize];
        Vec2i offset = genRan.RandomVec2i(World.WorldSize / 8, World.WorldSize / 4);

        Vec2i humMid = mid + offset;
        float humRadSqr = genRan.Random(World.WorldSize / 4, World.WorldSize / 2);
        humRadSqr *= humRadSqr;


        Vec2i tempMid = mid -offset;
        float tempRadSqr = genRan.Random(World.WorldSize / 4, World.WorldSize / 2);
        tempRadSqr *= tempRadSqr;
        float[,] temperature = new float[World.WorldSize, World.WorldSize];

        for (int x = 0; x < World.WorldSize; x++)
        {
            for (int z = 0; z < World.WorldSize; z++)
            {
                float c = 1- Mathf.Pow(Mathf.PerlinNoise(x*0.01f, z*0.01f), 2);

                humdity[x, z] = 0.4f + 0.6f * Mathf.PerlinNoise(4000 + x * 0.02f, 4000 + z * 0.02f);
                humdity[x, z] /= (((x - humMid.x) * (x - humMid.x) + (z - humMid.z) * (z - humMid.z)) / humRadSqr);
                humdity[x, z] = Mathf.Clamp(humdity[x, z], 0, 1);

                //temperature[x, z] = Mathf.PerlinNoise(700 + x * 0.02f, 700 + z * 0.02f);
                temperature[x, z] = 0.4f + 0.6f*Mathf.PerlinNoise(700 + x * 0.02f, 700 + z * 0.02f);
                temperature[x, z] /= (((x - tempMid.x) * (x - tempMid.x) + (z - tempMid.z) * (z - tempMid.z)) / tempRadSqr);
                temperature[x, z] = Mathf.Clamp(temperature[x, z], 0, 1);
                hum.SetPixel(x, z, new Color(humdity[x, z], humdity[x, z], humdity[x, z]));
                temp.SetPixel(x, z, new Color(temperature[x, z], temperature[x, z], temperature[x, z]));

                c /= (((x - mid.x) * (x - mid.x) + (z - mid.z) * (z - mid.z)) / r_sqr);

                t.SetPixel(x,z, new Color(c, c, c));
                Vec2i v = new Vec2i(x, z);

                //if ((x - mid.x) * (x - mid.x) + (z - mid.z) * (z - mid.z) < r_sqr)
                if (c > 0.5)
                { //If point within this radius of middle
                    ChunkBases[x, z] = new ChunkBase(v, true);
                    LandChunks.Add(v);


                    //Deserts if its hot and dry
                    if (temperature[x, z] > 0.7f && humdity[x, z] < 0.4f)
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
                    ChunkBases[x, z] = new ChunkBase(v, false);
                }
            }
        }
        t.Apply();
        hum.Apply();
        temp.Apply();

        GameManager.Game.toDrawTexts[0] = t;
        GameManager.Game.toDrawTexts[1] = hum;
        GameManager.Game.toDrawTexts[2] = temp;

    }

    public void GenerateTerrainDetails()
    {
        GameGenerator.RiverGenerator.GenerateAllRivers();
        GameGenerator.LakeGenerator.GenerateAllLakes();
    }


}