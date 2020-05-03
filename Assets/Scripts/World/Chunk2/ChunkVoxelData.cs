using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
[System.Serializable]
public class ChunkVoxelData
{

    public Voxel[] Voxels { get; private set; }

    /// <summary>
    /// A dictionary that defines the min
    /// </summary>
    public Dictionary<Voxel, VoxelBounds> VoxelTypeBounds { get; private set; }


    public ChunkVoxelData()
    {
        Voxels = new Voxel[(World.ChunkSize+1) * (World.ChunkSize+1) * (World.ChunkHeight+1)];
        VoxelTypeBounds = new Dictionary<Voxel, VoxelBounds>();
    }


    public void SetVoxel(int x, int y, int z, Voxel voxel)
    {
        int idx = x + y * (World.ChunkSize+1) + z * (World.ChunkHeight+1) * (World.ChunkSize+1);
        Voxels[idx] = voxel;
        //If a voxel of this type has been added already, we re-define the bounds
        if (VoxelTypeBounds.TryGetValue(voxel, out VoxelBounds t))
        {
            if (x < t.MinX)
                t.MinX = x;
            else if (x > t.MaxX)
                t.MaxX = x;

            if (y < t.MinY)
                t.MinY = y;
            else if (y > t.MaxY)
                t.MaxY = y;

            if (z < t.MinZ)
                t.MinZ = z;
            else if (z > t.MaxZ)
                t.MaxZ = z;
        }
        else
        {
            //If this is the first instance of this voxel type, we add it to the dictionary
            VoxelTypeBounds.Add(voxel, new VoxelBounds(x,y,z));
        }

    }
    public Voxel GetVoxel(int x, int y, int z)
    {
        int idx = x + y * (World.ChunkSize+1) + z * (World.ChunkHeight+1) * (World.ChunkSize+1);
        return Voxels[idx];
    }

}
[System.Serializable]
public class VoxelBounds
{

    public int MinX, MaxX, MinY, MaxY, MinZ, MaxZ;

    public VoxelBounds(int startX, int startY, int startZ)
    {
        MinX = startX;
        MaxX = startX;

        MinY = startY;
        MaxY = startY;

        MinZ = startZ;
        MaxZ = startZ;
    }
}