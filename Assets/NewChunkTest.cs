using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewChunkTest : MonoBehaviour
{

    private ChunkLoader ChunkLoader;

    private void Awake()
    {
        ChunkLoader = GetComponentInChildren<ChunkLoader>();
    }

    void Start()
    {
        int[,] tiles = new int[World.ChunkSize, World.ChunkSize];
        float[,] heights = new float[World.ChunkSize, World.ChunkSize];
        ChunkVoxelData cvd = new ChunkVoxelData();
        for(int x=0; x<World.ChunkSize; x++)
        {
            for (int z = 0; z < World.ChunkSize; z++)
            {
                heights[x, z] = x + z;
                tiles[x, z] = Tile.GRASS.ID;
                for(int y=0; y<World.ChunkHeight; y++)
                {
                    if(x+y+z < 20)
                    {
                        cvd.SetVoxel(x, y, z, Voxel.stone);
                    }else if((x-8)*(x-8)+ (y - 8) * (y - 8) + (z - 8) * (z - 8) < 5 * 5)
                    {
                        cvd.SetVoxel(x, y, z, Voxel.wood);
                    }
                }
            }
        }
        ChunkData cData = new ChunkData(0, 0, tiles, true, 5, heights);
        cData.SetVoxelData(cvd);
        ChunkLoader.LoadChunk(cData);

    }

    void Update()
    {
        
    }
}
