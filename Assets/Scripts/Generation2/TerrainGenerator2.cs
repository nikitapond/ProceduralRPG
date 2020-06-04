using UnityEngine;
using UnityEditor;

public class TerrainGenerator2
{

    public ChunkBase2[,] ChunkBases;
    private GenerationRandom GenRan;
    private GameGenerator2 GameGen;

    public RiverGenerator2 RiverGen;

    public TerrainGenerator2(GameGenerator2 gameGen, int seed)
    {
        GameGen = gameGen;
        GenRan = new GenerationRandom(seed);
    }


    public void Generate()
    {
        ChunkBases = new ChunkBase2[World.WorldSize, World.WorldSize];
        GenerateBaseTerrain();
        RiverGen = new RiverGenerator2(GameGen);
        RiverGen.GenerateRivers(8);
    }



    /// <summary>
    /// Generates the height map & biomes, + chunk resource details
    /// </summary>
    private void GenerateBaseTerrain()
    {
        for (int x = 0; x < World.WorldSize; x++)
        {
            for (int z = 0; z < World.WorldSize; z++)
            {
                float height = WorldHeight(x, z);

                float temp = PerlinNoise(x, z, 1);
                float hum = PerlinNoise(x, z, 2);

                ChunkBiome b = ChunkBiome.ocean;


                if (height > 100)
                {
                    b = ChunkBiome.mountain;
                }
                else if (height < 10)
                {
                    b = ChunkBiome.ocean;
                }
                else if (temp > 0.4f && hum < 0.3f)
                {
                    b = ChunkBiome.dessert;
                }
                else if (temp > 0.4f & hum > 0.5f)
                {
                    b = ChunkBiome.forrest;
                }
                else
                {
                    b = ChunkBiome.grassland;
                }
                ChunkBases[x, z] = new ChunkBase2(new Vec2i(x, z), height, b);

                switch (b)
                {
                    //Deserts and mountains are for mining
                    case ChunkBiome.mountain:
                    case ChunkBiome.dessert:
                        ChunkBases[x, z].SetResourceAmount(ChunkResource.ironOre, PerlinNoise(x, z, 3));
                        ChunkBases[x, z].SetResourceAmount(ChunkResource.silverOre,0.4f* PerlinNoise(x, z, 4));
                        ChunkBases[x, z].SetResourceAmount(ChunkResource.goldOre, 0.2f*PerlinNoise(x, z, 5));
                        break;
                    case ChunkBiome.forrest:
                        ChunkBases[x, z].SetResourceAmount(ChunkResource.wood, 1);
                        ChunkBases[x, z].SetResourceAmount(ChunkResource.sheepFarm, PerlinNoise(x, z, 6));
                        break;
                    case ChunkBiome.grassland:
                        ChunkBases[x, z].SetResourceAmount(ChunkResource.sheepFarm, PerlinNoise(x,z, 7));
                        ChunkBases[x, z].SetResourceAmount(ChunkResource.cattleFarm, PerlinNoise(x, z, 8));
                        ChunkBases[x, z].SetResourceAmount(ChunkResource.wheatFarm, PerlinNoise(x, z, 9));
                        ChunkBases[x, z].SetResourceAmount(ChunkResource.vegetableFarm, PerlinNoise(x, z, 10));
                        ChunkBases[x, z].SetResourceAmount(ChunkResource.silkFarm, PerlinNoise(x, z, 11));


                        break;
                }
            }
        }
    }



    private float PerlinNoise(float x, float z, int i)
    {
        return Mathf.PerlinNoise(x * 0.005f + i * 13, z * 0.005f + i * 27);
    }

    public float WorldHeight(float x, float z)
    {
        float c = 0;
        c = RidgeNoise(x, z) * 128;
        for (int i = 1; i < 4; i++)
        {
            float scale = 64 * Mathf.Pow(0.5f, i);
            float octave = 0.005f * i;
            c += scale * Mathf.PerlinNoise(x * octave, z * octave);
        }

        

        //c = Mathf.Pow(c, 1.1f);
        float dist = Mathf.Sqrt((x - World.WorldSize / 2) * (x - World.WorldSize / 2) + (z - World.WorldSize / 2) * (z - World.WorldSize / 2));
        float sigma = 128;
        if (dist > 128)
        {
            c *= Mathf.Exp(-(dist - 128)/ sigma);
        }
        if (dist > 512 - 64)
        {
            c *= Mathf.Exp(-4 * (dist - (512 - 64)) / sigma);
        }
        /*
        c = Mathf.Pow(c, 1.1f);
        float gPow = (x - World.WorldSize / 2) * (x - World.WorldSize / 2) + (z - World.WorldSize / 2) * (z - World.WorldSize / 2);
        float mult = Mathf.Exp(-gPow / sigma);
        return c*mult;
        */
        return c;

    }


    private float RidgeNoise(float x, float z)
    {

        float c = Mathf.PerlinNoise(x * 0.005f, z * 0.005f);
        c -= 0.5f;
        c = Mathf.Abs(c);

        return 1-c;
    }

    public float HeightFunction(int x, int z)
    {
        int cx = Mathf.FloorToInt((float)x / World.ChunkSize);
        int cz = Mathf.FloorToInt((float)z / World.ChunkSize);
        float px = ((float)(x % World.ChunkSize)) / World.ChunkSize;
        float pz = ((float)(z % World.ChunkSize)) / World.ChunkSize;
        return WorldHeight(cx + px, cz + pz);
    }


    public Texture2D ToTexture()
    {
        Texture2D tex = new Texture2D(World.WorldSize, World.WorldSize);
        for(int x=0; x<World.WorldSize; x++)
        {
            for(int z=0; z<World.WorldSize; z++)
            {
                tex.SetPixel(x, z, ChunkBases[x, z].GetMapColor());
            }
        }
        tex.Apply();
        return tex;
    }


   

}