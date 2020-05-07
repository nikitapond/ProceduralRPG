using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
/// <summary>
/// Defines a node in voxel space that may contain any
/// number of voxels
/// </summary>
[System.Serializable]
public struct VoxelNode 
{
    private Voxel? Voxel_;
    public Voxel Voxel { get { return Voxel_ ?? Voxel.none; } set { Voxel_ = value; } }
    public List<Voxel> OtherVoxels;
    public bool IsNode { get { return Voxel != Voxel.none; } }

    public VoxelNode(Voxel v)
    {
        Voxel_ = v;
        OtherVoxels = new List<Voxel>(1);
    }

    public override string ToString()
    {
        if (OtherVoxels == null)
            return "Vox: " + Voxel;
        else
        {
            string str = "Vox: " + Voxel;
            foreach (Voxel v in OtherVoxels)
                str += ", " + v.ToString();
            return str;
        }
    }
}

public static class VoxelNodeHelper
{
    public static bool ContainsVoxel(this VoxelNode node, Voxel query)
    {
        if (node.Voxel == query)
            return true;
        if (node.OtherVoxels == null)
            return false;

        return node.OtherVoxels.Contains(query);

    }

    public static void SetVoxel(this VoxelNode node, Voxel set)
    {
        node.Voxel = set;

       // Debug.Log("setting voxel: " + set);
    }
    public static void AddVoxel(this VoxelNode node, Voxel add)
    {
        if (node.Voxel == add)
            return;

        if (!node.OtherVoxels.Contains(add))
            node.OtherVoxels.Add(add);
        
    }

    public static void ClearVoxel(this VoxelNode node)
    {
        node.OtherVoxels.Clear();
        node.Voxel = Voxel.none;
    }
}