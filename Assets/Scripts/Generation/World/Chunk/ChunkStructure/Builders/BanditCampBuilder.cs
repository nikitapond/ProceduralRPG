using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class BanditCampBuilder : ChunkStructureBuilder
{

    public GenerationRandom GenRan;
    public int Boundry = 5;
    public BanditCampBuilder(ChunkStructure structure, GameGenerator gameGen = null) : base(structure, gameGen)
    {
        structure.Name = "Bandit camp";
        RaiseBase(Boundry, Boundry);
    }

    public override void Generate(GenerationRandom ran)
    {
        GenRan = ran;
        BuildWallAndEntrance();
        GenerateSubworldCave(new Vec2i(TileSize.x-10, TileSize.z-10));
        DEBUG = false;
        EntityFaction bandits = new EntityFaction("Bandits");
        for(int i=0; i<10; i++)
        {
            Vector3 pos = GenRan.RandomVector3(Boundry + 2, Mathf.Min(TileSize.x, TileSize.z) - Boundry - 2);
            Bandit bandit = new Bandit();
            bandit.SetEntityFaction(bandits);
            bandit.SetPosition(pos);
            AddEntity(bandit);
        }
    }


    private void BuildWallAndEntrance()
    {

        for (int x = Boundry + 1; x < TileSize.x - Boundry - 1; x++)
        {

            if(!(x>Boundry+3 && x < Boundry + 8))
            {
                WoodSpikeWall wall1 = new WoodSpikeWall();
                wall1.SetPosition(new Vec2i(x, Boundry + 1));
                AddObject(wall1, true);
            }
            
            WoodSpikeWall wall2 = new WoodSpikeWall();
            wall2.SetPosition(new Vec2i(x, TileSize.z - Boundry - 1));
            AddObject(wall2, true);
        }
        for (int z = Boundry + 1; z < TileSize.z - Boundry - 1; z++)
        {
            WoodSpikeWall wall1 = new WoodSpikeWall();
            wall1.SetPosition(new Vec2i(Boundry + 1, z));
            AddObject(wall1, true);
            WoodSpikeWall wall2 = new WoodSpikeWall();
            wall2.SetPosition(new Vec2i(TileSize.x - Boundry - 1, z));
            AddObject(wall2, true);
        }
        for(int x=0; x<TileSize.x; x++)
        {
            for(int z=0; z<TileSize.z; z++)
            {
                SetTile(x, z, Tile.DIRT);
            }
        }
    }

    public void GenerateSubworldCave(Vec2i localEntrance)
    {

        DoubleBed b = new DoubleBed();
        b.SetPosition(localEntrance);
        AddObject(b, true);
        TrapDoor td = new TrapDoor();
        td.SetPosition(localEntrance);
        AddObject(td, true);
        Debug.Log("HERHEHRE");
        
        CaveDungeonBuilder cdb = new CaveDungeonBuilder(localEntrance + BaseTile, new Vec2i(4, 4));
        cdb.Generate(GenRan);
        Subworld cave = cdb.ToSubworld();
        AddSubworld(td, cave);
        
    }
}