using UnityEngine;
using UnityEditor;

public class BanditCampBuilder : ChunkStructureBuilder
{

    public GenerationRandom GenRan;
    public BanditCampBuilder(ChunkStructure structure) : base(structure)
    {
        structure.Name = "Bandit camp";
    }

    public override void Generate(GenerationRandom ran)
    {

        for(int x=0; x < TileSize.x; x++)
        {
            for (int z = 0; z < TileSize.z; z++)
            {
                SetTile(x, z, Tile.TEST_RED);
               /*for(int y=0; y<3; y++)
                {
                    SetVoxelNode(x, y, z, new VoxelNode(Voxel.stone));
                }*/
            }
        }
        GenerateSubworldCave(new Vec2i(6, 6));
        DEBUG = true;
    }


    public void GenerateSubworldCave(Vec2i localEntrance)
    {

        DoubleBed b = new DoubleBed();
        b.SetPosition(localEntrance);
        AddObject(b, true, true);
        TrapDoor td = new TrapDoor();
        td.SetPosition(localEntrance);
        AddObject(td, true, true);
        Debug.Log("HERHEHRE");
        
        CaveDungeonBuilder cdb = new CaveDungeonBuilder(localEntrance + BaseTile, new Vec2i(4, 4));
        cdb.Generate(GenRan);
        Subworld cave = cdb.ToSubworld();
        AddSubworld(td, cave);
        
    }
}