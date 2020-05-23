using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
[System.Serializable]
public class ChunkVoxelData
{



    [System.NonSerialized]
    public VoxelNode[] Voxels;

    private Dictionary<Voxel, bool> KnownVoxels;

    /// <summary>
    /// A dictionary that defines the min
    /// </summary>
    public Dictionary<Voxel, VoxelBounds> VoxelTypeBounds { get; private set; }
    public Dictionary<Vec3i, VoxelNode> VoxelNodes;

    public ChunkVoxelData()
    {
        Voxels = new VoxelNode[(World.ChunkSize+1) * (World.ChunkSize+1) * (World.ChunkHeight+1)];
        VoxelTypeBounds = new Dictionary<Voxel, VoxelBounds>();
        KnownVoxels = new Dictionary<Voxel, bool>();
    }


    public void Compress()
    {
        VoxelNodes = new Dictionary<Vec3i, VoxelNode>();
        for(int x=0; x<World.ChunkSize+1; x++)
        {
            for (int y = 0; y < World.ChunkHeight + 1; y++)
            {
                for (int z = 0; z < World.ChunkSize + 1; z++)
                {
                    int idx = x + y * (World.ChunkSize + 1) + z * (World.ChunkHeight + 1) * (World.ChunkSize + 1);
                    if (Voxels[idx].IsNode)
                    {
                        VoxelNodes.Add(new Vec3i(x, y, z), Voxels[idx]);
                    }
                }
            }
        }
        Voxels = null;
    }
    public void UnCompress()
    {
        Voxels = new VoxelNode[(World.ChunkSize + 1) * (World.ChunkSize + 1) * (World.ChunkHeight + 1)];
        foreach(KeyValuePair<Vec3i, VoxelNode> kvp in VoxelNodes)
        {
            int idx = kvp.Key.x + kvp.Key.y * (World.ChunkSize + 1) + kvp.Key.z * (World.ChunkHeight + 1) * (World.ChunkSize + 1);

            Voxels[idx] = kvp.Value;
        }
        VoxelNodes.Clear();
    }

    public bool HasVoxel(Voxel v)
    {



        if (KnownVoxels.TryGetValue(v, out bool val))
            return val;
        return false;
    }
    private void SetHasVoxel(Voxel v, bool val)
    {
        if (KnownVoxels.ContainsKey(v))
            KnownVoxels[v] = val;
        else
            KnownVoxels.Add(v, val);
    }

    
    public void AddVoxel(int x, int y, int z, Voxel voxel)
    {
        int idx = x + y * (World.ChunkSize + 1) + z * (World.ChunkHeight + 1) * (World.ChunkSize + 1);
        if (!Voxels[idx].IsNode)
        {
           // Debug.Log(x + "," + y + "," + z + " has no node, now " + voxel);
            Voxels[idx] = new VoxelNode(voxel);
        }
        else
        {
            Voxels[idx].AddVoxel(voxel);
        }
        //Debug.Log("Has  " + voxel + " true");
        SetHasVoxel(voxel, true);


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
            VoxelTypeBounds.Add(voxel, new VoxelBounds(x, y, z));
        }

    }


    public void SetVoxelNode(int x, int y, int z, VoxelNode node)
    {
        int idx = x + y * (World.ChunkSize + 1) + z * (World.ChunkHeight + 1) * (World.ChunkSize + 1);
        Voxels[idx] = node;
        //Debug.Log("setting node " + x + "," + y + ", " + z + " to " + node.ToString());

        SetVoxel(x, y, z, node.Voxel);
        SetHasVoxel(node.Voxel, true);

        if (node.OtherVoxels != null)
        {
            foreach(Voxel v in node.OtherVoxels)
            {
                AddVoxel(x, y, z, v);
                SetHasVoxel(v, true);

            }
        }


    }
    public VoxelNode GetVoxelNode(int x, int y, int z)
    {
        int idx = x + y * (World.ChunkSize + 1) + z * (World.ChunkHeight + 1) * (World.ChunkSize + 1);
        return Voxels[idx];
    }
    public void SetVoxel(int x, int y, int z, Voxel voxel)
    {
        int idx = x + y * (World.ChunkSize+1) + z * (World.ChunkHeight+1) * (World.ChunkSize+1);
        Voxels[idx].SetVoxel(voxel);
        SetHasVoxel(voxel, true);

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
        return Voxels[idx].Voxel;
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