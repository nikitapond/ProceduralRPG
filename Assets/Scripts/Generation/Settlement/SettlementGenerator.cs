using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Threading;
public class SettlementGenerator
{
    private GameGenerator GameGenerator;
    public List<SettlementBase> Settlements { get; private set; }
    public Kingdom Kingdom { get; private set; }
    public World World { get; private set; }
    public SettlementGenerator(GameGenerator gamGen, World world, Kingdom kingdom, List<SettlementBase> settlements)
    {
        GameGenerator = gamGen;
        Settlements = settlements;
        Kingdom = kingdom;
        World = world;
    }

    public Dictionary<Settlement, Dictionary<Vec2i, ChunkData2>> GenerateAllSettlements()
    {
        Dictionary<Settlement, Dictionary<Vec2i, ChunkData2>> setChunks = new Dictionary<Settlement, Dictionary<Vec2i, ChunkData2>>(Settlements.Count);

   
        foreach (SettlementBase b in Settlements)
        {


            SettlementBuilder setBuild = new SettlementBuilder(GameGenerator, b);
           
            setBuild.GenerateSettlement();
            //Debug.EndProfile();

            Settlement set = new Settlement(Kingdom, "set", setBuild);
            World.AddSettlement(set);

            setChunks.Add(set, GenerateAllSettlementChunks(setBuild, set));

            //Debug.EndProfile();

        }
        
        return setChunks;
    }

    public Dictionary<Vec2i, ChunkData2> GenerateAllSettlementChunks(SettlementBuilder setBuild, Settlement set)
    {

        Vec2i baseChunk = new Vec2i(setBuild.BaseCoord.x / World.ChunkSize, setBuild.BaseCoord.z / World.ChunkSize);
        Dictionary<Vec2i, ChunkData2> setChunks = new Dictionary<Vec2i, ChunkData2>();
        int setSizeChunks = setBuild.TileSize / World.ChunkSize;
        for(int x=0; x< setSizeChunks; x++)
        {
            for(int z=0; z< setSizeChunks; z++)
            {
                ChunkBase cb = GameGenerator.TerrainGenerator.ChunkBases[baseChunk.x + x, baseChunk.z + z];
                ChunkData2 cd = null;
                if(cb.RiverNode != null || cb.Lake != null)
                {
                    cd = GameGenerator.ChunkGenerator.GenerateChunk(baseChunk.x + x, baseChunk.z + z);
                }
                else
                {
                    int[,] cTiles = new int[World.ChunkSize, World.ChunkSize];
                    float[,] cHeights = new float[World.ChunkSize, World.ChunkSize]; ;
                    Dictionary<int, WorldObjectData> cObj = new Dictionary<int, WorldObjectData>();
                    for (int x_ = 0; x_ < World.ChunkSize; x_++)
                    {
                        for (int z_ = 0; z_ < World.ChunkSize; z_++)
                        {
                            if(setBuild.Heights[x * World.ChunkSize + x_, z * World.ChunkSize + z_] > 0)
                            {
                                cHeights[x_, z_] = setBuild.Heights[x * World.ChunkSize + x_, z * World.ChunkSize + z_];
                            }
                            else
                            {
                                cHeights[x_, z_] = cb.BaseHeight;
                            }
                            Tile st = setBuild.Tiles[x * World.ChunkSize + x_, z * World.ChunkSize + z_];
                            cTiles[x_, z_] = st == null ? Tile.GRASS.ID : st.ID;
                            cObj[WorldObject.ObjectPositionHash(x_, z_)] = setBuild.SettlementObjects[x * World.ChunkSize + x_, z * World.ChunkSize + z_];
                            if (setBuild.SettlementObjects[x * World.ChunkSize + x_, z * World.ChunkSize + z_] != null)
                            {
                                setBuild.SettlementObjects[x * World.ChunkSize + x_, z * World.ChunkSize + z_].SetPosition(new Vec2i(baseChunk.x + x + x_, baseChunk.z + z + z_));
                            }
                        }
                    }
                    cd = new ChunkData2(baseChunk.x + x, baseChunk.z + z, cTiles, true, baseHeight:cb.BaseHeight, heightMap:cHeights, null);
                }


                cd.SetSettlement(set);
                setChunks.Add(new Vec2i(cd.X, cd.Z), cd);
            }
        }
        set.AfterApplyToWorld();
        return setChunks;
    }
}