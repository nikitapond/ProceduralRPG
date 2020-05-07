using UnityEngine;
using UnityEditor;

public class BuildingVoxels
{
    public readonly int Width, Height, Depth;

    public VoxelNode[] Voxels { get; private set; }

    public BuildingVoxels(int width, int height, int depth)
    {
        Voxels = new VoxelNode[(width+1) * (height+1) * (depth+1)];
        Width = width;
        Height = height;
        Depth = depth;
    }

    public void SetVoxel(int x, int y, int z, Voxel voxel)
    {
        int idx = x + y * (Width) + z * (Width) * (Height);
        Voxels[idx] = new VoxelNode(voxel);
        //Voxels[idx].SetVoxel(voxel);
    }
    public void AddVoxel(int x, int y, int z, Voxel voxel)
    {
        int idx = x + y * (Width) + z * (Width) * (Height);
        if (!Voxels[idx].IsNode)
        {
            Voxels[idx] = new VoxelNode(voxel);
        }
        else
        {
            //Debug.Log("Voxel adding " + voxel);
            Voxels[idx].AddVoxel(voxel);
           // Debug.Log("Voxel Node is now:" + Voxels[idx]);
        }
        //Voxels[idx].AddVoxel(voxel);
    }
    public void ClearVoxel(int x, int y, int z)
    {
        int idx = x + y * (Width) + z * (Width) * (Height);
        Voxels[idx] = new VoxelNode(Voxel.none);
    }
    public Voxel GetVoxel(int x, int y, int z)
    {
        int idx = x + y * (Width) + z * (Width) * (Height);
        return Voxels[idx].Voxel;
    }
    public VoxelNode GetVoxelNode(int x, int y, int z)
    {
        int idx = x + y * (Width) + z * (Width) * (Height);
        return Voxels[idx];
    }
}