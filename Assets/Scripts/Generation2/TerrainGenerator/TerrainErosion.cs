using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class TerrainErosion
{

    public static ErosionParams DefaultParams = new ErosionParams();
    private int Size;

    private float[,] HeightMap;
    private ErosionParams Params;


    private ComputeShader Erosion;

    public TerrainErosion(float[,] heightmap)
    {
        Size = heightmap.GetLength(0);
        HeightMap = heightmap;
        Params = DefaultParams;


    }
    public TerrainErosion(float[,] heightmap, ErosionParams param)
    {
        HeightMap = heightmap;
        Params = param;
    }


    private float[] Flatten(float[,] map)
    {
        float[] res = new float[Size * Size];

        for(int i=0, x=0; x<Size; x++)
        {
            for (int z = 0; z < Size; z++, i++)
            {
                res[i] = map[x, z];
            }
        }
        return res;
    }
    private float[,] Unpack(float[] flat)
    {
        float[,] res = new float[Size, Size];
        for (int i = 0, x = 0; x < Size; x++)
        {
            for (int z = 0; z < Size; z++, i++)
            {
                res[x,z] = flat[i];
            }
        }
        return res;
    }


    public float[,] Erode(int numErosionIterations = 50000)
    {


        float[] map = Flatten(HeightMap);
        Erosion = ResourceManager.GetComputeShader("erosion");
        int mapSizeWithBorder = Size + 2 * Params.erosionBrushRadius;
        int numThreads = numErosionIterations / 1024;

        // Create brush
        List<int> brushIndexOffsets = new List<int>();
        List<float> brushWeights = new List<float>();

        float weightSum = 0;
        for (int brushY = -Params.erosionBrushRadius; brushY <= Params.erosionBrushRadius; brushY++)
        {
            for (int brushX = -Params.erosionBrushRadius; brushX <= Params.erosionBrushRadius; brushX++)
            {
                float sqrDst = brushX * brushX + brushY * brushY;
                if (sqrDst < Params.erosionBrushRadius * Params.erosionBrushRadius)
                {
                    brushIndexOffsets.Add(brushY * Size + brushX);
                    float brushWeight = 1 - Mathf.Sqrt(sqrDst) / Params.erosionBrushRadius;
                    weightSum += brushWeight;
                    brushWeights.Add(brushWeight);
                }
            }
        }
        for (int i = 0; i < brushWeights.Count; i++)
        {
            brushWeights[i] /= weightSum;
        }

        // Send brush data to compute shader
        ComputeBuffer brushIndexBuffer = new ComputeBuffer(brushIndexOffsets.Count, sizeof(int));
        ComputeBuffer brushWeightBuffer = new ComputeBuffer(brushWeights.Count, sizeof(int));
        brushIndexBuffer.SetData(brushIndexOffsets);
        brushWeightBuffer.SetData(brushWeights);
        Erosion.SetBuffer(0, "brushIndices", brushIndexBuffer);
        Erosion.SetBuffer(0, "brushWeights", brushWeightBuffer);

        // Generate random indices for droplet placement
        int[] randomIndices = new int[numErosionIterations];
        for (int i = 0; i < numErosionIterations; i++)
        {
            int randomX = Random.Range(Params.erosionBrushRadius, Size + Params.erosionBrushRadius);
            int randomY = Random.Range(Params.erosionBrushRadius, Size + Params.erosionBrushRadius);
            randomIndices[i] = randomY * Size + randomX;
        }

        // Send random indices to compute shader
        ComputeBuffer randomIndexBuffer = new ComputeBuffer(randomIndices.Length, sizeof(int));
        randomIndexBuffer.SetData(randomIndices);
        Erosion.SetBuffer(0, "randomIndices", randomIndexBuffer);

        // Heightmap buffer
        ComputeBuffer mapBuffer = new ComputeBuffer(map.Length, sizeof(float));
        mapBuffer.SetData(map);
        Erosion.SetBuffer(0, "map", mapBuffer);

        // Settings
        Erosion.SetInt("borderSize", Params.erosionBrushRadius);
        Erosion.SetInt("mapSize", mapSizeWithBorder);
        Erosion.SetInt("brushLength", brushIndexOffsets.Count);
        Erosion.SetInt("maxLifetime", Params.maxLifetime);
        Erosion.SetFloat("inertia", Params.inertia);
        Erosion.SetFloat("sedimentCapacityFactor", Params.sedimentCapacityFactor);
        Erosion.SetFloat("minSedimentCapacity", Params.minSedimentCapacity);
        Erosion.SetFloat("depositSpeed", Params.depositSpeed);
        Erosion.SetFloat("erodeSpeed", Params.erodeSpeed);
        Erosion.SetFloat("evaporateSpeed", Params.evaporateSpeed);
        Erosion.SetFloat("gravity", Params.gravity);
        Erosion.SetFloat("startSpeed", Params.startSpeed);
        Erosion.SetFloat("startWater", Params.startWater);

        // Run compute shader
        Erosion.Dispatch(0, numThreads, 1, 1);
        mapBuffer.GetData(map);

        // Release buffers
        mapBuffer.Release();
        randomIndexBuffer.Release();
        brushIndexBuffer.Release();
        brushWeightBuffer.Release();

        return Unpack(map);
    }

}


public class ErosionParams
{
    public int erosionBrushRadius = 3;


    public int maxLifetime = 50;
    public float sedimentCapacityFactor = 3;
    public float minSedimentCapacity = .01f;
    public float depositSpeed = 0.3f;
    public float erodeSpeed = 0.3f;

    public float evaporateSpeed = .01f;
    public float gravity = 4;
    public float startSpeed = 1;
    public float startWater = 1;
    public float inertia = 0.3f;

}