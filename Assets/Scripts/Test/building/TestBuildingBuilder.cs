using UnityEngine;
using UnityEditor;

public class TestBuildingBuilder : BuilderBase
{
    public TestBuildingBuilder(Vec2i baseChunk, Vec2i chunkSize) : base(baseChunk, chunkSize, null, null)
    {
    }

    

    public void AddBuilding(Building b, BuildingVoxels vox, Vec2i pos)
    {
        float minHeight = 0;

        //SetTiles(pos.x, pos.z, b.Width, b.Height, b.BuildingTiles);
        for (int x = 0; x < b.Width; x++)
        {
            for (int z = 0; z < b.Height; z++)
            {

                int cx = WorldToChunk(x);
                int cz = WorldToChunk(z);

                int deltaHeight = ChunkBases != null ? (int)(minHeight - ChunkBases[cx, cz].Height) : 0;

                SetTile(x + pos.x, z + pos.z, b.BuildingTiles[x, z]);
                SetHeight(x + pos.x, z + pos.z, minHeight);


                

                for (int y = 0; y < vox.Height; y++)
                {
                    VoxelNode voxNode = vox.GetVoxelNode(x, y, z);
                    if (voxNode.IsNode)
                    {
                        //Debug.Log("Adding vox node: " + voxNode);
                        SetVoxelNode(x + pos.x, y, z + pos.z, vox.GetVoxelNode(x, y, z));
                    }
                   
                }
                
            }
        }
        foreach(WorldObjectData obj in b.GetBuildingExternalObjects())
        {

            obj.SetPosition(obj.Position + pos.AsVector3());
            //We (should) already have checked for object validity when creating the building
            AddObject(obj, true);
        }
    }

    public override void Generate(GenerationRandom ran)
    {
        throw new System.NotImplementedException();
    }
}