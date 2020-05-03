using UnityEngine;
using UnityEditor;

public class BuildingVoxels
{
    public readonly int Width, Height, Depth;

    public Voxel[] Voxels { get; private set; }

    public BuildingVoxels(int width, int height, int depth)
    {
        Voxels = new Voxel[(width+1) * (height+1) * (depth+1)];
        Width = width;
        Height = height;
        Depth = depth;
    }

    public void SetVoxel(int x, int y, int z, Voxel voxel)
    {
        int idx = x + y * (Width) + z * (Width) * (Height);
        Voxels[idx] = voxel;
    }

    public Voxel GetVoxel(int x, int y, int z)
    {
        int idx = x + y * (Width) + z * (Width) * (Height);
        return Voxels[idx];
    }
}