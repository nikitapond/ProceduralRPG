using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class DungeonGenerator
{

    private GameGenerator GameGenerator;
    private GenerationRandom GenRan;
    private int[,] BASE_DIRT;
    public DungeonGenerator(GameGenerator gameGen)
    {
        GameGenerator = gameGen;
        GenRan = new GenerationRandom(gameGen.Seed);
        BASE_DIRT = new int[World.ChunkSize, World.ChunkSize];
        for (int x = 0; x < World.ChunkSize; x++)
        {
            for (int z = 0; z < World.ChunkSize; z++)
            {
                BASE_DIRT[x, z] = Tile.DIRT.ID;
            }
        }
    }


    public List<Dungeon> GenerateAllDungeons(List<DungeonEntrance> dungeonEntrances)
    {
        List<Dungeon> allDung = new List<Dungeon>(100);


        foreach(DungeonEntrance de in dungeonEntrances)
        {
            allDung.Add(GenerateDungeon(de));
        }

        return allDung;
    }


    private Dungeon GenerateDungeon(DungeonEntrance entrance)
    {
        /*
        switch (entrance.DungeonType)
        {
            case DungeonType.FIRE:
                return GenerateFireDungeon(entrance);
            case DungeonType.CAVE:
                return GenerateCaveDungeon(entrance);
        }*/
        return null;
    }




    private Vec2i ChooseValidChunk()
    {
        Vec2i pos = GenRan.RandomFromList(GameGenerator.TerrainGenerator.LandChunks);
        while(!IsPositionValid(pos))
            pos = GenRan.RandomFromList(GameGenerator.TerrainGenerator.LandChunks);
        return pos;
    }

    ///Contains function to check if positions are valid
    #region misc
    /// <summary>
    /// Takes a position and size, checks against terrain values to see if 
    /// the position is free to place the desired structure on
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    private bool IsPositionValid(Vec2i position, int clear = 2)
    {

        for (int x = -clear; x <= clear; x++)
        {
            for (int z = -clear; z <= clear; z++)
            {
                int cx = position.x + x;
                int cz = position.z + z;
                //Debug.Log(cx + "_" + cz);
                if (cx < 0 || cz < 0 || cx >= World.WorldSize - 1 || cz >= World.WorldSize - 1)
                    return false;
                ChunkBase cb = GameGenerator.TerrainGenerator.ChunkBases[cx, cz];
                if (cb.HasSettlement || cb.RiverNode != null || cb.Lake != null || cb.ChunkStructure != null)
                {
                    //Debug.Log(cb.HasSettlement + "_" + cb.RiverNode + "_" + cb.Lake + "_" + cb.ChunkStructure);
                    return false;
                }
            }
        }
        return true;
    }
    #endregion

}